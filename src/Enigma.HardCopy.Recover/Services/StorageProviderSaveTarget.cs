using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// An <see cref="ISaveTarget"/> over one of Avalonia's storage files — on Android, a document provider's URI.
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
    public async Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await using Stream stream = await _file.OpenWriteAsync();

        // Replacing an existing document has to shorten it too. Whether a provider's write stream opens truncated
        // is up to the provider, and a recovered key file with the tail of a longer, older file glued to its end
        // would be corrupt while looking like a success.
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
