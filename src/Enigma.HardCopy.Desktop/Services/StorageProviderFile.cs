using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// An <see cref="IPickedFile"/> over one of Avalonia's storage files.
/// </summary>
/// <remarks>
/// The one place in the application where a storage file is turned into something a ViewModel can read.
/// <see cref="Adapt"/> is what the drag-and-drop handler uses: a drop carries
/// <see cref="IStorageItem"/> values, and folders among them are dropped silently — a directory is not a
/// scanned page, and refusing the whole gesture over one would be unhelpful.
/// </remarks>
internal sealed class StorageProviderFile : IPickedFile
{
    private readonly IStorageFile _file;

    internal StorageProviderFile(IStorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        _file = file;
    }

    /// <inheritdoc/>
    public string Name => _file.Name;

    /// <summary>Wraps every file among <paramref name="items"/>, ignoring anything that is not one.</summary>
    /// <param name="items">The dropped or picked storage items.</param>
    /// <returns>One <see cref="IPickedFile"/> per file, in the order given.</returns>
    internal static IReadOnlyList<IPickedFile> Adapt(IEnumerable<IStorageItem>? items)
    {
        if (items is null)
        {
            return [];
        }

        List<IPickedFile> files = [];
        foreach (IStorageItem item in items)
        {
            if (item is IStorageFile file)
            {
                files.Add(new StorageProviderFile(file));
            }
        }

        return files;
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        await using Stream stream = await _file.OpenReadAsync();
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken);

        return buffer.ToArray();
    }
}
