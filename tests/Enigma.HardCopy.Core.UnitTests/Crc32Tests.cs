using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class Crc32Tests
{
    [Fact]
    public void Compute_Empty_ReturnsZero() => Assert.Equal(0u, Crc32.Compute([]));

    // The published CRC-32/ISO-HDLC check values, including 0xCBF43926 for "123456789" — the vector every
    // implementation of this polynomial is expected to reproduce.
    [Theory]
    [InlineData("a", 0xE8B7BE43u)]
    [InlineData("abc", 0x352441C2u)]
    [InlineData("message digest", 0x20159D7Fu)]
    [InlineData("abcdefghijklmnopqrstuvwxyz", 0x4C2750BDu)]
    [InlineData("123456789", 0xCBF43926u)]
    [InlineData("The quick brown fox jumps over the lazy dog", 0x414FA339u)]
    public void Compute_MatchesPublishedVectors(string text, uint expected)
        => Assert.Equal(expected, Crc32.Compute(Encoding.ASCII.GetBytes(text)));

    // The whole point of using this CRC rather than any other: a hand-recovery can check a chunk with the
    // same tools that check a gzip file, because it is the same checksum gzip stores in its trailer.
    [Fact]
    public void Compute_MatchesTheCrcGzipStoresInItsTrailer()
    {
        byte[] original = Encoding.ASCII.GetBytes("Enigma.HardCopy — paper backup and recovery.");
        using MemoryStream buffer = new();
        using (GZipStream gzip = new(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(original);
        }

        byte[] compressed = buffer.ToArray();
        uint trailerCrc = BinaryPrimitives.ReadUInt32LittleEndian(compressed.AsSpan(compressed.Length - 8, 4));

        Assert.Equal(trailerCrc, Crc32.Compute(original));
    }

    [Fact]
    public void Compute_IsSensitiveToASingleFlippedBit()
    {
        byte[] original = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] flipped = [.. original];
        flipped[4] ^= 0x01;

        Assert.NotEqual(Crc32.Compute(original), Crc32.Compute(flipped));
    }

    [Fact]
    public void Compute_IsSensitiveToTransposedBytes()
    {
        Assert.NotEqual(Crc32.Compute([0xAA, 0xBB]), Crc32.Compute([0xBB, 0xAA]));
    }

    [Fact]
    public void Compute_IsStableAcrossCalls()
    {
        byte[] bytes = new byte[4096];
        new Random(42).NextBytes(bytes);

        Assert.Equal(Crc32.Compute(bytes), Crc32.Compute(bytes));
    }
}
