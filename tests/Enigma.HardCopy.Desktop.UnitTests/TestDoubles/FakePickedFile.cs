using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// A file the user "chose": a name and some bytes, or a name and the failure reading it produces.
/// </summary>
internal sealed class FakePickedFile : IPickedFile
{
    private readonly byte[] _content;
    private readonly Exception? _failure;

    internal FakePickedFile(string name, byte[] content)
    {
        Name = name;
        _content = content;
    }

    private FakePickedFile(string name, Exception failure)
    {
        Name = name;
        _content = [];
        _failure = failure;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Gets how many times the content was read.</summary>
    internal int ReadCount { get; private set; }

    /// <summary>Creates a file whose read fails.</summary>
    /// <param name="name">The file's name.</param>
    /// <param name="failure">The exception the read throws.</param>
    /// <returns>The unreadable file.</returns>
    internal static FakePickedFile Failing(string name, Exception failure) => new(name, failure);

    /// <inheritdoc/>
    public Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        ReadCount++;

        return _failure is null ? Task.FromResult(_content) : Task.FromException<byte[]>(_failure);
    }
}
