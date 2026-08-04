using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// A destination that keeps what was written to it, or fails on the attempt.
/// </summary>
internal sealed class FakeSaveTarget : ISaveTarget
{
    private readonly Exception? _failure;

    internal FakeSaveTarget(string name = "out.bin")
    {
        Name = name;
    }

    private FakeSaveTarget(string name, Exception failure)
    {
        Name = name;
        _failure = failure;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Location => $"/tmp/{Name}";

    /// <summary>Gets what was written, or <see langword="null"/> if nothing was.</summary>
    internal byte[]? Written { get; private set; }

    /// <summary>Creates a destination whose write fails.</summary>
    /// <param name="name">The destination's name.</param>
    /// <param name="failure">The exception the write throws.</param>
    /// <returns>The unwritable destination.</returns>
    internal static FakeSaveTarget Failing(string name, Exception failure) => new(name, failure);

    /// <inheritdoc/>
    public Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_failure is not null)
        {
            return Task.FromException(_failure);
        }

        Written = content;

        return Task.CompletedTask;
    }
}
