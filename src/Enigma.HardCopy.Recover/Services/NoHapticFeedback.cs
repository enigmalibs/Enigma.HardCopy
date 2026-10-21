namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The haptic feedback of a platform with no vibration motor to drive: it does nothing.
/// </summary>
/// <remarks>The default registration, which a platform head replaces with its own.</remarks>
public sealed class NoHapticFeedback : IHapticFeedback
{
    /// <inheritdoc/>
    public void Accepted()
    {
    }

    /// <inheritdoc/>
    public void Rejected()
    {
    }
}
