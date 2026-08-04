using System;
using System.Collections.Generic;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Splits a payload into the fixed-size chunks that become one barcode each.
/// </summary>
/// <remarks>
/// Chunks are returned as slices of the source buffer, not copies: a backup of a few hundred kilobytes
/// otherwise doubles in memory for no benefit. Only the final chunk may be shorter than the chunk size.
/// </remarks>
public static class Chunker
{
    /// <summary>Returns the number of chunks <paramref name="byteCount"/> bytes split into.</summary>
    /// <param name="byteCount">The payload length in bytes. Zero yields zero chunks.</param>
    /// <param name="chunkSizeInBytes">The chunk size in bytes.</param>
    /// <returns>The chunk count — and therefore the <c>TOT</c> field of every code of the backup.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="byteCount"/> is negative, or <paramref name="chunkSizeInBytes"/> is not positive.
    /// </exception>
    public static int CountChunks(long byteCount, int chunkSizeInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSizeInBytes);

        long count = (byteCount + (chunkSizeInBytes - 1)) / chunkSizeInBytes;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, int.MaxValue, nameof(byteCount));

        return (int)count;
    }

    /// <summary>Splits <paramref name="payload"/> into consecutive chunks of at most the chunk size.</summary>
    /// <param name="payload">The payload to split. May be empty, which yields no chunks.</param>
    /// <param name="chunkSizeInBytes">The chunk size in bytes.</param>
    /// <returns>
    /// The chunks, in order, as slices of <paramref name="payload"/>. Every chunk but the last is exactly
    /// <paramref name="chunkSizeInBytes"/> long.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chunkSizeInBytes"/> is not positive.</exception>
    public static IReadOnlyList<ReadOnlyMemory<byte>> Split(ReadOnlyMemory<byte> payload, int chunkSizeInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSizeInBytes);

        int count = CountChunks(payload.Length, chunkSizeInBytes);
        List<ReadOnlyMemory<byte>> chunks = new(count);
        for (int offset = 0; offset < payload.Length; offset += chunkSizeInBytes)
        {
            chunks.Add(payload.Slice(offset, Math.Min(chunkSizeInBytes, payload.Length - offset)));
        }

        return chunks;
    }
}
