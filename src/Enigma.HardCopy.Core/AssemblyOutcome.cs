namespace Enigma.HardCopy.Core;

/// <summary>
/// What came of asking a <see cref="RecoverySession"/> to rebuild the file.
/// </summary>
/// <remarks>
/// <see cref="Incomplete"/> is the zero value, so an uninitialised outcome cannot read as a verified
/// recovery.
/// </remarks>
public enum AssemblyOutcome
{
    /// <summary>
    /// The session is not ready: the metadata block or at least one data chunk is still missing. See
    /// <see cref="RecoveryStatus.MissingIndexes"/> for what to look for.
    /// </summary>
    Incomplete = 0,

    /// <summary>
    /// The metadata block has the encryption flag set. Format version 1 never sets it — the flag is reserved
    /// for a later version — so this backup was written by something that can do more than this version can
    /// undo. The chunks are all present and verified; they are simply not a file this build can produce, and
    /// handing back what is presumably ciphertext as a "recovered file" would be a lie.
    /// </summary>
    EncryptionUnsupported,

    /// <summary>
    /// The metadata block says the chunks form a gzip stream, and they do not decompress. Every chunk passed
    /// its own checksum, so this means the codes are internally consistent but not the ones this metadata
    /// block describes — the likeliest cause is a mistyped index that moved a chunk without breaking its
    /// CRC-32, which covers a code's payload and not its header.
    /// </summary>
    DecompressionFailed,

    /// <summary>
    /// The file was rebuilt, but its SHA-256 is not the one the metadata block recorded, so this is not
    /// byte-for-byte the file that was backed up. The bytes are still returned: a user who knows what they
    /// are looking at may want them anyway, which is the only reason this is not simply a failure.
    /// </summary>
    HashMismatch,

    /// <summary>
    /// The file was rebuilt and its SHA-256 matches the metadata block. The recovery is intact — this is the
    /// only outcome that says so.
    /// </summary>
    Verified,
}
