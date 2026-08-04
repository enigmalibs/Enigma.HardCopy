using System;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Everything a recovery needs to turn reassembled chunks back into the original file: what it was called,
/// how big it was, what it hashes to, and how it was packed.
/// </summary>
/// <remarks>
/// These are exactly the fields carried by the metadata block at barcode index
/// <see cref="HardCopyFormat.MetadataIndex"/>, one for one — <see cref="BackupMetadataCodec"/> round-trips
/// this record and nothing else. The backup ID is deliberately <i>not</i> here: it lives in the header of
/// every code (see <see cref="CodeHeader.BackupId"/>), including the metadata block's own, so storing it in
/// the payload as well would only create a second source of truth.
/// </remarks>
public sealed record BackupMetadata
{
    /// <summary>Gets the payload format version — the <c>v</c> field. Defaults to the current version.</summary>
    public int FormatVersion { get; init; } = HardCopyFormat.Version;

    /// <summary>Gets the original file's name — the <c>n</c> field. A leaf name, with no directory part.</summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the length of the <b>original</b> file in bytes — the <c>s</c> field. This is the size before
    /// any compression, so it describes the file a recovery is expected to produce.
    /// </summary>
    public required long OriginalSizeInBytes { get; init; }

    /// <summary>
    /// Gets the SHA-256 of the <b>original</b> file as 64 lower-case hexadecimal characters — the
    /// <c>h</c> field. Lower case matches what <c>sha256sum</c> prints, so it can be compared by eye
    /// against a hash computed with ordinary tools.
    /// </summary>
    public required string Sha256Hex { get; init; }

    /// <summary>
    /// Gets a value indicating whether the data chunks reassemble to a gzip stream rather than the file
    /// itself — the <c>c</c> field.
    /// </summary>
    public required bool IsCompressed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the payload is encrypted — the reserved <c>e</c> field, always
    /// <see langword="false"/> in format version 1.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets the chunk size the payload was split with, in bytes — the <c>z</c> field. Informational for a
    /// recovery, which learns the real chunk boundaries from the codes themselves.
    /// </summary>
    public required int ChunkSizeInBytes { get; init; }

    /// <summary>Gets the date the backup was produced — the <c>d</c> field, <c>yyyy-MM-dd</c>.</summary>
    public required DateOnly CreatedOn { get; init; }
}
