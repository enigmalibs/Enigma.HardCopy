using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>
/// A destination that keeps what was written to it, or fails on the attempt.
/// </summary>
internal sealed class FakeSaveTarget : ISaveTarget
{
    private readonly Exception? _failure;

    internal FakeSaveTarget(string name = "out.bin", Exception? failure = null)
    {
        Name = name;
        _failure = failure;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Gets what was written, or <see langword="null"/> if nothing was.</summary>
    internal byte[]? Written { get; private set; }

    /// <inheritdoc/>
    public Task WriteAllBytesAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_failure is not null)
        {
            throw _failure;
        }

        Written = [.. content];

        return Task.CompletedTask;
    }
}
