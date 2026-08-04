using System.Collections.Generic;

namespace Enigma.HardCopy.Core;

/// <summary>
/// A snapshot of how far a <see cref="RecoverySession"/> has got: which backup it is recovering, what it
/// knows about the file, and which codes are still missing.
/// </summary>
/// <remarks>
/// This is what the user is shown between scans, so it is phrased in terms of what is still needed rather
/// than of what has been collected. The metadata block is tracked apart from the data chunks —
/// <see cref="HasMetadata"/> versus <see cref="MissingIndexes"/> — because it is the one code that is not
/// part of the file: without it the chunks can still all be present, and yet the file has no name, no
/// expected hash, and no way to know whether it needs decompressing.
/// </remarks>
public sealed record RecoveryStatus
{
    /// <summary>
    /// Gets the backup ID every accepted code shares, or <see langword="null"/> while no code has been
    /// accepted yet. The first accepted code fixes it, and any code carrying a different one is rejected
    /// from then on.
    /// </summary>
    public string? BackupId { get; init; }

    /// <summary>
    /// Gets the metadata read from the metadata block, or <see langword="null"/> until that block arrives.
    /// </summary>
    public BackupMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets the number of data chunks the backup is made of, or <see langword="null"/> while no code has
    /// been accepted yet. Every code carries it, so it is known from the first one — including before the
    /// metadata block arrives.
    /// </summary>
    public int? TotalChunks { get; init; }

    /// <summary>Gets the number of distinct data chunks the session holds.</summary>
    public required int ReceivedChunks { get; init; }

    /// <summary>
    /// Gets the indexes of the data chunks still missing, ascending — the codes the user has to find. Empty
    /// while <see cref="TotalChunks"/> is unknown, since nothing is known to be missing yet. The metadata
    /// block is not listed here; see <see cref="HasMetadata"/>.
    /// </summary>
    public required IReadOnlyList<int> MissingIndexes { get; init; }

    /// <summary>Gets a value indicating whether the metadata block has been read.</summary>
    public bool HasMetadata => Metadata is not null;

    /// <summary>
    /// Gets a value indicating whether everything needed to rebuild the file is present: the metadata block
    /// and every data chunk. A backup of an empty file is complete with the metadata block alone, since it
    /// has no data chunks.
    /// </summary>
    public bool IsComplete => HasMetadata && TotalChunks is int total && ReceivedChunks == total;
}
