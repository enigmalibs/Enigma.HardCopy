using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Asks the user for a file to read or a place to write, through the platform's own dialogs.
/// </summary>
/// <remarks>
/// No method takes a <see cref="System.Threading.CancellationToken"/>. A modal dialog is cancelled by the
/// user dismissing it — which is reported as <see langword="null"/> or an empty list — and there is nothing
/// for a token to cancel; accepting one would only promise something no platform picker can honour. The reads
/// and writes that follow do take a token, on <see cref="IPickedFile"/> and <see cref="ISaveTarget"/>.
/// </remarks>
public interface IFileDialogService
{
    /// <summary>Asks for the file to back up.</summary>
    /// <returns>The chosen file, or <see langword="null"/> if the user dismissed the dialog.</returns>
    Task<IPickedFile?> ChooseFileToBackUpAsync();

    /// <summary>Asks for the scanned or photographed pages to import.</summary>
    /// <returns>The chosen images, or an empty list if the user dismissed the dialog.</returns>
    Task<IReadOnlyList<IPickedFile>> ChooseImagesAsync();

    /// <summary>Asks where to write the backup PDF.</summary>
    /// <param name="suggestedFileName">The name to offer in the dialog.</param>
    /// <returns>The chosen destination, or <see langword="null"/> if the user dismissed the dialog.</returns>
    Task<ISaveTarget?> ChoosePdfDestinationAsync(string suggestedFileName);

    /// <summary>Asks where to write a recovered file.</summary>
    /// <param name="suggestedFileName">The name to offer in the dialog — the name recorded in the backup.</param>
    /// <returns>The chosen destination, or <see langword="null"/> if the user dismissed the dialog.</returns>
    Task<ISaveTarget?> ChooseRecoveredFileDestinationAsync(string suggestedFileName);
}
