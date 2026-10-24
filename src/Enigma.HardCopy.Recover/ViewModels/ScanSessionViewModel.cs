using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.Resources;
using Enigma.HardCopy.Recover.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// The application's one screen: a scan session that collects codes from the camera, in any order, until the file
/// can be rebuilt — and saved, or, for a check, verified and let go.
/// </summary>
/// <remarks>
/// <para>
/// <b>Core does the recovering.</b> Every code goes into one <see cref="RecoverySession"/>, which adopts the backup
/// ID and the code count from the first code it verifies and refuses anything from another backup from then on —
/// which is the whole of "do not mix codes from different files". This ViewModel turns what the session reports
/// into progress, feedback and the save flow.
/// </para>
/// <para>
/// <b>Each distinct text is handled once.</b> The camera reports a code again on every frame it is in view, many
/// times a second. A text already handled in this session — accepted, refused or duplicate — is ignored without a
/// word, so a code held in view does not stutter the haptics or repeat a warning banner.
/// </para>
/// <para>
/// <b>A hash mismatch never saves silently.</b> As in the desktop app, the rebuilt bytes are held back and only an
/// explicit, confirmed second action writes them.
/// </para>
/// <para>
/// <b>A session either recovers or checks.</b> The two scan alike and differ only once every code is in: a
/// recovery offers to save the file; a check rebuilds it in memory, compares its SHA-256 with the one the backup
/// recorded, says whether the printout is sound, and lets the bytes go. A check never reaches the save picker.
/// </para>
/// <para>
/// <b>Nothing outlives the process.</b> The session, the texts seen and any rebuilt bytes live in this object only;
/// starting a new session drops them.
/// </para>
/// </remarks>
public sealed class ScanSessionViewModel : ObservableObject
{
    /// <summary>How many runs of missing indexes are listed before the rest is summarised as a count.</summary>
    public const int MaxListedMissingRanges = 8;

    private readonly ICameraScanner _scanner;
    private readonly IHapticFeedback _haptics;
    private readonly IRecoveredFileSaver _saver;
    private readonly INotificationService _notifications;
    private readonly IConfirmationService _confirmations;
    private readonly ILogger<ScanSessionViewModel> _logger;

    /// <summary>Every code text handled in this session, so that a code held in view is handled once.</summary>
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    private RecoverySession _session = new();
    private RecoveryStatus _status;
    private AssemblyResult? _unverified;

    /// <summary>Initializes a new instance of the <see cref="ScanSessionViewModel"/> class.</summary>
    /// <param name="scanner">The camera.</param>
    /// <param name="haptics">The tick in the user's hand for each code.</param>
    /// <param name="saver">Asks where the recovered file goes.</param>
    /// <param name="notifications">Carries what the user must not miss to the info bar.</param>
    /// <param name="confirmations">Asks before the two things a session cannot take back.</param>
    /// <param name="logger">Records failures for diagnosis; never a code's text.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ScanSessionViewModel(
        ICameraScanner scanner,
        IHapticFeedback haptics,
        IRecoveredFileSaver saver,
        INotificationService notifications,
        IConfirmationService confirmations,
        ILogger<ScanSessionViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(haptics);
        ArgumentNullException.ThrowIfNull(saver);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(confirmations);
        ArgumentNullException.ThrowIfNull(logger);

        _scanner = scanner;
        _haptics = haptics;
        _saver = saver;
        _notifications = notifications;
        _confirmations = confirmations;
        _logger = logger;
        _status = _session.Status;

