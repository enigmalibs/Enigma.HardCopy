namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// A tick in the user's hand for each code read, because while scanning they are looking at the paper, not at the
/// screen.
/// </summary>
public interface IHapticFeedback
{
    /// <summary>Confirms that a code was new and moved the recovery forward.</summary>
    void Accepted();

    /// <summary>Signals that a code was refused — from another backup, damaged, or not one of ours.</summary>
    void Rejected();
}
