using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Turns a file into the barcode strings that a PDF is composed from.
/// </summary>
public interface IBackupEncoder
{
    /// <summary>Encodes <paramref name="file"/> into the codes of one backup.</summary>
    /// <param name="file">The file's contents. Read to the end from its current position.</param>
    /// <param name="fileName">
    /// The file's name, recorded so a recovery can restore it. A leaf name, with no directory part.
    /// </param>
    /// <param name="options">The encoding options, or <see langword="null"/> for <see cref="EncodeOptions.Default"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The encoded backup.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">
    /// <paramref name="file"/> is not readable, or <paramref name="fileName"/> is <see langword="null"/>,
    /// empty or white space.
    /// </exception>
    /// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task<EncodedBackup> EncodeAsync(
        Stream file,
        string fileName,
        EncodeOptions? options = null,
        CancellationToken cancellationToken = default);
}
