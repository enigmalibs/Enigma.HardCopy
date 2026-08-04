using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for the platform's file dialogs. Each answer is set before the act, and a
/// <see langword="null"/> or empty one models the user dismissing the dialog.
/// </summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    /// <summary>Gets or sets what the "choose the file to back up" dialog returns.</summary>
    internal IPickedFile? FileToBackUp { get; set; }

    /// <summary>Gets or sets what the "choose the scanned pages" dialog returns.</summary>
    internal IReadOnlyList<IPickedFile> Images { get; set; } = [];

    /// <summary>Gets or sets what the "save the PDF" dialog returns.</summary>
    internal ISaveTarget? PdfDestination { get; set; }

    /// <summary>Gets or sets what the "save the recovered file" dialog returns.</summary>
    internal ISaveTarget? RecoveredDestination { get; set; }

    /// <summary>Gets the file name the PDF dialog was last offered.</summary>
    internal string? SuggestedPdfName { get; private set; }

    /// <summary>Gets the file name the recovered-file dialog was last offered.</summary>
    internal string? SuggestedRecoveredName { get; private set; }

    /// <summary>Gets how many times the recovered-file dialog was opened.</summary>
    internal int RecoveredDestinationCalls { get; private set; }

    /// <inheritdoc/>
    public Task<IPickedFile?> ChooseFileToBackUpAsync() => Task.FromResult(FileToBackUp);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IPickedFile>> ChooseImagesAsync() => Task.FromResult(Images);

    /// <inheritdoc/>
    public Task<ISaveTarget?> ChoosePdfDestinationAsync(string suggestedFileName)
    {
        SuggestedPdfName = suggestedFileName;

        return Task.FromResult(PdfDestination);
    }

    /// <inheritdoc/>
    public Task<ISaveTarget?> ChooseRecoveredFileDestinationAsync(string suggestedFileName)
    {
        SuggestedRecoveredName = suggestedFileName;
        RecoveredDestinationCalls++;

        return Task.FromResult(RecoveredDestination);
    }
}
