using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// The recovery page: feed codes in from scanned images or from the keyboard until nothing is missing, then
/// rebuild the file and check it against the hash the backup recorded.
/// </summary>
/// <remarks>
/// <para>
/// Both input paths — imported images and typed text — feed <b>one</b> <see cref="RecoverySession"/>, which is
/// what makes a mixed recovery work: eleven codes off a photograph and the twelfth typed in by hand is the
/// normal case when a symbol refuses to read.
/// </para>
/// <para>
/// <b>A hash mismatch never saves silently.</b> The rebuilt bytes are held back, the two hashes are shown side
/// by side, and only an explicit second action writes them — under a message that says they are not the file
/// that was backed up. That refusal is the point of the whole application, so it is a state of this ViewModel
/// rather than a detail of a dialog. Since the second action is still one click, it is now also guarded by a
/// confirmation that names both hashes; the guard is asked <i>before</i> the destination picker, so declining
/// costs nothing and leaves the offer exactly where it was.
/// </para>
/// <para>
/// Command handlers run on the UI thread and push the CPU-bound work — barcode reading, decompression,
/// hashing — onto background tasks. The state and the activity log are therefore only ever touched from the UI
/// thread, and need no marshaling.
/// </para>
/// </remarks>
public sealed class RecoverViewModel : ObservableObject
{
    /// <summary>
    /// How many missing indexes are listed before the rest are summarised as a count. A user hunting for
    /// missing pages can act on a dozen numbers; a list of four hundred is just noise.
    /// </summary>
    public const int MaxListedMissingIndexes = 12;

    /// <summary>
    /// How many activity entries are kept. Importing a large backup produces one line per code, and the
    /// interesting ones are the recent ones — the collection is trimmed from the far end rather than growing
    /// without limit.
    /// </summary>
    public const int MaxActivityEntries = 500;

    private readonly IFileDialogService _dialogs;
    private readonly ILogger<RecoverViewModel> _logger;
    private readonly IProgressOverlay _overlay;
    private readonly INotificationService _notifications;
    private readonly IConfirmationService _confirmations;

    private RecoverySession _session = new();
    private RecoveryStatus _status;
    private AssemblyResult? _unverified;

