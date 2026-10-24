namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// What a scan session is for — which only changes what happens once every code is in.
/// </summary>
public enum SessionPurpose
{
    /// <summary>
    /// Get the file back: once every code is in, the file is rebuilt and saved where the user chooses.
    /// </summary>
    Recover = 0,

    /// <summary>
    /// Prove a printout is sound: once every code is in, the file is rebuilt in memory, checked against the
    /// SHA-256 recorded in the backup, and forgotten. Nothing is saved.
    /// </summary>
    Check,
}
