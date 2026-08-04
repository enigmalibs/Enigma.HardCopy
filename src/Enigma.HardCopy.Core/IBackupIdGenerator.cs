namespace Enigma.HardCopy.Core;

/// <summary>
/// Supplies the backup ID stamped into every code of one backup.
/// </summary>
/// <remarks>
/// This is an abstraction so the identifier is a substitutable dependency rather than a hidden call to a
/// random source: tests need a known ID to assert against, and the encoder should not decide where entropy
/// comes from.
/// </remarks>
public interface IBackupIdGenerator
{
    /// <summary>Returns a new backup ID.</summary>
    /// <returns>
    /// <see cref="HardCopyFormat.BackupIdLength"/> characters drawn from <see cref="Base32.Alphabet"/>.
    /// </returns>
    string NewBackupId();
}
