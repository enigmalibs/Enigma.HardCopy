using System.Security.Cryptography;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The default <see cref="IBackupIdGenerator"/>: four uniformly random <see cref="Base32"/> characters
/// from a cryptographic source.
/// </summary>
/// <remarks>
/// A cryptographic source is not needed for secrecy — the ID is printed on the page — but for uniformity.
/// With only 20 bits to work with, a generator that clustered would undermine the one job the ID has:
/// making two different backups distinguishable when their pages get mixed up.
/// </remarks>
public sealed class RandomBackupIdGenerator : IBackupIdGenerator
{
    /// <inheritdoc/>
    public string NewBackupId() => RandomNumberGenerator.GetString(Base32.Alphabet, HardCopyFormat.BackupIdLength);
}
