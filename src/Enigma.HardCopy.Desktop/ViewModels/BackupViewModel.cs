using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// The backup page: choose a file, choose how densely to pack the codes, choose where the PDF goes, and
/// generate it.
/// </summary>
/// <remarks>
/// <para>
/// The chosen file is held in memory from the moment it is picked. That is what lets the page show the file's
/// SHA-256 and its page estimate <i>before</i> anything is generated — the two things a user needs in order to
/// decide whether to print at all — and it costs nothing that the encoder was not going to pay anyway, since
/// it reads its input to the end.
/// </para>
/// <para>
/// Progress is reported by stage rather than by percentage. Core's encoder and composer are single calls with
/// no progress callback, so a percentage would be invented; naming the stage is honest and is what makes the
/// wait legible.
/// </para>
/// <para>
/// Every command handler runs on the UI thread and moves the CPU-bound work — encoding, composing — onto a
/// background task, so the state mutated after each <c>await</c> is still touched from the UI thread and needs
/// no marshaling.
/// </para>
/// </remarks>
public sealed class BackupViewModel : ObservableObject
{
    /// <summary>
    /// The page count above which the user is warned. Not a limit — there is deliberately none — but the point
    /// where printing and re-scanning stops being a five-minute job and the user should know it before they
    /// start.
    /// </summary>
    public const int LargeInputPageThreshold = 20;

    private readonly IBackupEncoder _encoder;
    private readonly IPdfComposer _composer;
    private readonly IFileDialogService _dialogs;
    private readonly ILogger<BackupViewModel> _logger;

    private SourceFile? _source;
    private ISaveTarget? _output;
    private CancellationTokenSource? _generation;

    /// <summary>Initializes a new instance of the <see cref="BackupViewModel"/> class.</summary>
    /// <param name="encoder">Turns the chosen file into code strings.</param>
    /// <param name="composer">Turns those codes into the printable PDF.</param>
    /// <param name="dialogs">Asks the user for the file and the destination.</param>
    /// <param name="logger">Records failures for diagnosis; the user sees the friendly message instead.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public BackupViewModel(
        IBackupEncoder encoder,
        IPdfComposer composer,
        IFileDialogService dialogs,
        ILogger<BackupViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(logger);

        _encoder = encoder;
        _composer = composer;
        _dialogs = dialogs;
        _logger = logger;

