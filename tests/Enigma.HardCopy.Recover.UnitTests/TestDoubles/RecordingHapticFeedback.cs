using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>Counts the ticks instead of making them.</summary>
internal sealed class RecordingHapticFeedback : IHapticFeedback
{
    /// <summary>Gets how many acceptance ticks were made.</summary>
    internal int AcceptedCount { get; private set; }

    /// <summary>Gets how many rejection ticks were made.</summary>
    internal int RejectedCount { get; private set; }

    /// <inheritdoc/>
    public void Accepted() => AcceptedCount++;

    /// <inheritdoc/>
    public void Rejected() => RejectedCount++;
}
