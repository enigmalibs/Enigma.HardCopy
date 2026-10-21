using System;
using System.Globalization;
using System.Linq;
using Enigma.HardCopy.Recover.Resources;
using Enigma.HardCopy.Recover.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Recover.UnitTests;

/// <summary>
/// The missing-index list as a person reads it off a phone: runs collapsed, the long tail counted.
/// </summary>
public sealed class IndexRangesTests
{
    [Fact]
    public void Format_OfNothing_IsEmpty() => Assert.Equal(string.Empty, IndexRanges.Format([], 8));

    [Fact]
    public void Format_OfOneIndex_IsThatIndex() => Assert.Equal("7", IndexRanges.Format([7], 8));

    [Fact]
    public void Format_OfARun_IsARange() => Assert.Equal("3–9", IndexRanges.Format([3, 4, 5, 6, 7, 8, 9], 8));

    [Fact]
    public void Format_OfTwoInARow_IsAPair_NotARange() => Assert.Equal("4, 5", IndexRanges.Format([4, 5], 8));

    [Fact]
    public void Format_OfMixedRunsAndSingles_ListsEachInOrder()
        => Assert.Equal("1, 3–5, 8, 9, 12", IndexRanges.Format([1, 3, 4, 5, 8, 9, 12], 8));

    [Fact]
    public void Format_PastTheRangeLimit_CountsTheRest()
    {
        string expected = string.Format(CultureInfo.CurrentCulture, Strings.MissingOverflowFormat, "1, 3", 4);

        Assert.Equal(expected, IndexRanges.Format([1, 3, 5, 7, 8, 9], 2));
    }

    [Fact]
    public void Format_ExactlyAtTheRangeLimit_ListsEverything()
        => Assert.Equal("1, 3, 5", IndexRanges.Format([1, 3, 5], 3));

    // A backup missing everything still fits on a line: one range, however long the run.
    [Fact]
    public void Format_OfAVeryLongRun_StaysOneRange()
        => Assert.Equal("1–400", IndexRanges.Format(Enumerable.Range(1, 400).ToArray(), 1));

    [Fact]
    public void Format_NullIndexes_Throws()
        => Assert.Throws<ArgumentNullException>(() => IndexRanges.Format(null!, 8));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Format_ANonPositiveLimit_Throws(int maxRanges)
        => Assert.Throws<ArgumentOutOfRangeException>(() => IndexRanges.Format([1], maxRanges));
}
