namespace Enigma.HardCopy.Core;

/// <summary>
/// The result of rebuilding the file from a <see cref="RecoverySession"/>: the bytes, if there are any, and
/// whether they are provably the ones that were backed up.
/// </summary>
/// <remarks>
/// A class rather than a record, because it carries the file's bytes: value equality over a payload of
/// arbitrary size would be an expensive operation offered by accident. Instances come from the factory
/// methods, so a result cannot claim to be verified without content or a hash.
/// </remarks>
public sealed class AssemblyResult
{
    private AssemblyResult()
    {
    }

    /// <summary>Gets what came of the assembly.</summary>
    public required AssemblyOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the rebuilt file's bytes, or <see langword="null"/> when there are none to offer — that is, for
    /// every outcome except <see cref="AssemblyOutcome.Verified"/> and
    /// <see cref="AssemblyOutcome.HashMismatch"/>. Note that the mismatch <i>does</i> carry content:
    /// unverified, but there.
    /// </summary>
    public byte[]? Content { get; init; }

    /// <summary>
    /// Gets the SHA-256 of <see cref="Content"/> as lower-case hexadecimal, or <see langword="null"/> when
    /// there is no content. Worth showing beside <see cref="BackupMetadata.Sha256Hex"/> when the two differ.
    /// </summary>
    public string? Sha256Hex { get; init; }

    /// <summary>
    /// Gets the metadata the recovery was carried out against, or <see langword="null"/> when the metadata
    /// block had not arrived yet. It holds the file name to save under, among the rest.
    /// </summary>
    public BackupMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether the rebuilt file's SHA-256 matches the one recorded when the backup
    /// was made — the end-to-end integrity check, and the only thing that makes a recovery trustworthy.
    /// </summary>
    public bool HashVerified => Outcome is AssemblyOutcome.Verified;

    /// <summary>Creates an <see cref="AssemblyOutcome.Incomplete"/> result.</summary>
    /// <param name="metadata">The metadata, if the metadata block has already been read.</param>
    /// <returns>The result.</returns>
    public static AssemblyResult Incomplete(BackupMetadata? metadata = null)
        => new() { Outcome = AssemblyOutcome.Incomplete, Metadata = metadata };

    /// <summary>Creates an <see cref="AssemblyOutcome.EncryptionUnsupported"/> result.</summary>
    /// <param name="metadata">The metadata block that has the encryption flag set.</param>
    /// <returns>The result.</returns>
    public static AssemblyResult EncryptionUnsupported(BackupMetadata metadata)
        => new() { Outcome = AssemblyOutcome.EncryptionUnsupported, Metadata = metadata };

    /// <summary>Creates an <see cref="AssemblyOutcome.DecompressionFailed"/> result.</summary>
    /// <param name="metadata">The metadata the recovery was carried out against.</param>
    /// <returns>The result.</returns>
    public static AssemblyResult DecompressionFailed(BackupMetadata metadata)
        => new() { Outcome = AssemblyOutcome.DecompressionFailed, Metadata = metadata };

    /// <summary>Creates an <see cref="AssemblyOutcome.HashMismatch"/> result.</summary>
    /// <param name="content">The rebuilt bytes, which are not what the metadata block describes.</param>
    /// <param name="sha256Hex">The SHA-256 of <paramref name="content"/>, as lower-case hexadecimal.</param>
    /// <param name="metadata">The metadata the recovery was carried out against.</param>
    /// <returns>The result.</returns>
    public static AssemblyResult HashMismatch(byte[] content, string sha256Hex, BackupMetadata metadata)
        => new()
        {
            Outcome = AssemblyOutcome.HashMismatch,
            Content = content,
            Sha256Hex = sha256Hex,
            Metadata = metadata,
        };

    /// <summary>Creates an <see cref="AssemblyOutcome.Verified"/> result.</summary>
    /// <param name="content">The rebuilt bytes.</param>
    /// <param name="sha256Hex">The SHA-256 of <paramref name="content"/>, as lower-case hexadecimal.</param>
    /// <param name="metadata">The metadata the recovery was carried out against.</param>
    /// <returns>The result.</returns>
    public static AssemblyResult Verified(byte[] content, string sha256Hex, BackupMetadata metadata)
        => new()
        {
            Outcome = AssemblyOutcome.Verified,
            Content = content,
            Sha256Hex = sha256Hex,
            Metadata = metadata,
        };
}
