using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IFileDialogService"/> backed by the platform's own pickers, through the main window's
/// <see cref="TopLevel.StorageProvider"/>.
/// </summary>
/// <remarks>
/// It depends on the window rather than on a storage provider because the provider is a property of a live
/// top-level: it exists once the window does, and taking the window lets this service be constructed at
/// startup while the provider is only fetched when a dialog is actually opened. The application has exactly
/// one window, so there is no ambiguity about which one owns the dialog.
/// </remarks>
internal sealed class StorageProviderFileDialogService : IFileDialogService
{
    private readonly Window _owner;

    internal StorageProviderFileDialogService(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
    }

    /// <inheritdoc/>
    public async Task<IPickedFile?> ChooseFileToBackUpAsync()
    {
        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.DialogChooseFileTitle,
            AllowMultiple = false,
        });

        return files.Count == 0 ? null : new StorageProviderFile(files[0]);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IPickedFile>> ChooseImagesAsync()
    {
        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
