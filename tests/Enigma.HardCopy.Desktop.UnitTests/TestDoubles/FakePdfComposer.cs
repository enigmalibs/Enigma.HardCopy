using System;
using System.Threading;
using Enigma.HardCopy.Core;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for PDF composition, so a ViewModel test can make it fail, or make it wait, without spending the
/// time real composition takes.
/// </summary>
internal sealed class FakePdfComposer : IPdfComposer
{
    /// <summary>Gets or sets what runs before the bytes are returned — throw, or block on the token.</summary>
    internal Action<CancellationToken>? OnCompose { get; set; }

    /// <summary>Gets the bytes handed back, standing in for a PDF.</summary>
    internal byte[] Pdf { get; init; } = [0x25, 0x50, 0x44, 0x46];

    /// <summary>Gets how many times composition was asked for.</summary>
    internal int ComposeCount { get; private set; }

    /// <inheritdoc/>
    public byte[] Compose(EncodedBackup backup, PdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);

        ComposeCount++;
        OnCompose?.Invoke(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return Pdf;
    }
}
