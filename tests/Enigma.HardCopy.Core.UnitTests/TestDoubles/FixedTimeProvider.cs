using System;

namespace Enigma.HardCopy.Core.UnitTests.TestDoubles;

/// <summary>
/// A <see cref="TimeProvider"/> frozen at one instant, in UTC, so the metadata date is deterministic
/// wherever the suite runs.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateOnly DefaultDate = new(2026, 8, 4);

    public FixedTimeProvider()
        : this(new DateTimeOffset(2026, 8, 4, 13, 45, 0, TimeSpan.Zero))
    {
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => now;
}
