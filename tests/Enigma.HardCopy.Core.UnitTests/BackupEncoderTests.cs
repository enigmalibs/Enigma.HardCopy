using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class BackupEncoderTests
{
    private const string FileName = "keys.kdbx";

    [Fact]
    public async Task EncodeAsync_ProducesTheMetadataCodePlusOnePerChunk()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(100_000));

        int expectedChunks = Chunker.CountChunks(100_000, (int)ChunkSizePreset.Medium);
        Assert.Equal(expectedChunks + 1, backup.Codes.Count);
    }

    [Fact]
    public async Task EncodeAsync_MetadataCodeComesFirstAndCarriesTheDataChunkTotal()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(10_000));

        CodeHeader header = ParseHeader(backup.Codes[0]);
        Assert.True(header.IsMetadata);
        Assert.Equal(HardCopyFormat.MetadataIndex, header.Index);
        Assert.Equal(backup.Codes.Count - 1, header.Total);
    }

    [Fact]
    public async Task EncodeAsync_DataCodesAreNumberedFromOneAndShareTheTotal()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(10_000));

        int total = backup.Codes.Count - 1;
        for (int index = 1; index <= total; index++)
        {
            CodeHeader header = ParseHeader(backup.Codes[index]);
            Assert.Equal(index, header.Index);
            Assert.Equal(total, header.Total);
            Assert.False(header.IsMetadata);
        }
    }

    [Fact]
    public async Task EncodeAsync_EveryCodeCarriesTheSameBackupId()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(10_000));

        Assert.Equal(FixedBackupIdGenerator.DefaultBackupId, backup.BackupId);
        Assert.All(backup.Codes, code => Assert.Equal(backup.BackupId, ParseHeader(code).BackupId));
    }

    // The invariant the whole payload encoding exists to protect: if a single character fell outside the QR
    // alphanumeric charset, every symbol would silently drop to byte mode and the capacity budget — and so
    // the page layout — would no longer hold.
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task EncodeAsync_EveryCodeStaysWithinTheQrAlphanumericCharset(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(
            Incompressible(20_000),
            fileName: "clé privée — 100%|weird 🔐.pem",
            EncodeOptions.FromPreset(preset));

        foreach (string code in backup.Codes)
        {
            Assert.All(code, character => Assert.True(
                HardCopyFormat.AlphanumericCharset.Contains(character, StringComparison.Ordinal),
                $"'{character}' is outside the QR alphanumeric charset."));
        }
    }

    [Fact]
    public async Task EncodeAsync_EveryCodeCarriesTheCrcOfItsOwnBytes()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(10_000));

        Assert.All(backup.Codes, code =>
        {
            Assert.True(HeaderCodec.TryParseCode(code, out CodeHeader header, out string? payload));
            Assert.Equal(header.Crc, Crc32.Compute(Base32.Decode(payload!)));
        });
    }

    [Fact]
    public async Task EncodeAsync_MetadataCodeRoundTripsToTheReportedMetadata()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(10_000));

        Assert.True(HeaderCodec.TryParseCode(backup.Codes[0], out _, out string? payload));
        string text = Encoding.UTF8.GetString(Base32.Decode(payload!));
        Assert.True(BackupMetadataCodec.TryParse(text, out BackupMetadata? parsed));
        Assert.Equal(backup.Metadata, parsed);
    }

    [Fact]
    public async Task EncodeAsync_RecordsTheSizeAndHashOfTheOriginalFile()
    {
        byte[] content = Compressible();

        EncodedBackup backup = await EncodeAsync(content);

        Assert.True(backup.Metadata.IsCompressed, "the fixture must exercise the compressed path");
        Assert.Equal(content.Length, backup.Metadata.OriginalSizeInBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), backup.Metadata.Sha256Hex);
    }

    [Fact]
    public async Task EncodeAsync_RecordsTheFileNameChunkSizeAndDate()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(1_000), FileName, EncodeOptions.FromPreset(ChunkSizePreset.Large));

        Assert.Equal(FileName, backup.Metadata.FileName);
        Assert.Equal((int)ChunkSizePreset.Large, backup.Metadata.ChunkSizeInBytes);
        Assert.Equal(FixedTimeProvider.DefaultDate, backup.Metadata.CreatedOn);
        Assert.Equal(HardCopyFormat.Version, backup.Metadata.FormatVersion);
        Assert.False(backup.Metadata.IsEncrypted);
    }

    [Fact]
    public async Task EncodeAsync_WithoutOptions_UsesTheMediumChunkSize()
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(1_000));

        Assert.Equal((int)ChunkSizePreset.Medium, backup.Metadata.ChunkSizeInBytes);
    }

    [Fact]
    public async Task EncodeAsync_CompressibleContent_IsCompressedIntoFewerChunks()
    {
        byte[] content = Compressible();

        EncodedBackup backup = await EncodeAsync(content);

        Assert.True(backup.Metadata.IsCompressed);
        Assert.True(
            backup.Codes.Count - 1 < Chunker.CountChunks(content.Length, (int)ChunkSizePreset.Medium),
            "compression should have reduced the chunk count");
    }

    [Fact]
    public async Task EncodeAsync_IncompressibleContent_IsStoredAsIs()
    {
        byte[] content = Incompressible(100_000);

        EncodedBackup backup = await EncodeAsync(content);

        Assert.False(backup.Metadata.IsCompressed);
        Assert.Equal(Chunker.CountChunks(content.Length, (int)ChunkSizePreset.Medium), backup.Codes.Count - 1);
    }

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task EncodeAsync_ChunkCountFollowsTheChosenChunkSize(ChunkSizePreset preset)
    {
        byte[] content = Incompressible(50_000);

        EncodedBackup backup = await EncodeAsync(content, FileName, EncodeOptions.FromPreset(preset));

        Assert.Equal(Chunker.CountChunks(content.Length, (int)preset), backup.Codes.Count - 1);
    }

    // The string-level half of the end-to-end round trip: everything but the QR symbols themselves, which
    // PHASE03 adds. Reassembling the codes must reproduce the file bit for bit, hash included.
    [Theory]
    [InlineData(ChunkSizePreset.Small, true)]
    [InlineData(ChunkSizePreset.Medium, true)]
    [InlineData(ChunkSizePreset.Large, true)]
    [InlineData(ChunkSizePreset.Small, false)]
    [InlineData(ChunkSizePreset.Medium, false)]
    [InlineData(ChunkSizePreset.Large, false)]
    public async Task EncodeAsync_CodesReassembleToTheOriginalFile(ChunkSizePreset preset, bool compressible)
    {
        byte[] content = compressible ? Compressible() : Incompressible(100_000);

        EncodedBackup backup = await EncodeAsync(content, FileName, EncodeOptions.FromPreset(preset));

        byte[] recovered = Reassemble(backup);
        Assert.Equal(content, recovered);
        Assert.Equal(backup.Metadata.Sha256Hex, Convert.ToHexStringLower(SHA256.HashData(recovered)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    public async Task EncodeAsync_ContentAroundAChunkBoundary_RoundTrips(int length)
    {
        byte[] content = Incompressible(length);

        EncodedBackup backup = await EncodeAsync(content);

        Assert.Equal(content, Reassemble(backup));
    }

    [Fact]
    public async Task EncodeAsync_EmptyFile_ProducesOnlyTheMetadataCode()
    {
        EncodedBackup backup = await EncodeAsync([]);

        Assert.Single(backup.Codes);
        Assert.Equal(0, ParseHeader(backup.Codes[0]).Total);
        Assert.Equal(0, backup.Metadata.OriginalSizeInBytes);
        Assert.False(backup.Metadata.IsCompressed);
        Assert.Empty(Reassemble(backup));
    }

    [Theory]
    [InlineData("keys.kdbx")]
    [InlineData("my|weird|name.key")]
    [InlineData("100%|pure.txt")]
    [InlineData("clé privée — sauvegarde.pem")]
    [InlineData("файл-ключа.txt")]
    [InlineData("emoji 🔐 backup.key")]
    public async Task EncodeAsync_ExoticFileName_SurvivesTheWholeCodePath(string fileName)
    {
        EncodedBackup backup = await EncodeAsync(Incompressible(2_000), fileName);

        Assert.True(HeaderCodec.TryParseCode(backup.Codes[0], out _, out string? payload));
        Assert.True(
            BackupMetadataCodec.TryParse(Encoding.UTF8.GetString(Base32.Decode(payload!)), out BackupMetadata? parsed));
        Assert.Equal(fileName, parsed!.FileName);
    }

    [Fact]
    public async Task EncodeAsync_ReadsFromTheStreamsCurrentPosition()
    {
        byte[] content = [1, 2, 3, 4, 5, 6, 7, 8];
        using MemoryStream stream = new(content);
        stream.Position = 4;

        EncodedBackup backup = await CreateEncoder().EncodeAsync(stream, FileName, null, TestContext.Current.CancellationToken);

        Assert.Equal(4, backup.Metadata.OriginalSizeInBytes);
        Assert.Equal(new byte[] { 5, 6, 7, 8 }, Reassemble(backup));
    }

    [Fact]
    public async Task EncodeAsync_NullStream_Throws()
        => await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateEncoder().EncodeAsync(null!, FileName, null, TestContext.Current.CancellationToken));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EncodeAsync_MissingFileName_Throws(string? fileName)
    {
        using MemoryStream stream = new([1, 2, 3]);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => CreateEncoder().EncodeAsync(stream, fileName!, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EncodeAsync_UnreadableStream_Throws()
    {
        // A disposed MemoryStream is the simplest stream that reports CanRead == false.
        MemoryStream stream = new([1, 2, 3]);
        await stream.DisposeAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateEncoder().EncodeAsync(stream, FileName, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EncodeAsync_AlreadyCancelled_Throws()
    {
        using MemoryStream stream = new(Incompressible(10_000));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateEncoder().EncodeAsync(stream, FileName, null, cancellation.Token));
    }

    [Fact]
    public void Constructor_NullBackupIdGenerator_Throws()
        => Assert.Throws<ArgumentNullException>(() => new BackupEncoder(null!, new FixedTimeProvider()));

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
        => Assert.Throws<ArgumentNullException>(() => new BackupEncoder(new FixedBackupIdGenerator(), null!));

    private static BackupEncoder CreateEncoder()
        => new(new FixedBackupIdGenerator(), new FixedTimeProvider());

    private static async Task<EncodedBackup> EncodeAsync(
        byte[] content,
        string fileName = FileName,
        EncodeOptions? options = null)
    {
        using MemoryStream stream = new(content);

        return await CreateEncoder().EncodeAsync(stream, fileName, options, TestContext.Current.CancellationToken);
    }

    private static CodeHeader ParseHeader(string code)
    {
        Assert.True(HeaderCodec.TryParseCode(code, out CodeHeader header, out _), $"'{code}' did not parse.");

        return header;
    }

    /// <summary>Walks the data codes exactly as a recovery will: parse, decode, verify the CRC, concatenate.</summary>
    private static byte[] Reassemble(EncodedBackup backup)
    {
        List<byte> payload = [];
        for (int index = 1; index < backup.Codes.Count; index++)
        {
            Assert.True(HeaderCodec.TryParseCode(backup.Codes[index], out CodeHeader header, out string? chunk));
            byte[] bytes = Base32.Decode(chunk!);
            Assert.Equal(header.Crc, Crc32.Compute(bytes));
            payload.AddRange(bytes);
        }

        byte[] assembled = [.. payload];

        return backup.Metadata.IsCompressed ? Decompress(assembled) : assembled;
    }

    private static byte[] Decompress(byte[] compressed)
    {
        using MemoryStream source = new(compressed);
        using GZipStream gzip = new(source, CompressionMode.Decompress);
        using MemoryStream buffer = new();
        gzip.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>Random bytes: gzip cannot shrink them, so this exercises the stored-as-is path.</summary>
    private static byte[] Incompressible(int length)
    {
        byte[] content = new byte[length];
        new Random(length).NextBytes(content);

        return content;
    }

    /// <summary>Highly repetitive text: gzip shrinks it by orders of magnitude.</summary>
    private static byte[] Compressible()
        => Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 2_000)));
}
