using System;
using System.Collections.Generic;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class RecoveryInstructionsTests
{
    [Fact]
    public void Title_IsNotBlank() => Assert.False(string.IsNullOrWhiteSpace(RecoveryInstructions.Title));

    [Fact]
    public void Paragraphs_AreAllNonBlank()
    {
        Assert.NotEmpty(RecoveryInstructions.Paragraphs);
        Assert.All(RecoveryInstructions.Paragraphs, paragraph => Assert.False(string.IsNullOrWhiteSpace(paragraph)));
    }

    [Fact]
    public void CodeShape_IsBuiltFromTheFormatConstants()
    {
        Assert.StartsWith(HardCopyFormat.Prefix, RecoveryInstructions.CodeShape, StringComparison.Ordinal);
        Assert.Contains("<BID>", RecoveryInstructions.CodeShape, StringComparison.Ordinal);
        Assert.Contains("<IDX>", RecoveryInstructions.CodeShape, StringComparison.Ordinal);
        Assert.Contains("<TOT>", RecoveryInstructions.CodeShape, StringComparison.Ordinal);
        Assert.Contains("<CRC>", RecoveryInstructions.CodeShape, StringComparison.Ordinal);
        Assert.Contains("<PAYLOAD>", RecoveryInstructions.CodeShape, StringComparison.Ordinal);
    }

    [Fact]
    public void CodeShape_MatchesTheShapeOfARealCode()
    {
        string realCode = HeaderCodec.FormatCode(new CodeHeader("K7QA", 3, 17, 0x1A2B3C4D), "MZXW6YTB");

        Assert.Equal(CountSeparators(realCode), CountSeparators(RecoveryInstructions.CodeShape));
    }

    // Everything a hand recovery needs, and nothing it can guess. Each of these is a step someone would be
    // stuck at without the printed page: the two codecs by name and RFC, the checksum, the compression
    // container, the hash to verify against, the caption identifying the metadata code — and the escaping
    // rule, which the format spec only acquired once a file name was allowed to contain the separator.
    [Theory]
    [InlineData("Base32")]
    [InlineData("RFC 4648")]
    [InlineData("CRC-32")]
    [InlineData("gzip")]
    [InlineData("RFC 1952")]
    [InlineData("SHA-256")]
    [InlineData("META")]
    [InlineData("%25")]
    [InlineData("%7C")]
    [InlineData("format version")]
    [InlineData("file name")]
    [InlineData("chunk size")]
    [InlineData("encryption flag")]
    public void Paragraphs_ExplainWhatAnAppLessRecoveryNeeds(string expected)
        => Assert.Contains(expected, string.Join(' ', RecoveryInstructions.Paragraphs), StringComparison.Ordinal);

    [Fact]
    public void Paragraphs_QuoteTheCodeShape()
        => Assert.Contains(
            RecoveryInstructions.CodeShape,
            string.Join(' ', RecoveryInstructions.Paragraphs),
            StringComparison.Ordinal);

    [Fact]
    public void Paragraphs_NameTheMetadataIndex()
        => Assert.Contains(
            $"IDX {HardCopyFormat.MetadataIndex} is the metadata code",
            string.Join(' ', RecoveryInstructions.Paragraphs),
            StringComparison.Ordinal);

    private static int CountSeparators(string code)
    {
        int count = 0;
        foreach (char character in code)
        {
            if (character == HardCopyFormat.FieldSeparator || character == HardCopyFormat.IndexSeparator)
            {
                count++;
            }
        }

        return count;
    }
}