        StartScanningCommand = new AsyncRelayCommand(OnStartScanningAsync, () => State is ScanState.Ready && !IsBusy);
        StartCheckCommand = new AsyncRelayCommand(OnStartCheckAsync, () => CanStartCheck && !IsBusy);
        StopScanningCommand = new RelayCommand(OnStopScanning, () => State is ScanState.Scanning);
        SaveCommand = new AsyncRelayCommand(OnSaveAsync, () => CanSave && !IsBusy);
        SaveAnywayCommand = new AsyncRelayCommand(OnSaveAnywayAsync, () => _unverified is not null && !IsBusy);
        VerifyCommand = new AsyncRelayCommand(OnVerifyAsync, () => State is ScanState.Complete && IsChecking && !IsBusy);
        NewSessionCommand = new AsyncRelayCommand(OnNewSessionAsync, () => State is not ScanState.Starting && !IsBusy);

        // The ViewModel is a singleton that lives as long as the process, exactly like the scanner, so the
        // subscription never needs undoing.
        _scanner.CodesScanned += OnCodesScanned;
    }

    /// <summary>Gets where the session stands, from the camera's point of view.</summary>
    public ScanState State
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(IsStarting));
                OnPropertyChanged(nameof(IsScanning));
                OnPropertyChanged(nameof(IsComplete));
                OnPropertyChanged(nameof(ShowIntroduction));
                OnPropertyChanged(nameof(CanStartCheck));
                OnPropertyChanged(nameof(CanSave));
                NotifyCommands();
            }
        }
    }

    /// <summary>
    /// Gets what the session is for. The button that turns the camera on for an empty session decides it; resuming
    /// a session that holds codes keeps it.
    /// </summary>
    public SessionPurpose Purpose
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsChecking));
                OnPropertyChanged(nameof(ProgressTitle));
                OnPropertyChanged(nameof(CanSave));
                NotifyCommands();
            }
        }
    }

    /// <summary>Gets a value indicating whether the session checks a printout rather than recovering the file.</summary>
    public bool IsChecking => Purpose is SessionPurpose.Check;

    /// <summary>
    /// Gets the progress card's heading, which says when the session is a check — so nobody finishes a scan
    /// expecting a save button a check never shows.
    /// </summary>
    public string ProgressTitle => IsChecking ? Strings.CheckProgressHeader : Strings.ProgressHeader;

    /// <summary>
    /// Gets a value indicating whether a check can be started: the camera is off and the session is still empty, so
    /// what it is for has not been settled by a code yet.
    /// </summary>
    public bool CanStartCheck => State is ScanState.Ready && !HasAnyCode;

    /// <summary>Gets a value indicating whether every code is in and the file can be saved — never for a check.</summary>
    public bool CanSave => State is ScanState.Complete && Purpose is SessionPurpose.Recover;

    /// <summary>Gets whether the checked printout is sound, or <see langword="null"/> until a check is over.</summary>
    public StatusMessage? CheckVerdict
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasCheckVerdict));
            }
        }
    }

    /// <summary>Gets a value indicating whether a check is over and its verdict is there to read.</summary>
    public bool HasCheckVerdict => CheckVerdict is not null;

    /// <summary>Gets a value indicating whether the camera is off and more codes can be scanned.</summary>
    public bool IsReady => State is ScanState.Ready;

    /// <summary>Gets a value indicating whether the camera is being started.</summary>
    public bool IsStarting => State is ScanState.Starting;

    /// <summary>Gets a value indicating whether the camera is running.</summary>
    public bool IsScanning => State is ScanState.Scanning;

    /// <summary>Gets a value indicating whether every code is in.</summary>
    public bool IsComplete => State is ScanState.Complete;

    /// <summary>Gets a value indicating whether the introduction is worth showing: nothing has happened yet.</summary>
    public bool ShowIntroduction => !HasAnyCode && State is ScanState.Ready;

    /// <summary>Gets a value indicating whether a save is under way.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                NotifyCommands();
            }
        }
    }

    /// <summary>Gets why the camera is not running when it was asked to, or <see langword="null"/>.</summary>
    public string? CameraProblem
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasCameraProblem));
            }
        }
    }

    /// <summary>Gets a value indicating whether there is a camera problem to explain.</summary>
    public bool HasCameraProblem => CameraProblem is not null;

    /// <summary>Gets what became of the last new code the camera read, or <see langword="null"/>.</summary>
    public StatusMessage? LastScan
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasLastScan));
            }
        }
    }

    /// <summary>Gets a value indicating whether a code has been read in this session.</summary>
    public bool HasLastScan => LastScan is not null;

    /// <summary>Gets a value indicating whether any code has been accepted yet.</summary>
    public bool HasAnyCode => _status.BackupId is not null;

    /// <summary>Gets the label of the button that turns the camera on.</summary>
    public string StartLabel => HasAnyCode ? Strings.ResumeScanning : Strings.StartScanning;

    /// <summary>Gets the backup the session is recovering, or <see langword="null"/> while it is unknown.</summary>
    public string? BackupId => _status.BackupId;

    /// <summary>Gets the name recorded in the backup, or <see langword="null"/> until the metadata code is in.</summary>
    public string? FileName => _status.Metadata?.FileName;

    /// <summary>Gets the recorded size, written out, or <see langword="null"/> until the metadata code is in.</summary>
    public string? FileSizeText => _status.Metadata is { } metadata
        ? Format(Strings.FileSizeFormat, metadata.OriginalSizeInBytes)
        : null;

    /// <summary>Gets the hash recorded in the backup, or <see langword="null"/> until the metadata code is in.</summary>
    public string? ExpectedSha256 => _status.Metadata?.Sha256Hex;

    /// <summary>Gets a value indicating whether the metadata code has been read.</summary>
    public bool HasMetadata => _status.HasMetadata;

    /// <summary>Gets how many data codes are in, against how many the backup is made of.</summary>
    public string CodesText => _status.TotalChunks is int total
        ? Format(Strings.CodesProgressFormat, _status.ReceivedChunks, total)
        : Strings.CodesProgressUnknown;

    /// <summary>Gets whether the metadata code — the one that names the file — is in.</summary>
    public string MetadataText => _status.HasMetadata ? Strings.MetadataReceived : Strings.MetadataMissing;

    /// <summary>
    /// Gets how far the recovery has got, from 0 to 100, counting the metadata code as one more code: the file
    /// cannot be rebuilt without it, so a bar at 100 % without it would be a lie.
    /// </summary>
    public double ProgressPercent => _status.TotalChunks is int total
        ? (_status.ReceivedChunks + (_status.HasMetadata ? 1 : 0)) * 100d / (total + 1)
        : 0d;

    /// <summary>Gets a value indicating whether some data codes are known to be missing.</summary>
    public bool HasMissing => _status.MissingIndexes.Count > 0;

    /// <summary>Gets the data codes still missing, as ranges, the longest lists summarised.</summary>
    public string MissingText => IndexRanges.Format(_status.MissingIndexes, MaxListedMissingRanges);

    /// <summary>
    /// Gets a value indicating whether a rebuilt-but-unverified file is waiting for the user to decide about it.
    /// </summary>
    public bool CanSaveAnyway => _unverified is not null;

    /// <summary>
    /// Gets the command that turns the camera on — for a recovery when the session is empty, or to carry on with
    /// whatever the session was started as.
    /// </summary>
    public AsyncRelayCommand StartScanningCommand { get; }

    /// <summary>Gets the command that turns the camera on to check a printout rather than recover the file.</summary>
    public AsyncRelayCommand StartCheckCommand { get; }

    /// <summary>Gets the command that pauses the camera, keeping every code read so far.</summary>
    public RelayCommand StopScanningCommand { get; }

    /// <summary>Gets the command that rebuilds the file and, when it verifies, saves it.</summary>
    public AsyncRelayCommand SaveCommand { get; }

    /// <summary>Gets the command that writes a rebuilt file whose hash did not match, once confirmed.</summary>
    public AsyncRelayCommand SaveAnywayCommand { get; }

    /// <summary>
    /// Gets the command that rebuilds the file in memory, checks its SHA-256 and reports the verdict, keeping none
    /// of the bytes. It runs by itself when a check's last code arrives; it is a command rather than a detached
    /// task so the work is tracked, awaitable, and fails the way every other command does.
    /// </summary>
    public AsyncRelayCommand VerifyCommand { get; }

    /// <summary>Gets the command that discards the session and starts scanning a new one.</summary>
    public AsyncRelayCommand NewSessionCommand { get; }

    private Task OnStartScanningAsync()
    {
        // An empty session has not committed to anything, so this button makes it a recovery. A session that holds
        // codes carries on as whatever it was started as — Resume scanning resumes a check too.
        if (!HasAnyCode)
        {
            Purpose = SessionPurpose.Recover;
        }

        return StartCameraAsync();
    }

    private Task OnStartCheckAsync()
    {
        Purpose = SessionPurpose.Check;

        return StartCameraAsync();
    }

    private async Task StartCameraAsync()
    {
        CameraProblem = null;
        State = ScanState.Starting;

        CameraStartResult result = await _scanner.StartAsync();

        switch (result)
        {
            case CameraStartResult.Started:
                State = ScanState.Scanning;
                break;
            case CameraStartResult.PermissionDenied:
                CameraProblem = Strings.CameraPermissionDenied;
                State = ScanState.Ready;
                break;
            default:
                CameraProblem = Strings.CameraUnavailable;
                State = ScanState.Ready;
                break;
        }
    }

    private void OnStopScanning()
    {
        _scanner.Stop();
        State = ScanState.Ready;
    }

    private void OnCodesScanned(object? sender, CodesScannedEventArgs e)
    {
        // A frame read just before the camera stopped can still be on its way.
        if (State is not ScanState.Scanning)
        {
            return;
        }

        bool anyNew = false;
        foreach (string code in e.Codes)
        {
            if (!_seen.Add(code))
            {
                continue;
            }

            anyNew = true;
            Report(_session.AddCode(code));
        }

        if (!anyNew)
        {
            return;
        }

        RefreshStatus();

        if (_status.IsComplete)
        {
            _scanner.Stop();
            State = ScanState.Complete;

            if (IsChecking)
            {
                // The verdict is the announcement: an "every code is in" banner would only be covered by it.
                VerifyCommand.Execute(null);
            }
            else
            {
                _notifications.Publish(StatusMessage.Success(Strings.SessionComplete));
            }
        }
    }

    /// <summary>
    /// Tells the user what became of one new code: on the page, in the hand, and — for a refusal — in a banner.
    /// </summary>
    private void Report(AddCodeResult result)
    {
        // Only a refusal needs the session's backup ID, and it is read only then: the snapshot walks the session.
        string? sessionBackupId = result.Outcome is AddCodeOutcome.WrongBackup ? _session.Status.BackupId : null;
        StatusMessage message = OutcomeMessages.Describe(result, sessionBackupId);
        LastScan = message;

        switch (result.Outcome)
        {
            case AddCodeOutcome.Accepted:
                _haptics.Accepted();
                break;
            case AddCodeOutcome.Duplicate:
                // The same code under a different spelling: nothing changed and nothing is wrong.
                break;
            default:
                _haptics.Rejected();
                _notifications.Publish(message);
                break;
        }
    }

    private async Task OnSaveAsync()
    {
        IsBusy = true;
        AssemblyResult result;
        try
        {
            // Decompression and hashing are CPU-bound; a large backup would otherwise freeze the screen.
            result = await Task.Run(_session.TryAssemble);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is { Outcome: AssemblyOutcome.Verified, Content: byte[] content })
        {
            await SaveAsync(content, result.Metadata?.FileName, verified: true);
            return;
        }

        if (result.Outcome is AssemblyOutcome.HashMismatch)
        {
            SetUnverified(result);
        }

        _notifications.Publish(OutcomeMessages.Describe(result));
    }

    private async Task OnVerifyAsync()
    {
        IsBusy = true;
        AssemblyResult result;
        try
        {
            // The same rebuild a save does, off the UI thread for the same reason.
            result = await Task.Run(_session.TryAssemble);
        }
        finally
        {
            IsBusy = false;
        }

        // Only the verdict is kept. The rebuilt bytes go out of scope with the result — a check never holds them
        // for a later save, which is what keeps it from ever producing the file.
        StatusMessage verdict = OutcomeMessages.DescribeCheck(result);
        CheckVerdict = verdict;
        _notifications.Publish(verdict);
    }

    private async Task OnSaveAnywayAsync()
    {
        if (_unverified is not { Content: byte[] content } result)
        {
            return;
        }

        // Asked before the picker, so declining costs nothing and leaves the offer standing. Only an explicit
        // agreement gets past here — a dismissed dialog is a refusal.
        bool agreed = await _confirmations.ConfirmUnverifiedSaveAsync(
            result.Metadata?.Sha256Hex ?? string.Empty,
            result.Sha256Hex ?? string.Empty);

        if (agreed && await SaveAsync(content, result.Metadata?.FileName, verified: false))
        {
            // Decided and acted on; leaving the button would only invite a second copy of the same bytes.
            SetUnverified(null);
        }
    }

    private async Task<bool> SaveAsync(byte[] content, string? suggestedFileName, bool verified)
    {
        ISaveTarget? target = await _saver.ChooseDestinationAsync(
            string.IsNullOrWhiteSpace(suggestedFileName) ? Strings.DefaultFileName : suggestedFileName);

        if (target is null)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            await target.WriteAllBytesAsync(content);

            _notifications.Publish(verified
                ? StatusMessage.Success(Format(Strings.SavedFormat, target.Name))
                : StatusMessage.Warning(Format(Strings.SavedUnverifiedFormat, target.Name)));

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogError(ex, "Writing the recovered file failed.");
            _notifications.Publish(StatusMessage.Error(Format(Strings.SaveFailedFormat, ex.Message)));

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnNewSessionAsync()
    {
        // Only a session that holds something is worth guarding: confirming the loss of nothing is noise.
        if (HasAnyCode && !await _confirmations.ConfirmNewSessionAsync())
        {
            return;
        }

        _scanner.Stop();
        _session = new RecoverySession();
        _seen.Clear();
        SetUnverified(null);
        CheckVerdict = null;
        LastScan = null;
        CameraProblem = null;
        State = ScanState.Ready;
        RefreshStatus();

        // A new session is for scanning, and for the same purpose as the last — checking a stack of printouts one
        // after the other is the natural follow-up to a check. The camera comes straight back on.
        await StartCameraAsync();
    }

    private void SetUnverified(AssemblyResult? result)
    {
        _unverified = result;

        OnPropertyChanged(nameof(CanSaveAnyway));
        SaveAnywayCommand.NotifyCanExecuteChanged();
    }

    private void RefreshStatus()
    {
        _status = _session.Status;

        OnPropertyChanged(nameof(HasAnyCode));
        OnPropertyChanged(nameof(ShowIntroduction));
        OnPropertyChanged(nameof(CanStartCheck));
        StartCheckCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(BackupId));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(FileSizeText));
        OnPropertyChanged(nameof(ExpectedSha256));
        OnPropertyChanged(nameof(HasMetadata));
        OnPropertyChanged(nameof(CodesText));
        OnPropertyChanged(nameof(MetadataText));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(HasMissing));
        OnPropertyChanged(nameof(MissingText));
    }

    private void NotifyCommands()
    {
        StartScanningCommand.NotifyCanExecuteChanged();
        StartCheckCommand.NotifyCanExecuteChanged();
        StopScanningCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAnywayCommand.NotifyCanExecuteChanged();
        VerifyCommand.NotifyCanExecuteChanged();
        NewSessionCommand.NotifyCanExecuteChanged();
    }

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
