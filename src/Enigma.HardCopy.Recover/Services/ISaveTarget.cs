using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The destination the user chose for the recovered file — on the phone, on Google Drive, or with any other
/// document provider.
/// </summary>
/// <remarks>
/// Abstract for the same reason as in the desktop app: a ViewModel decides <i>what</i> to write, and is tested on
/// that, without a platform storage provider in the way.
/// </remarks>
public interface ISaveTarget
{
    /// <summary>Gets the destination's name as the provider reports it — a leaf name, never a path.</summary>
    string Name { get; }

    /// <summary>Writes <paramref name="content"/>, replacing whatever was there.</summary>
    /// <param name="content">The bytes to write.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the bytes have been flushed.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.IO.IOException">The destination could not be written.</exception>
    /// <exception cref="System.UnauthorizedAccessException">The provider refused the write.</exception>
    Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default);
}
