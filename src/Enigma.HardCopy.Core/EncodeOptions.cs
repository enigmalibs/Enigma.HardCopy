using System;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The tunable inputs of <see cref="IBackupEncoder.EncodeAsync"/>.
/// </summary>
/// <remarks>
/// Compression is not an option here: it is applied automatically and kept only when it actually shrinks
/// the payload, which is a decision the encoder can always make better than the caller.
/// </remarks>
public sealed record EncodeOptions
{
    /// <summary>
    /// The smallest permitted chunk size in bytes. Small enough for tests to exercise multi-chunk paths
    /// with tiny inputs, large enough that the header does not dwarf the payload.
    /// </summary>
    public const int MinChunkSizeInBytes = 16;

    /// <summary>
    /// The largest permitted chunk size in bytes. A chunk of this size Base32-expands to about 3277
    /// characters which, with the header, still fits the 3391-character alphanumeric capacity of the
    /// largest QR symbol (version 40) at error-correction level M.
    /// </summary>
    public const int MaxChunkSizeInBytes = 2048;

    /// <summary>Gets the default options — <see cref="ChunkSizePreset.Medium"/> chunks.</summary>
    public static EncodeOptions Default { get; } = new();

    /// <summary>
    /// Gets the chunk size in bytes, between <see cref="MinChunkSizeInBytes"/> and
    /// <see cref="MaxChunkSizeInBytes"/>. Defaults to <see cref="ChunkSizePreset.Medium"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the permitted range.</exception>
    public int ChunkSizeInBytes
    {
        get => field;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinChunkSizeInBytes);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxChunkSizeInBytes);
            field = value;
        }
    } = (int)ChunkSizePreset.Medium;

    /// <summary>Creates options using the chunk size of <paramref name="preset"/>.</summary>
    /// <param name="preset">The preset whose size to use.</param>
    /// <returns>Options with <see cref="ChunkSizeInBytes"/> set from <paramref name="preset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="preset"/> is not a defined preset.</exception>
    public static EncodeOptions FromPreset(ChunkSizePreset preset)
    {
        if (!Enum.IsDefined(preset))
        {
            throw new ArgumentOutOfRangeException(nameof(preset));
        }

        return new EncodeOptions { ChunkSizeInBytes = (int)preset };
    }
}
