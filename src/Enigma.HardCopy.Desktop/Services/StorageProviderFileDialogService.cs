using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Enigma.HardCopy.Desktop.Resources;
using LibraryFileDialogs = Enigma.Avalonia.Desktop.Services.IFileDialogService;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IFileDialogService"/> backed by the platform's own pickers, through the control library's
/// picker service.
/// </summary>
/// <remarks>
/// <para>
/// The library's service is reached through an alias because it shares this interface's simple name: inside
/// this namespace a bare <c>IFileDialogService</c> is always the application's own, and the library's has to be
/// named some other way.
/// </para>
/// <para>
/// Both layers of the library's API were considered and the <b>options-object</b> overloads are used
/// deliberately. Its string-path overloads project the selection through <c>TryGetLocalPath()</c> and silently
/// drop every item without one, and this application needs the <see cref="IStorageFile"/> handles anyway: a
/// picked file is read as a stream, and the drop handler on the recovery page adapts the very same handles.
/// </para>
/// </remarks>
internal sealed class StorageProviderFileDialogService : IFileDialogService
{
    private readonly LibraryFileDialogs _dialogs;

    internal StorageProviderFileDialogService(LibraryFileDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        _dialogs = dialogs;
    }

    /// <inheritdoc/>
    public async Task<IPickedFile?> ChooseFileToBackUpAsync()
    {
        IReadOnlyList<IStorageFile> files = await _dialogs.ShowOpenFileDialogAsync(new FilePickerOpenOptions
        {
            Title = Strings.DialogChooseFileTitle,
            AllowMultiple = false,
        });

        return files.Count == 0 ? null : new StorageProviderFile(files[0]);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IPickedFile>> ChooseImagesAsync()
    {
        IReadOnlyList<IStorageFile> files = await _dialogs.ShowOpenFileDialogAsync(new FilePickerOpenOptions
        {
            Title = Strings.DialogChooseImagesTitle,
            AllowMultiple = true,
            FileTypeFilter = [ImageFileType, AllFilesFileType],
        });

        return StorageProviderFile.Adapt(files);
    }

    /// <inheritdoc/>
    public async Task<ISaveTarget?> ChoosePdfDestinationAsync(string suggestedFileName)
    {
        IStorageFile? file = await _dialogs.ShowSaveFileDialogAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogSavePdfTitle,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            ShowOverwritePrompt = true,
            FileTypeChoices = [PdfFileType],
        });

        return file is null ? null : new StorageProviderSaveTarget(file);
    }

    /// <inheritdoc/>
    public async Task<ISaveTarget?> ChooseRecoveredFileDestinationAsync(string suggestedFileName)
    {
        IStorageFile? file = await _dialogs.ShowSaveFileDialogAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogSaveRecoveredTitle,
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true,
        });

        return file is null ? null : new StorageProviderSaveTarget(file);
    }

    /// <summary>
    /// The image formats a recovery accepts — what SkiaSharp decodes and a scanner or phone produces. The
    /// unrestricted filter is offered alongside it because a scanner that names its output oddly should not
    /// make the file unpickable.
    /// </summary>
    private static FilePickerFileType ImageFileType => new(Strings.DialogFilterImages)
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"],
        MimeTypes = ["image/png", "image/jpeg", "image/bmp"],
        AppleUniformTypeIdentifiers = ["public.image"],
    };

    private static FilePickerFileType PdfFileType => new(Strings.DialogFilterPdf)
    {
        Patterns = ["*.pdf"],
        MimeTypes = ["application/pdf"],
        AppleUniformTypeIdentifiers = ["com.adobe.pdf"],
    };

    private static FilePickerFileType AllFilesFileType => new(Strings.DialogFilterAllFiles)
    {
        Patterns = ["*"],
    };
}
