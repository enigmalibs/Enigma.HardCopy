using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// A file the user pointed the application at — chosen in a picker, or dropped onto a view.
/// </summary>
/// <remarks>
/// This exists so a ViewModel never touches Avalonia's storage types. A ViewModel that took an
/// <c>IStorageFile</c> could only be tested by standing up a windowing platform; one that takes this can be
/// tested with a few bytes in memory, which is the whole point.
/// <para>
/// The content is handed over as a single array rather than a stream: this application backs up small
/// critical files, the encoder reads its input to the end anyway, and having the bytes up front is what lets
/// the view show a hash and a page estimate before anything is generated.
/// </para>
/// </remarks>
public interface IPickedFile
{
    /// <summary>Gets the file's name — a leaf name, with no directory part.</summary>
    string Name { get; }

    /// <summary>Reads the whole file.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The file's bytes.</returns>
    /// <exception cref="System.IO.IOException">The file could not be read.</exception>
    /// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default);
}
