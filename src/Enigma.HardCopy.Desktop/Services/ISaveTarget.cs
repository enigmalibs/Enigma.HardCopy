using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// A destination the user chose to write to — the backup PDF, or a recovered file.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="IPickedFile"/>, and abstract for the same reason: a ViewModel decides
/// <i>what</i> to write, and is tested on that, without a windowing platform in the way.
/// </remarks>
public interface ISaveTarget
{
    /// <summary>Gets the destination's name — a leaf name, with no directory part.</summary>
    string Name { get; }

    /// <summary>
    /// Gets the destination's full path when it has one, and its <see cref="Name"/> when it does not. Shown
    /// to the user, never parsed.
    /// </summary>
    string Location { get; }

    /// <summary>Writes <paramref name="content"/>, replacing whatever was there.</summary>
    /// <param name="content">The bytes to write.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the bytes have been flushed.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.IO.IOException">The destination could not be written.</exception>
    /// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default);
}
