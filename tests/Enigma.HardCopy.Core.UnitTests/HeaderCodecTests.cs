using System;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class HeaderCodecTests
{
    // Golden strings: these are the bytes that get printed on paper, so they are pinned character for
    // character rather than asserted through a round trip.
    [Fact]
    public void FormatHeader_ProducesTheSpecifiedLayout()
        => Assert.Equal("EHC1:K7QA:3/17:1A2B3C4D", HeaderCodec.FormatHeader(new CodeHeader("K7QA", 3, 17, 0x1A2B3C4Du)));

    [Fact]
    public void FormatHeader_ZeroPadsTheCrcToEightCharacters()
        => Assert.Equal("EHC1:K7QA:1/1:000000FF", HeaderCodec.FormatHeader(new CodeHeader("K7QA", 1, 1, 0xFFu)));

    [Fact]
    public void FormatHeader_KeepsTheFullWidthOfTheCrc()
        => Assert.Equal("EHC1:K7QA:1/1:FFFFFFFF", HeaderCodec.FormatHeader(new CodeHeader("K7QA", 1, 1, 0xFFFFFFFFu)));

    [Fact]
    public void FormatHeader_MetadataBlockUsesIndexZero()
        => Assert.Equal("EHC1:AAAA:0/9:00000000", HeaderCodec.FormatHeader(new CodeHeader("AAAA", 0, 9, 0u)));

    [Fact]
    public void FormatHeader_DoesNotPadTheCounters()
        => Assert.Equal("EHC1:K7QA:7/1234:00000000", HeaderCodec.FormatHeader(new CodeHeader("K7QA", 7, 1234, 0u)));

    [Fact]
    public void FormatCode_AppendsThePayloadAfterASeparator()
        => Assert.Equal(
            "EHC1:K7QA:1/1:1A2B3C4D:MZXW6YTBOI",
            HeaderCodec.FormatCode(new CodeHeader("K7QA", 1, 1, 0x1A2B3C4Du), "MZXW6YTBOI"));

    [Fact]
    public void FormatCode_StaysWithinTheQrAlphanumericCharset()
    {
        string code = HeaderCodec.FormatCode(new CodeHeader("K7QA", 12, 345, 0xDEADBEEFu), "MZXW6YTBOI");

        Assert.All(code, character => Assert.True(
            HardCopyFormat.AlphanumericCharset.Contains(character, StringComparison.Ordinal),
            $"'{character}' is outside the QR alphanumeric charset."));
    }

    [Theory]
    [InlineData("K7Q")]          // too short
    [InlineData("K7QAA")]        // too long
    [InlineData("K7Q1")]         // '1' is not a Base32 character
    [InlineData("k7qa")]         // lower case is never produced
    [InlineData("")]
    public void FormatHeader_InvalidBackupId_Throws(string backupId)
        => Assert.Throws<ArgumentException>(() => HeaderCodec.FormatHeader(new CodeHeader(backupId, 1, 1, 0u)));

    [Fact]
    public void FormatHeader_NullBackupId_Throws()
        => Assert.Throws<ArgumentException>(() => HeaderCodec.FormatHeader(new CodeHeader(null!, 1, 1, 0u)));

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(2, 1)]
    [InlineData(1, -1)]
    public void FormatHeader_InconsistentCounters_Throws(int index, int total)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => HeaderCodec.FormatHeader(new CodeHeader("K7QA", index, total, 0u)));

    [Fact]
    public void FormatCode_EmptyPayload_Throws()
        => Assert.Throws<ArgumentException>(() => HeaderCodec.FormatCode(new CodeHeader("K7QA", 1, 1, 0u), string.Empty));

    [Theory]
    [InlineData("MZXW0")]
    [InlineData("mzxw6")]
    [InlineData("MZXW6=")]
    public void FormatCode_NonBase32Payload_Throws(string payload)
        => Assert.Throws<ArgumentException>(() => HeaderCodec.FormatCode(new CodeHeader("K7QA", 1, 1, 0u), payload));

    [Fact]
    public void TryParseCode_ReadsBackWhatFormatCodeWrote()
    {
        CodeHeader original = new("K7QA", 3, 17, 0x1A2B3C4Du);

        Assert.True(HeaderCodec.TryParseCode(
            HeaderCodec.FormatCode(original, "MZXW6YTBOI"),
            out CodeHeader parsed,
            out string? payload));
        Assert.Equal(original, parsed);
        Assert.Equal("MZXW6YTBOI", payload);
    }

    [Fact]
    public void TryParseCode_MetadataBlockIsRecognized()
    {
        Assert.True(HeaderCodec.TryParseCode("EHC1:K7QA:0/9:00000000:MY", out CodeHeader header, out _));

        Assert.True(header.IsMetadata);
        Assert.Equal(9, header.Total);
    }

    [Fact]
    public void TryParseCode_DataChunkIsNotTheMetadataBlock()
    {
        Assert.True(HeaderCodec.TryParseCode("EHC1:K7QA:1/9:00000000:MY", out CodeHeader header, out _));

        Assert.False(header.IsMetadata);
    }

    // A code entered by hand may arrive in lower case and wrapped across lines; neither can lose
    // information, because every field of the format is upper case or numeric and none contains a space.
    [Theory]
    [InlineData("ehc1:k7qa:3/17:1a2b3c4d:mzxw6ytboi")]
    [InlineData("EHC1:K7QA:3/17:1A2B3C4D:MZXW6\r\nYTBOI")]
    [InlineData("  EHC1 : K7QA : 3/17 : 1A2B3C4D : MZXW6YTBOI  ")]
    [InlineData("Ehc1:K7qa:3/17:1a2B3c4D:MzXw6YtBoI")]
    public void TryParseCode_NormalizesCaseAndWhitespace(string code)
    {
        Assert.True(HeaderCodec.TryParseCode(code, out CodeHeader header, out string? payload));

        Assert.Equal(new CodeHeader("K7QA", 3, 17, 0x1A2B3C4Du), header);
        Assert.Equal("MZXW6YTBOI", payload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EHC1:K7QA:3/17:1A2B3C4D")]                // the payload field is missing entirely
    [InlineData("EHC1:K7QA:3/17:1A2B3C4D:MY:EXTRA")]        // one field too many
    [InlineData("EHC2:K7QA:1/1:00000000:MY")]               // a format version this decoder does not know
    [InlineData("XHC1:K7QA:1/1:00000000:MY")]               // not an Enigma.HardCopy code at all
    [InlineData("EHC1:K7Q:1/1:00000000:MY")]                // truncated backup ID
    [InlineData("EHC1:K7Q1:1/1:00000000:MY")]               // backup ID outside the Base32 alphabet
    [InlineData("EHC1:K7QA:1:00000000:MY")]                 // no index/total separator
    [InlineData("EHC1:K7QA:1/2/3:00000000:MY")]             // two index/total separators
    [InlineData("EHC1:K7QA:2/1:00000000:MY")]               // index beyond the total
    [InlineData("EHC1:K7QA:-1/1:00000000:MY")]              // negative index
    [InlineData("EHC1:K7QA:1/-1:00000000:MY")]              // negative total
    [InlineData("EHC1:K7QA:X/1:00000000:MY")]               // index is not a number
    [InlineData("EHC1:K7QA::00000000:MY")]                  // empty counters field
    [InlineData("EHC1:K7QA:1/1:1A2B3C:MY")]                 // CRC too short
    [InlineData("EHC1:K7QA:1/1:1A2B3C4D5:MY")]              // CRC too long
    [InlineData("EHC1:K7QA:1/1:1A2B3C4G:MY")]               // CRC is not hexadecimal
    [InlineData("EHC1:K7QA:1/1:00000000:")]                 // empty payload
    [InlineData("EHC1:K7QA:1/1:00000000:M!Y")]              // payload outside the Base32 alphabet
    [InlineData("EHC1:K7QA:1/1:00000000:MY=")]              // padded payload
    public void TryParseCode_Malformed_ReturnsFalse(string? code)
    {
        Assert.False(HeaderCodec.TryParseCode(code, out CodeHeader header, out string? payload));

        Assert.Equal(default, header);
        Assert.Null(payload);
    }

    [Fact]
    public void TryParseCode_AcceptsTheEmptyBackupWhereTheTotalIsZero()
    {
        Assert.True(HeaderCodec.TryParseCode("EHC1:K7QA:0/0:00000000:MY", out CodeHeader header, out _));

        Assert.Equal(0, header.Total);
    }

    [Fact]
    public void Normalize_StripsWhitespaceAndRaisesCase()
        => Assert.Equal("EHC1:K7QA:1/1:00000000:MY", HeaderCodec.Normalize(" ehc1:k7qa:1/1:\t00000000:\r\nmy "));

    [Fact]
    public void Normalize_Null_Throws() => Assert.Throws<ArgumentNullException>(() => HeaderCodec.Normalize(null!));
}
