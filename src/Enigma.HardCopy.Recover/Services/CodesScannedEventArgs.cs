using System;
using System.Collections.Generic;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The QR code texts read from one camera frame.
/// </summary>
public sealed class CodesScannedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="CodesScannedEventArgs"/> class.</summary>
    /// <param name="codes">The texts read, in the order the reader found them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="codes"/> is <see langword="null"/>.</exception>
    public CodesScannedEventArgs(IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        Codes = codes;
    }

    /// <summary>
    /// Gets the texts read from the frame — every QR code in view, ours or not. A frame with nothing readable in it
    /// raises no event.
    /// </summary>
    public IReadOnlyList<string> Codes { get; }
}
