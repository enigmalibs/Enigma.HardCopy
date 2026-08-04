using System.Threading;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Turns an encoded backup into the printable PDF that is the actual backup.
/// </summary>
public interface IPdfComposer
{
    /// <summary>Composes the A4 document for <paramref name="backup"/>.</summary>
    /// <param name="backup">The backup to print.</param>
    /// <param name="options">The composition options, or <see langword="null"/> for <see cref="PdfOptions.Default"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The PDF file's bytes.</returns>
    /// <remarks>
    /// This is deliberately synchronous: nothing here waits on I/O, it is CPU-bound from end to end, and
    /// wrapping that in a <see cref="System.Threading.Tasks.Task"/> would only disguise which thread pays
    /// for it. Choosing the thread is the caller's business — a UI caller runs this on a background task
    /// and passes the token its cancel button owns, which is honoured between symbols.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="backup"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">
    /// <paramref name="backup"/> holds no codes, or its longest code cannot be printed under
    /// <paramref name="options"/> — see <see cref="PageLayout.ForBackup"/>.
    /// </exception>
    /// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    byte[] Compose(EncodedBackup backup, PdfOptions? options = null, CancellationToken cancellationToken = default);
}
