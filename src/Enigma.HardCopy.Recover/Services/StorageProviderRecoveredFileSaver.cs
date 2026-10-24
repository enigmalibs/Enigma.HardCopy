using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Recover.Resources;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The <see cref="IRecoveredFileSaver"/> backed by the control library's file picker service — on Android, the
/// Storage Access Framework's document picker.
/// </summary>
/// <remarks>
/// <para>
/// The picker is what makes "on the phone or on Google Drive" one feature rather than two: every installed document
/// provider appears in it, and the application needs neither a storage permission nor any provider's own API.
/// </para>
/// <para>
/// The one file type offered is <c>application/octet-stream</c>. A recovered file can be anything, and a document
/// provider creating an octet stream keeps the suggested name exactly as given — a more specific MIME type invites
/// the provider to append an extension of its own choosing.
/// </para>
/// </remarks>
internal sealed class StorageProviderRecoveredFileSaver : IRecoveredFileSaver
{
    private readonly IFileDialogService _dialogs;

    /// <summary>Initializes a new instance of the <see cref="StorageProviderRecoveredFileSaver"/> class.</summary>
    /// <param name="dialogs">The library's picker service, whose storage provider the app hands over at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialogs"/> is <see langword="null"/>.</exception>
    public StorageProviderRecoveredFileSaver(IFileDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        _dialogs = dialogs;
    }

    /// <inheritdoc/>
    public async Task<ISaveTarget?> ChooseDestinationAsync(string suggestedFileName)
    {
        IStorageFile? file = await _dialogs.ShowSaveFileDialogAsync(new FilePickerSaveOptions
        {
            Title = Strings.SavePickerTitle,
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true,
            FileTypeChoices = [AnyFileType],
        });

        return file is null ? null : new StorageProviderSaveTarget(file);
    }

    private static FilePickerFileType AnyFileType => new(Strings.SavePickerFileType)
    {
        Patterns = ["*"],
        MimeTypes = ["application/octet-stream"],
    };
}
