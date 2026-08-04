using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// The seam a recovery imports through: an image goes in, the codes printed on it come out.
/// </summary>
public sealed class ImageDecoderTests
{
    /// <summary>
    /// Sized so that a default-chunk backup fills a page grid exactly: eleven data chunks plus the metadata
    /// block is twelve codes, which is what an A4 sheet holds at the default chunk size.
    /// </summary>
    private const int PageFixtureSizeInBytes = 11_000;

    [Fact]
    public async Task TryReadCodes_ASingleCroppedSymbol_ReadsIt()
    {
        EncodedBackup backup = await SmallBackupAsync();

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.RenderSymbol(backup.Codes[0]));

        AssertSameCodes([backup.Codes[0]], codes);
    }

    // The shape that matters most: one scanned sheet carrying a whole grid of symbols. A single-barcode read
    // would return one arbitrary symbol and silently lose the other eleven.
    [Fact]
    public async Task TryReadCodes_APageOfTwelveSymbols_ReadsEveryOne()
    {
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(
            RecoveryFixtures.Incompressible(PageFixtureSizeInBytes));
        Assert.Equal(12, backup.Codes.Count);

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.RenderPage(backup.Codes));

        AssertSameCodes(backup.Codes, codes);
    }

    [Fact]
    public async Task TryReadCodes_AJpegScan_ReadsTheCodes()
    {
        EncodedBackup backup = await SmallBackupAsync();

        IReadOnlyList<string> codes = ReadCodes(
            RecoveryFixtures.RenderPage(backup.Codes, columns: 2, format: SKEncodedImageFormat.Jpeg));

        AssertSameCodes(backup.Codes, codes);
    }

    // Skia decodes BMP but will not write it, so the fixture assembles the file itself — worth the trouble,
    // because BMP is what some scanner drivers still produce by default and the application claims to take it.
    [Fact]
    public async Task TryReadCodes_ABmpScan_ReadsTheCodes()
    {
        EncodedBackup backup = await SmallBackupAsync();

        IReadOnlyList<string> codes = ReadCodes(
            RecoveryFixtures.ToBmp(RecoveryFixtures.RenderPage(backup.Codes, columns: 2, pixelsPerModule: 3)));

        AssertSameCodes(backup.Codes, codes);
    }

    // "No codes on it" and "not an image" are different problems with different remedies — re-scan the page,
    // versus import a different file — so they are reported differently.
    [Fact]
    public void TryReadCodes_ABlankPage_SucceedsWithNoCodes()
    {
        using MemoryStream stream = new(RecoveryFixtures.BlankImage());

        Assert.True(ImageDecoder.TryReadCodes(stream, out IReadOnlyList<string>? codes));
        Assert.Empty(codes);
    }

    [Fact]
    public void TryReadCodes_BytesThatAreNotAnImage_Fails()
    {
        using MemoryStream stream = new(RecoveryFixtures.NotAnImage());

        Assert.False(ImageDecoder.TryReadCodes(stream, out IReadOnlyList<string>? codes));
        Assert.Null(codes);
    }

    [Fact]
    public void TryReadCodes_AnEmptyStream_Fails()
    {
        using MemoryStream stream = new();

        Assert.False(ImageDecoder.TryReadCodes(stream, out _));
    }

    [Fact]
    public void TryReadCodes_NullStream_Throws()
        => Assert.Throws<ArgumentNullException>(() => ImageDecoder.TryReadCodes(null!, out _));

    // The decoder reads QR codes; it does not judge them. Deciding that a code belongs to some other system
    // entirely is the session's job, and it needs to see the text to say so.
    [Fact]
    public void TryReadCodes_AQrCodeThatIsNotOneOfOurs_StillReturnsItsText()
    {
        const string foreign = "HTTPS://EXAMPLE.INVALID/NOT-A-BACKUP";

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.RenderSymbol(foreign));

        AssertSameCodes([foreign], codes);
    }

    [Fact]
    public async Task TryReadCodes_ReadsFromTheStreamsCurrentPosition()
    {
        EncodedBackup backup = await SmallBackupAsync();
        byte[] png = RecoveryFixtures.RenderSymbol(backup.Codes[1]);
        using MemoryStream stream = new();
        stream.Write([0xDE, 0xAD]);
        stream.Write(png);
        stream.Position = 2;

        Assert.True(ImageDecoder.TryReadCodes(stream, out IReadOnlyList<string>? codes));
        AssertSameCodes([backup.Codes[1]], codes);
    }

    /// <summary>A backup of two codes — the metadata block and one small data chunk.</summary>
    private static Task<EncodedBackup> SmallBackupAsync()
        => RecoveryFixtures.EncodeAsync(
            RecoveryFixtures.Incompressible(300),
            options: EncodeOptions.FromPreset(ChunkSizePreset.Small));

    /// <summary>
    /// Compares the codes read off an image with the ones printed on it, as sets: the reader is not promised
    /// to return a page's symbols in any particular order, and a recovery does not need it to.
    /// </summary>
    private static void AssertSameCodes(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        Assert.All(expected, code => Assert.Contains(code, actual));
    }

    private static IReadOnlyList<string> ReadCodes(byte[] image)
    {
        using MemoryStream stream = new(image);

        Assert.True(ImageDecoder.TryReadCodes(stream, out IReadOnlyList<string>? codes), "the image did not decode");

        return codes;
    }
}