        ChooseFileCommand = new AsyncRelayCommand(OnChooseFileAsync, () => !IsBusy);
        ChooseOutputCommand = new AsyncRelayCommand(OnChooseOutputAsync, () => !IsBusy);
        GenerateCommand = new AsyncRelayCommand(OnGenerateAsync, CanGenerate);
        CancelCommand = new RelayCommand(OnCancel, () => IsBusy);
    }

    /// <summary>Gets the barcode-density options offered.</summary>
    public IReadOnlyList<ChunkSizeOption> ChunkSizes => ChunkSizeOption.All;

    /// <summary>
    /// Gets or sets the chosen barcode density. Session-only: it is deliberately not persisted, so every run
    /// starts from the default that the page layout is tuned for.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public ChunkSizeOption SelectedChunkSize
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (SetProperty(ref field, value))
            {
                UpdateEstimate();
            }
        }
    } = ChunkSizeOption.Default;

    /// <summary>Gets a value indicating whether a file has been chosen.</summary>
    public bool HasFile => _source is not null;

    /// <summary>Gets the chosen file's name, or <see langword="null"/> while none is chosen.</summary>
    public string? FileName => _source?.Name;

    /// <summary>Gets the chosen file's size as text, or <see langword="null"/> while none is chosen.</summary>
    public string? SizeText => _source is null
        ? null
        : string.Format(CultureInfo.CurrentCulture, Strings.BackupBytesFormat, _source.Content.Length);

    /// <summary>
    /// Gets the SHA-256 of the chosen file as lower-case hexadecimal — the same value the recovery will check
    /// against, so it can be compared by eye with <c>sha256sum</c>.
    /// </summary>
    public string? Sha256Hex => _source?.Sha256Hex;

    /// <summary>Gets the code and page estimate, or <see langword="null"/> while no file is chosen.</summary>
    public string? EstimateText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the warning about a file that will run to many pages, or <see langword="null"/> when there is
    /// nothing to warn about.
    /// </summary>
    public string? LargeInputWarning
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasLargeInputWarning));
            }
        }
    }

    /// <summary>Gets a value indicating whether the page count is worth warning about.</summary>
    public bool HasLargeInputWarning => LargeInputWarning is not null;

    /// <summary>Gets a value indicating whether a destination has been chosen.</summary>
    public bool HasOutput => _output is not null;

    /// <summary>Gets the chosen destination's path, or <see langword="null"/> while none is chosen.</summary>
    public string? OutputLocation => _output?.Location;

    /// <summary>
    /// Gets a value indicating whether a long operation is running. Every command is disabled while it is,
    /// except cancellation.
    /// </summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                ChooseFileCommand.NotifyCanExecuteChanged();
                ChooseOutputCommand.NotifyCanExecuteChanged();
                GenerateCommand.NotifyCanExecuteChanged();
                CancelCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the stage currently running, or <see langword="null"/> when nothing is.</summary>
    public string? ProgressText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the outcome of the last thing the user asked for, or <see langword="null"/>.</summary>
    public StatusMessage? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    /// <summary>Gets a value indicating whether there is a message to show.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets the command that asks for the file to back up.</summary>
    public AsyncRelayCommand ChooseFileCommand { get; }

    /// <summary>Gets the command that asks where to write the PDF.</summary>
    public AsyncRelayCommand ChooseOutputCommand { get; }

    /// <summary>Gets the command that produces and writes the PDF.</summary>
    public AsyncRelayCommand GenerateCommand { get; }

    /// <summary>Gets the command that cancels the running generation.</summary>
    public RelayCommand CancelCommand { get; }

    private bool CanGenerate() => !IsBusy && _source is not null && _output is not null;

    private async Task OnChooseFileAsync()
    {
        IPickedFile? file = await _dialogs.ChooseFileToBackUpAsync();
        if (file is null)
        {
            return;
        }

        IsBusy = true;
        ProgressText = Strings.BackupStageReading;
        Message = null;
        try
        {
            byte[] content = await file.ReadAllBytesAsync();
            string sha256Hex = await Task.Run(() => Convert.ToHexStringLower(SHA256.HashData(content)));

            SetSource(new SourceFile(file.Name, content, sha256Hex));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogError(ex, "Reading the file to back up failed.");
            SetSource(null);
            Message = StatusMessage.Error(Format(Strings.BackupReadFailedFormat, ex.Message));
        }
        finally
        {
            IsBusy = false;
            ProgressText = null;
        }
    }

    private async Task OnChooseOutputAsync()
    {
        string suggested = _source is null
            ? string.Empty
            : Format(Strings.BackupDefaultPdfNameFormat, Path.GetFileNameWithoutExtension(_source.Name));

        ISaveTarget? target = await _dialogs.ChoosePdfDestinationAsync(suggested);
        if (target is null)
        {
            return;
        }

        Message = null;
        SetOutput(target);
    }

    private async Task OnGenerateAsync()
    {
        if (_source is not SourceFile source || _output is not ISaveTarget output)
        {
            return;
        }

        using CancellationTokenSource cancellation = new();
        _generation = cancellation;
        CancellationToken token = cancellation.Token;

        IsBusy = true;
        Message = null;
        try
        {
            ProgressText = Strings.BackupStageEncoding;
            EncodeOptions encodeOptions = EncodeOptions.FromPreset(SelectedChunkSize.Preset);
            EncodedBackup backup = await Task.Run(
                async () =>
                {
                    using MemoryStream stream = new(source.Content, writable: false);

                    return await _encoder.EncodeAsync(stream, source.Name, encodeOptions, token);
                },
                token);

            ProgressText = Strings.BackupStageComposing;
            (byte[] pdf, int pageCount) = await Task.Run(
                () =>
                {
                    PageLayout layout = PageLayout.ForBackup(backup, PdfOptions.Default);

                    return (_composer.Compose(backup, PdfOptions.Default, token), layout.PageCount);
                },
                token);

            ProgressText = Strings.BackupStageWriting;
            await output.WriteAllBytesAsync(pdf, token);

            Message = StatusMessage.Success(
                Format(Strings.BackupSuccessFormat, output.Name, backup.Codes.Count, pageCount));
        }
        catch (OperationCanceledException)
        {
            Message = StatusMessage.Information(Strings.BackupCancelled);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Encoding and composition are entirely in memory, so an I/O failure can only be the write.
            _logger.LogError(ex, "Writing the backup PDF failed.");
            Message = StatusMessage.Error(Format(Strings.BackupWriteFailedFormat, ex.Message));
        }
        catch (Exception ex)
        {
            // A backup that cannot be produced must be reported, not swallowed and not allowed to take the
            // application down with it — this is the outermost frame of a user-initiated operation.
            _logger.LogError(ex, "Producing the backup PDF failed.");
            Message = StatusMessage.Error(Format(Strings.BackupGenerateFailedFormat, ex.Message));
        }
        finally
        {
            _generation = null;
            IsBusy = false;
            ProgressText = null;
        }
    }

    private void OnCancel() => _generation?.Cancel();

    private void SetSource(SourceFile? source)
    {
        _source = source;

        OnPropertyChanged(nameof(HasFile));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(Sha256Hex));
        GenerateCommand.NotifyCanExecuteChanged();
        UpdateEstimate();
    }

    private void SetOutput(ISaveTarget? output)
    {
        _output = output;

        OnPropertyChanged(nameof(HasOutput));
        OnPropertyChanged(nameof(OutputLocation));
        GenerateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Recomputes the code and page estimate. Worst case by construction — <see cref="PageLayout.Estimate"/>
    /// assumes the file does not compress — so the printed document never needs more pages than were promised.
    /// </summary>
    private void UpdateEstimate()
    {
        if (_source is null)
        {
            EstimateText = null;
            LargeInputWarning = null;
            return;
        }

        try
        {
            PageLayout layout = PageLayout.Estimate(
                _source.Content.Length,
                EncodeOptions.FromPreset(SelectedChunkSize.Preset));

            EstimateText = Format(Strings.BackupEstimateFormat, layout.CodeCount, layout.PageCount);
            LargeInputWarning = layout.PageCount >= LargeInputPageThreshold
                ? Format(Strings.BackupLargeInputWarningFormat, layout.PageCount)
                : null;
        }
        catch (ArgumentException ex)
        {
            // A density and a page size that cannot be reconciled — reported where the estimate would go,
            // rather than left to fail later at the point of generation.
            _logger.LogError(ex, "Estimating the page count failed.");
            EstimateText = null;
            LargeInputWarning = Format(Strings.BackupEstimateFailedFormat, ex.Message);
        }
    }

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);

    /// <summary>The chosen file, as everything downstream of the picker needs it.</summary>
    private sealed record SourceFile(string Name, byte[] Content, string Sha256Hex);
}
