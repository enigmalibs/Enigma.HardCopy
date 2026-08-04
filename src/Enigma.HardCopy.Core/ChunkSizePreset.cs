namespace Enigma.HardCopy.Core;

/// <summary>
/// The chunk sizes offered to the user, in bytes. The enum value <b>is</b> the size in bytes.
/// </summary>
/// <remarks>
/// A larger chunk means fewer, denser barcodes: fewer pages to print and fewer codes to scan, but smaller
/// modules on paper and a longer string to retype if a code ever has to be entered by hand.
/// <see cref="Medium"/> is the default and the size the page layout is tuned for.
/// </remarks>
public enum ChunkSizePreset
{
    /// <summary>512 bytes — the sparsest codes, for poor printers or difficult scanning conditions.</summary>
    Small = 512,

    /// <summary>1024 bytes — the default.</summary>
    Medium = 1024,

    /// <summary>1536 bytes — the densest codes, for the fewest pages.</summary>
    Large = 1536,
}
