using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class ChunkerTests
{
    [Theory]
    [InlineData(0, 1024, 0)]
    [InlineData(1, 1024, 1)]
    [InlineData(1023, 1024, 1)]
    [InlineData(1024, 1024, 1)]
    [InlineData(1025, 1024, 2)]
    [InlineData(2048, 1024, 2)]
    [InlineData(100_000, 1024, 98)]
    [InlineData(100_000, 512, 196)]
    [InlineData(100_000, 1536, 66)]
    public void CountChunks_RoundsUp(long byteCount, int chunkSizeInBytes, int expected)
        => Assert.Equal(expected, Chunker.CountChunks(byteCount, chunkSizeInBytes));

    [Fact]
    public void CountChunks_NegativeByteCount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Chunker.CountChunks(-1, 1024));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CountChunks_NonPositiveChunkSize_Throws(int chunkSizeInBytes)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Chunker.CountChunks(1024, chunkSizeInBytes));

    [Fact]
    public void Split_Empty_ReturnsNoChunks() => Assert.Empty(Chunker.Split(ReadOnlyMemory<byte>.Empty, 1024));

    [Fact]
    public void Split_ShorterThanOneChunk_ReturnsTheWholePayload()
    {
        byte[] payload = [1, 2, 3];

        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(payload, 1024);

        Assert.Equal(payload, Assert.Single(chunks).ToArray());
    }

    [Fact]
    public void Split_OnlyTheLastChunkIsShort()
    {
        byte[] payload = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(payload, 4);

        Assert.Equal(new[] { 4, 4, 2 }, chunks.Select(chunk => chunk.Length).ToArray());
    }

    [Fact]
    public void Split_ExactMultiple_LeavesNoShortChunk()
    {
        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(new byte[12], 4);

        Assert.Equal(new[] { 4, 4, 4 }, chunks.Select(chunk => chunk.Length).ToArray());
    }

    [Theory]
    [InlineData(16)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(1536)]
    public void Split_ConcatenatedChunksReproduceThePayload(int chunkSizeInBytes)
    {
        byte[] payload = new byte[10_000];
        new Random(chunkSizeInBytes).NextBytes(payload);

        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(payload, chunkSizeInBytes);

        Assert.Equal(Chunker.CountChunks(payload.Length, chunkSizeInBytes), chunks.Count);
        Assert.Equal(payload, chunks.SelectMany(chunk => chunk.ToArray()).ToArray());
    }
}