    /// <summary>Initializes a new instance of the <see cref="RecoverViewModel"/> class.</summary>
    /// <param name="dialogs">Asks the user for the images to import and where to write the recovered file.</param>
    /// <param name="logger">Records failures for diagnosis; the user sees the friendly message instead.</param>
    /// <param name="overlay">Puts a long run behind a modal card carrying its stage.</param>
    /// <param name="notifications">Carries each outcome to the window's info bar.</param>
    /// <param name="confirmations">Asks before the two things a recovery cannot take back.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RecoverViewModel(
        IFileDialogService dialogs,
        ILogger<RecoverViewModel> logger,
        IProgressOverlay overlay,
        INotificationService notifications,
        IConfirmationService confirmations)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(confirmations);

        _dialogs = dialogs;
        _logger = logger;
        _overlay = overlay;
        _notifications = notifications;
        _confirmations = confirmations;
        _status = _session.Status;

        ImportImagesCommand = new AsyncRelayCommand(OnImportImagesAsync, () => !IsBusy);
        ImportFilesCommand = new AsyncRelayCommand<IReadOnlyList<IPickedFile>>(OnImportFilesAsync, _ => !IsBusy);
        AddCodesCommand = new RelayCommand(OnAddCodes, CanAddCodes);
        AssembleCommand = new AsyncRelayCommand(OnAssembleAsync, CanAssemble);
        SaveAnywayCommand = new AsyncRelayCommand(OnSaveAnywayAsync, () => !IsBusy && _unverified is not null);
        StartOverCommand = new AsyncRelayCommand(OnStartOverAsync, () => !IsBusy);
    }

    /// <summary>Gets what has happened so far, newest first.</summary>
    public ObservableCollection<StatusMessage> Activity { get; } = [];

    /// <summary>Gets or sets the code text typed or pasted by the user.</summary>
    public string? ManualEntry
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AddCodesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets a value indicating whether any code has been accepted yet.</summary>
    public bool HasAnyCode => _status.BackupId is not null;

    /// <summary>Gets the backup ID the session adopted, or <see langword="null"/> while it has none.</summary>
    public string? BackupId => _status.BackupId;

    /// <summary>Gets a value indicating whether the metadata code has been read.</summary>
    public bool HasMetadata => _status.HasMetadata;

    /// <summary>Gets the name recorded in the backup, or <see langword="null"/> while it is unknown.</summary>
    public string? FileName => _status.Metadata?.FileName;

    /// <summary>Gets the hash recorded in the backup, or <see langword="null"/> while it is unknown.</summary>
    public string? ExpectedSha256 => _status.Metadata?.Sha256Hex;

    /// <summary>Gets how many data codes are in, against how many the backup is made of.</summary>
    public string CodesText => _status.TotalChunks is int total
        ? Format(Strings.RecoverChunksFormat, _status.ReceivedChunks, total)
        : Strings.RecoverChunksUnknown;

    /// <summary>Gets the data codes still missing, truncated to <see cref="MaxListedMissingIndexes"/>.</summary>
    public string MissingText
    {
        get
        {
            if (_status.TotalChunks is null)
            {
                return Strings.RecoverMissingNothingKnown;
            }

            IReadOnlyList<int> missing = _status.MissingIndexes;
            if (missing.Count == 0)
            {
                return Strings.RecoverMissingNone;
            }

            int listed = Math.Min(missing.Count, MaxListedMissingIndexes);
            string head = string.Join(", ", missing.Take(listed));

            return listed == missing.Count
                ? head
                : Format(Strings.RecoverMissingOverflowFormat, head, missing.Count - listed);
        }
    }

    /// <summary>Gets how far the recovery has got, from 0 to 100.</summary>
    public double ProgressPercent => _status.TotalChunks is int total and > 0
        ? Math.Min(_status.ReceivedChunks * 100d / total, 100d)
        : 0d;

    /// <summary>Gets a value indicating whether everything needed to rebuild the file is present.</summary>
    public bool IsComplete => _status.IsComplete;

    /// <summary>
    /// Gets a value indicating whether a rebuilt-but-unverified file is waiting for the user to decide about
    /// it. This is what puts the "save anyway" escape hatch on screen, and it is deliberately never set
    /// without a message explaining what the bytes are.
    /// </summary>
    public bool CanSaveAnyway => _unverified is not null;

    /// <summary>Gets a value indicating whether a long operation is running.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                ImportImagesCommand.NotifyCanExecuteChanged();
                ImportFilesCommand.NotifyCanExecuteChanged();
                AddCodesCommand.NotifyCanExecuteChanged();
                AssembleCommand.NotifyCanExecuteChanged();
                SaveAnywayCommand.NotifyCanExecuteChanged();
                StartOverCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the stage currently running, or <see langword="null"/> when nothing is.</summary>
    /// <remarks>
    /// Setting it also writes the stage onto the overlay card. Clearing it does not: the card is about to be
    /// taken down by the same <c>finally</c> that cleared this.
    /// </remarks>
    public string? ProgressText
    {
        get;
        private set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                _overlay.Update(value);
            }
        }
    }

    /// <summary>Gets the outcome of the last thing the user asked for, or <see langword="null"/>.</summary>
    /// <remarks>
    /// Setting it to an outcome also publishes that outcome as a notification — exactly once, because this is
    /// the single place the page records one. Clearing it publishes nothing, and the activity log is
    /// deliberately not published: it is a record to scroll, not a banner.
    /// </remarks>
    public StatusMessage? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasMessage));

                if (value is not null)
                {
                    _notifications.Publish(value);
                }
            }
        }
    }

    /// <summary>Gets a value indicating whether there is a message to show.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets the command that asks for scanned pages to import.</summary>
    public AsyncRelayCommand ImportImagesCommand { get; }

    /// <summary>
    /// Gets the command that imports files the user already chose — what a drag-and-drop gesture invokes, the
    /// picker having been bypassed.
    /// </summary>
    public AsyncRelayCommand<IReadOnlyList<IPickedFile>> ImportFilesCommand { get; }

    /// <summary>Gets the command that offers the typed text to the session.</summary>
    public RelayCommand AddCodesCommand { get; }

    /// <summary>Gets the command that rebuilds the file and, when it verifies, saves it.</summary>
    public AsyncRelayCommand AssembleCommand { get; }

    /// <summary>Gets the command that writes a rebuilt file whose hash did not match.</summary>
    public AsyncRelayCommand SaveAnywayCommand { get; }

    /// <summary>Gets the command that discards the session and starts a new recovery.</summary>
    public AsyncRelayCommand StartOverCommand { get; }

    /// <summary>
    /// Splits typed or pasted text into candidate codes, on the leading <c>EHC1</c> of each.
    /// </summary>
    /// <param name="text">The text as the user entered it.</param>
    /// <returns>One entry per code found, in order. Empty when the text holds no code at all.</returns>
    /// <remarks>
    /// Splitting on line breaks would be wrong in both directions: a single code pasted out of a text editor
    /// arrives wrapped across several lines, and several codes arrive one per line. The format's own prefix is
    /// the only boundary that is right in both cases. Whatever whitespace and case survive the split are
    /// normalized by the session.
    /// </remarks>
    public static IReadOnlyList<string> SplitCodes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        List<string> codes = [];
        int start = text.IndexOf(HardCopyFormat.Prefix, StringComparison.OrdinalIgnoreCase);
        while (start >= 0)
        {
            int next = text.IndexOf(
                HardCopyFormat.Prefix,
                start + HardCopyFormat.Prefix.Length,
                StringComparison.OrdinalIgnoreCase);

            codes.Add(next < 0 ? text[start..] : text[start..next]);
            start = next;
        }

        return codes;
    }

    private bool CanAddCodes() => !IsBusy && !string.IsNullOrWhiteSpace(ManualEntry);

    private bool CanAssemble() => !IsBusy && IsComplete;

    private async Task OnImportImagesAsync()
    {
        IReadOnlyList<IPickedFile> images = await _dialogs.ChooseImagesAsync();

        await ImportAsync(images);
    }

    private Task OnImportFilesAsync(IReadOnlyList<IPickedFile>? files) => ImportAsync(files ?? []);

    private async Task ImportAsync(IReadOnlyList<IPickedFile> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        DiscardUnverified();
        IsBusy = true;
        Message = null;

        // Raised before the stage is named: an Update that arrives before the card does is a no-op. No cancel
        // button — importing takes no cancellation token, and offering a dead one would be worse than none.
        await _overlay.ShowAsync(Strings.OverlayImportTitle);
        ProgressText = Strings.RecoverStageImporting;
        try
        {
            foreach (IPickedFile image in images)
            {
                await ImportOneAsync(image);
            }
        }
        finally
        {
            IsBusy = false;
            ProgressText = null;
            RefreshStatus();
            await _overlay.HideAsync();
        }
    }

    private async Task ImportOneAsync(IPickedFile image)
    {
        try
        {
            byte[] bytes = await image.ReadAllBytesAsync();
            ImageScanResult scan = await Task.Run(() =>
            {
                using MemoryStream stream = new(bytes, writable: false);

                return _session.AddImage(stream);
            });

            Append(Summarize(image.Name, scan));
            foreach (AddCodeResult result in scan.Results)
            {
                Append(OutcomeMessages.Describe(result));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // One unreadable file out of a batch must not abandon the rest: the user selected a folder full of
            // scans, and the other pages still advance the recovery.
            _logger.LogError(ex, "Reading the imported image {Image} failed.", image.Name);
            Append(StatusMessage.Error(Format(Strings.RecoverImageFailedFormat, image.Name, ex.Message)));
        }
    }

    private void OnAddCodes()
    {
        IReadOnlyList<string> codes = SplitCodes(ManualEntry);
        if (codes.Count == 0)
        {
            Message = StatusMessage.Error(string.IsNullOrWhiteSpace(ManualEntry)
                ? Strings.RecoverManualEmpty
                : Strings.RecoverManualNoCodesFound);
            return;
        }

        DiscardUnverified();
        Message = null;
        foreach (string code in codes)
        {
            Append(OutcomeMessages.Describe(_session.AddCode(code)));
        }

        ManualEntry = null;
        RefreshStatus();
    }

    private async Task OnAssembleAsync()
    {
        IsBusy = true;
        Message = null;

        await _overlay.ShowAsync(Strings.OverlayAssembleTitle);
        ProgressText = Strings.RecoverStageAssembling;

        AssemblyResult result;
        try
        {
            result = await Task.Run(_session.TryAssemble);
        }
        finally
        {
            IsBusy = false;
            ProgressText = null;
            await _overlay.HideAsync();
        }

        StatusMessage described = OutcomeMessages.Describe(result);
        Append(described);

        if (result is { Outcome: AssemblyOutcome.Verified, Content: byte[] content })
        {
            if (!await SaveAsync(content, result.Metadata?.FileName, verified: true) && Message is null)
            {
                Message = described;
            }

            return;
        }

        if (result.Outcome is AssemblyOutcome.HashMismatch)
        {
            SetUnverified(result);
        }

        Message = described;
    }

    private async Task OnSaveAnywayAsync()
    {
        if (_unverified is not { Content: byte[] content } result)
        {
            return;
        }

        // Asked before the destination picker, so declining costs the user nothing and leaves the offer
        // standing. Only an explicit agreement gets past here — a dismissed dialog is a refusal.
        bool agreed = await _confirmations.ConfirmUnverifiedSaveAsync(
            result.Metadata?.Sha256Hex ?? string.Empty,
            result.Sha256Hex ?? string.Empty);

        if (!agreed)
        {
            return;
        }

        if (await SaveAsync(content, result.Metadata?.FileName, verified: false))
        {
            // The decision has been made and acted on; leaving the button there would only invite a second
            // copy of the same unverified bytes.
            DiscardUnverified();
        }
    }

    private async Task<bool> SaveAsync(byte[] content, string? suggestedFileName, bool verified)
    {
        ISaveTarget? target = await _dialogs.ChooseRecoveredFileDestinationAsync(
            string.IsNullOrWhiteSpace(suggestedFileName) ? Strings.RecoverDefaultFileName : suggestedFileName);

        if (target is null)
        {
            return false;
        }

        IsBusy = true;
        await _overlay.ShowAsync(Strings.OverlaySaveTitle);
        ProgressText = Strings.RecoverStageWriting;
        try
        {
            await target.WriteAllBytesAsync(content);

            StatusMessage saved = verified
                ? StatusMessage.Success(Format(Strings.RecoverSavedFormat, target.Name))
                : StatusMessage.Warning(Format(Strings.RecoverSavedUnverifiedFormat, target.Name));

            Append(saved);
            Message = saved;

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogError(ex, "Writing the recovered file failed.");
            StatusMessage failed = StatusMessage.Error(Format(Strings.RecoverWriteFailedFormat, ex.Message));
            Append(failed);
            Message = failed;

            return false;
        }
        finally
        {
            IsBusy = false;
            ProgressText = null;
            await _overlay.HideAsync();
        }
    }

    private async Task OnStartOverAsync()
    {
        // Only a session that has actually read something is worth guarding: asking to confirm the discarding
        // of nothing is noise, and this button is the one a user reaches for between two recoveries.
        if (HasAnyCode && !await _confirmations.ConfirmStartOverAsync())
        {
            return;
        }

        _session = new RecoverySession();
        _unverified = null;
        Activity.Clear();
        ManualEntry = null;
        Message = null;

        OnPropertyChanged(nameof(CanSaveAnyway));
        SaveAnywayCommand.NotifyCanExecuteChanged();
        RefreshStatus();
    }

    private static StatusMessage Summarize(string name, ImageScanResult scan)
    {
        if (!scan.IsImageReadable)
        {
            return StatusMessage.Error(Format(Strings.RecoverImageUnreadableFormat, name));
        }

        if (scan.CodesFound == 0)
        {
            return StatusMessage.Warning(Format(Strings.RecoverImageNoCodesFormat, name));
        }

        string text = Format(Strings.RecoverImageScannedFormat, name, scan.CodesFound, scan.AcceptedCount);

        return scan.AcceptedCount > 0 ? StatusMessage.Success(text) : StatusMessage.Information(text);
    }

    private void Append(StatusMessage entry)
    {
        Activity.Insert(0, entry);
        while (Activity.Count > MaxActivityEntries)
        {
            Activity.RemoveAt(Activity.Count - 1);
        }
    }

    private void SetUnverified(AssemblyResult result)
    {
        _unverified = result;

        OnPropertyChanged(nameof(CanSaveAnyway));
        SaveAnywayCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Forgets a rebuilt-but-unverified file. Any new code invalidates the verdict that produced it, so the
    /// escape hatch closes again rather than offering bytes that were rebuilt from a different set of codes.
    /// </summary>
    private void DiscardUnverified()
    {
        if (_unverified is null)
        {
            return;
        }

        _unverified = null;

        OnPropertyChanged(nameof(CanSaveAnyway));
        SaveAnywayCommand.NotifyCanExecuteChanged();
    }

    private void RefreshStatus()
    {
        _status = _session.Status;

        OnPropertyChanged(nameof(HasAnyCode));
        OnPropertyChanged(nameof(BackupId));
        OnPropertyChanged(nameof(HasMetadata));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(ExpectedSha256));
        OnPropertyChanged(nameof(CodesText));
        OnPropertyChanged(nameof(MissingText));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(IsComplete));
        AssembleCommand.NotifyCanExecuteChanged();
    }

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
