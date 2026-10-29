using System;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// A clock that only moves when a test moves it, so "not again for a minute" is tested in no time.
/// </summary>
/// <remarks>
/// Timestamps only: nothing under test here starts a timer.
/// </remarks>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc />
    /// <remarks>One tick per tick of <see cref="GetUtcNow"/>, so elapsed times follow this clock too.</remarks>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp() => _now.UtcTicks;

    /// <summary>
    /// Moves the clock on.
    /// </summary>
    /// <param name="by">How far.</param>
    public void Advance(TimeSpan by) => _now += by;
}
