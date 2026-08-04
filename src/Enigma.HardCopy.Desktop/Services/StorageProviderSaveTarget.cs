using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// An <see cref="ISaveTarget"/> over one of Avalonia's storage files.
/// </summary>
internal sealed class StorageProviderSaveTarget : ISaveTarget
{
    private readonly IStorageFile _file;

    internal StorageProviderSaveTarget(IStorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        _file = file;
    }

    /// <inheritdoc/>
    public string Name => _file.Name;

    /// <inheritdoc/>
    public string Location => _file.TryGetLocalPath() ?? _file.Name;

    /// <inheritdoc/>
    public async Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await using Stream stream = await _file.OpenWriteAsync();

        // Overwriting an existing file has to shorten it too. Whether the platform's write stream already
        // opened truncated is not documented, and a PDF left with a previous, longer file's tail glued to its
        // end would be a corrupt backup that still looked like a successful one.
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
