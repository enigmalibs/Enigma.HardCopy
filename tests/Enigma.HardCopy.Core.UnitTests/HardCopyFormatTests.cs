using System;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class HardCopyFormatTests
{
    [Fact]
    public void Prefix_IsMagicFollowedByVersion() => Assert.Equal("EHC1", HardCopyFormat.Prefix);

    [Fact]
    public void AlphanumericCharset_MatchesTheQrAlphanumericMode()
        => Assert.Equal(45, HardCopyFormat.AlphanumericCharset.Length);

    // Everything a code is made of has to live inside the QR alphanumeric charset, or the symbol silently
    // falls back to byte mode: the Base32 payload, the separators, and the hexadecimal of the CRC field.
    [Fact]
    public void Base32Alphabet_IsASubsetOfTheQrAlphanumericCharset()
        => Assert.All(Base32.Alphabet, character => Assert.True(
            HardCopyFormat.AlphanumericCharset.Contains(character, StringComparison.Ordinal),
            $"Base32 uses '{character}', which is outside the QR alphanumeric charset."));

    [Fact]
    public void Separators_AreInTheQrAlphanumericCharset()
    {
        Assert.Contains(HardCopyFormat.FieldSeparator, HardCopyFormat.AlphanumericCharset);
        Assert.Contains(HardCopyFormat.IndexSeparator, HardCopyFormat.AlphanumericCharset);
    }

    [Fact]
    public void UpperCaseHexDigits_AreInTheQrAlphanumericCharset()
        => Assert.All("0123456789ABCDEF", character => Assert.True(
            HardCopyFormat.AlphanumericCharset.Contains(character, StringComparison.Ordinal)));

    [Fact]
    public void BackupIdLength_IsFourCharacters() => Assert.Equal(4, HardCopyFormat.BackupIdLength);

    [Fact]
    public void CrcHexLength_CoversAFullThirtyTwoBitChecksum()
        => Assert.Equal(8, HardCopyFormat.CrcHexLength);

    [Fact]
    public void MetadataIndex_IsZeroSoDataChunksCanStartAtOne()
        => Assert.Equal(0, HardCopyFormat.MetadataIndex);
}
