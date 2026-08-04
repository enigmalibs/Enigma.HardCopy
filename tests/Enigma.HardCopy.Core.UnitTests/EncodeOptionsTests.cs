using System;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class EncodeOptionsTests
{
    [Fact]
    public void Default_UsesTheMediumPreset()
        => Assert.Equal((int)ChunkSizePreset.Medium, EncodeOptions.Default.ChunkSizeInBytes);

    [Theory]
    [InlineData(ChunkSizePreset.Small, 512)]
    [InlineData(ChunkSizePreset.Medium, 1024)]
    [InlineData(ChunkSizePreset.Large, 1536)]
    public void FromPreset_TakesItsSizeFromThePreset(ChunkSizePreset preset, int expected)
        => Assert.Equal(expected, EncodeOptions.FromPreset(preset).ChunkSizeInBytes);

    [Fact]
    public void FromPreset_UndefinedPreset_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => EncodeOptions.FromPreset((ChunkSizePreset)777));

    [Theory]
    [InlineData(EncodeOptions.MinChunkSizeInBytes)]
    [InlineData(EncodeOptions.MaxChunkSizeInBytes)]
    [InlineData(1024)]
    public void ChunkSizeInBytes_WithinRange_IsAccepted(int chunkSizeInBytes)
        => Assert.Equal(chunkSizeInBytes, new EncodeOptions { ChunkSizeInBytes = chunkSizeInBytes }.ChunkSizeInBytes);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(EncodeOptions.MinChunkSizeInBytes - 1)]
    [InlineData(EncodeOptions.MaxChunkSizeInBytes + 1)]
    public void ChunkSizeInBytes_OutOfRange_Throws(int chunkSizeInBytes)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new EncodeOptions { ChunkSizeInBytes = chunkSizeInBytes });

    // The largest permitted chunk must still fit the biggest QR symbol at error-correction level M, or the
    // encoder would happily produce a chunk that PHASE03 cannot render.
    [Fact]
    public void MaxChunkSizeInBytes_FitsTheLargestQrSymbolAtEccM()
    {
        const int qrVersion40AlphanumericCapacityAtEccM = 3391;
        int headerLength = HeaderCodec.FormatHeader(new CodeHeader("AAAA", 9999, 9999, 0xFFFFFFFFu)).Length + 1;

        int longestCode = headerLength + Base32.GetSymbolCount(EncodeOptions.MaxChunkSizeInBytes);

        Assert.True(
            longestCode <= qrVersion40AlphanumericCapacityAtEccM,
            $"The longest code is {longestCode} characters, over the {qrVersion40AlphanumericCapacityAtEccM} available.");
    }

    [Fact]
    public void With_RevalidatesTheChunkSize()
        => Assert.Throws<ArgumentOutOfRangeException>(() => EncodeOptions.Default with { ChunkSizeInBytes = 1 });
}
