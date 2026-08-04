using System.Collections.Generic;
using System.Linq;

namespace Enigma.HardCopy.Core;

/// <summary>
/// What one imported image contributed to a recovery: every code found in it, and what became of each.
/// </summary>
/// <remarks>
/// A scanned or photographed sheet holds many codes, so the interesting failure is partial — eleven of twelve
/// symbols read, one smudged. Reporting per code rather than per image is what lets the user be told to
/// re-scan a page instead of being told the import failed.
/// </remarks>
public sealed record ImageScanResult
{
    private ImageScanResult()
    {
    }

    /// <summary>
    /// Gets the result of an image whose bytes are not a picture at all — a PDF, a text file, a truncated
    /// download. Nothing was read from it, and no code was offered to the session.
    /// </summary>
    public static ImageScanResult Unreadable { get; } = new() { IsImageReadable = false, Results = [] };

    /// <summary>
    /// Gets a value indicating whether the image could be decoded as a picture. When
    /// <see langword="false"/>, <see cref="Results"/> is empty and the file itself is the problem — not the
    /// codes on it.
    /// </summary>
    public required bool IsImageReadable { get; init; }

    /// <summary>
    /// Gets the outcome of every code found in the image, in the order the reader found them. A readable
    /// image with no codes in it yields an empty list.
    /// </summary>
    public required IReadOnlyList<AddCodeResult> Results { get; init; }

    /// <summary>Gets the number of codes found in the image, whatever became of them.</summary>
    public int CodesFound => Results.Count;

    /// <summary>
    /// Gets the number of codes this image contributed that the session did not already have — the measure
    /// of whether importing it advanced the recovery.
    /// </summary>
    public int AcceptedCount => Results.Count(result => result.IsAccepted);

    /// <summary>Creates the result of an image that was decoded and read.</summary>
    /// <param name="results">The outcome of every code found, in reading order. May be empty.</param>
    /// <returns>The result.</returns>
    public static ImageScanResult Scanned(IReadOnlyList<AddCodeResult> results)
        => new() { IsImageReadable = true, Results = results };
}
