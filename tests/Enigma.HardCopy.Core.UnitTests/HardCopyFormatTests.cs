using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class HardCopyFormatTests
{
    [Fact]
    public void Prefix_IsMagicFollowedByVersion() => Assert.Equal("EHC1", HardCopyFormat.Prefix);

    [Fact]
    public void AlphanumericCharset_MatchesTheQrAlphanumericMode()
        => Assert.Equal(45, HardCopyFormat.AlphanumericCharset.Length);
}
