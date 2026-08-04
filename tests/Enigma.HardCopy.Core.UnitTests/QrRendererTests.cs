using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using SkiaSharp;
using Xunit;
using ZXing;
using ZXing.Common;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class QrRendererTests
{
    private const string FileName = "keys.kdbx";

    /// <summary>
    /// The size of the round-trip fixture. Large enough that every preset produces a couple of dozen codes —
    /// so the first and the last data code really are different codes, the last one a partial chunk — and small
    /// enough that the suite stays quick.
    /// </summary>
    private const int FixtureSizeInBytes = 40_000;

    // ---------------------------------------------------------------------------------------------------
    // The seam: PHASE03 acceptance item 4 — code string -> PNG -> ZXing.Net decode -> identical string,
    // across all three chunk sizes. Only three codes per preset are rendered (the metadata block, the first
    // data chunk and the last, partial one): they are the three shapes a backup produces, and rendering every
    // code of a 40 KB backup would cost seconds for no extra coverage.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task Render_MetadataCode_RoundTripsThroughZxing(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(preset);

        AssertRoundTripsThroughZxing(backup.Codes[0]);
    }

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task Render_FirstDataCode_RoundTripsThroughZxing(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(preset);

        AssertRoundTripsThroughZxing(backup.Codes[1]);
    }

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task Render_LastDataCode_RoundTripsThroughZxing(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(preset);

        AssertRoundTripsThroughZxing(backup.Codes[^1]);
    }

    // Every error-correction level has to survive the same round trip, because the level is a build-tunable
    // constant: whoever changes it must find out from a red test, not from an unreadable print.
    [Theory]
    [InlineData(QrErrorCorrectionLevel.Low)]
    [InlineData(QrErrorCorrectionLevel.Medium)]
    [InlineData(QrErrorCorrectionLevel.Quartile)]
    [InlineData(QrErrorCorrectionLevel.High)]
    public void Render_EveryErrorCorrectionLevel_RoundTripsThroughZxing(QrErrorCorrectionLevel errorCorrection)
        => AssertRoundTripsThroughZxing(
            LongestCode((int)ChunkSizePreset.Medium),
            new QrRenderOptions { ErrorCorrection = errorCorrection });

    // ---------------------------------------------------------------------------------------------------
    // The alphanumeric-mode invariant. GetVersionForLength sizes a synthetic string, which is only equivalent
    // to a real code while the encoder keeps choosing alphanumeric mode: byte mode would spend 8 bits per
    // character instead of 5.5, the two versions would diverge here, and the page-layout capacity budget
    // computed from GetVersionForLength would silently be wrong.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task GetVersion_RealCodes_AgreeWithGetVersionForLength(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(preset);

        Assert.All(
            Representative(backup),
            code => Assert.Equal(QrRenderer.GetVersionForLength(code.Length), QrRenderer.GetVersion(code)));
    }

    [Theory]
    [InlineData(QrErrorCorrectionLevel.Low)]
    [InlineData(QrErrorCorrectionLevel.Medium)]
    [InlineData(QrErrorCorrectionLevel.Quartile)]
    [InlineData(QrErrorCorrectionLevel.High)]
    public void GetVersionForLength_AgreesWithGetVersionAtEveryErrorCorrectionLevel(
        QrErrorCorrectionLevel errorCorrection)
    {
        string code = LongestCode((int)ChunkSizePreset.Small);

        Assert.Equal(
            QrRenderer.GetVersionForLength(code.Length, errorCorrection),
            QrRenderer.GetVersion(code, errorCorrection));
    }

    // ---------------------------------------------------------------------------------------------------
    // Capacity.
    // ---------------------------------------------------------------------------------------------------

    // The plan's *Capacity check* line (docs/plan/FEATURE-79FF.md, "Barcode format specification") reads:
    // "1024 B -> 1639 Base32 chars + ~27 header chars ~= 1666 -> fits QR version 25 at ECC M (1708
    // alphanumeric)". The character arithmetic is right — a real default-chunk code measures 1663 to 1668
    // characters — but the capacity lookup is NOT: ISO/IEC 18004 gives version 25 at ECC M an alphanumeric
    // capacity of 1451, not 1708. QRCoder agrees to the character (1451 -> v25, 1452 -> v26, 1637 -> v27,
    // 1638..1732 -> v28), so a default chunk needs version 28, whose 129 + 8 = 137 modules per side come to
    // ~48 mm at the plan's 0.35 mm/module rather than the ~44 mm it assumed. The plan's 3 x 5 = 15
    // codes-per-A4-page conclusion still holds at 48 mm; only its version number does not.
    [Fact]
    public void Render_DefaultChunkCode_FitsTheVersionThePageLayoutBudgetsFor()
    {
        const int budgetedVersion = 28;

        QrSymbol symbol = QrRenderer.Render(LongestCode((int)ChunkSizePreset.Medium));

        Assert.InRange(symbol.Version, 1, budgetedVersion);
        Assert.Equal(QrRenderer.GetModulesPerSide(symbol.Version), symbol.ModulesPerSide);
        Assert.InRange(symbol.ModulesPerSide, 1, QrRenderer.GetModulesPerSide(budgetedVersion));
    }

    // Mirrors EncodeOptionsTests.MaxChunkSizeInBytes_FitsTheLargestQrSymbolAtEccM, one level down: the ceiling
    // the encoder permits must actually render, or the encoder could produce a chunk PHASE03 cannot print.
    [Fact]
    public void Render_MaxChunkSizeCode_StillFitsTheLargestSymbol()
    {
        string code = LongestCode(EncodeOptions.MaxChunkSizeInBytes);

        QrSymbol symbol = QrRenderer.Render(code);

        Assert.InRange(symbol.Version, 1, 40);
        AssertRoundTripsThroughZxing(code);
    }

    // ---------------------------------------------------------------------------------------------------
    // Geometry. The PNG's own IHDR is the authority here, not our arithmetic: a mismatch between the reported
    // module count and the real pixel dimensions would put every symbol on the page at the wrong scale.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(QrRenderOptions.MinPixelsPerModule)]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(QrRenderOptions.MaxPixelsPerModule)]
    public void Render_PngHeader_ReportsPixelSizeOnBothSides(int pixelsPerModule)
    {
        QrSymbol symbol = QrRenderer.Render(
            LongestCode((int)ChunkSizePreset.Small),
            new QrRenderOptions { PixelsPerModule = pixelsPerModule });

        (int width, int height) = ReadPngDimensions(symbol.Png);

        Assert.Equal(symbol.PixelSize, width);
        Assert.Equal(symbol.PixelSize, height);
        Assert.Equal(symbol.ModulesPerSide, width / pixelsPerModule);
        Assert.Equal(pixelsPerModule, symbol.PixelsPerModule);
    }

    [Fact]
    public void Render_WithoutOptions_UsesTheDefaults()
    {
        string code = LongestCode((int)ChunkSizePreset.Small);

        QrSymbol symbol = QrRenderer.Render(code);

        Assert.Equal(QrRenderOptions.Default.PixelsPerModule, symbol.PixelsPerModule);
        Assert.Equal(QrRenderer.GetVersion(code, QrErrorCorrectionLevel.Medium), symbol.Version);
    }

    [Theory]
    [InlineData(1, 29)]
    [InlineData(2, 33)]
    [InlineData(10, 65)]
    [InlineData(25, 125)]
    [InlineData(28, 137)]
    [InlineData(40, 185)]
    public void GetModulesPerSide_FollowsTheStandardFormula(int version, int expected)
    {
        Assert.Equal(expected, QrRenderer.GetModulesPerSide(version));
        Assert.Equal(
            21 + (4 * (version - 1)) + (2 * QrRenderer.QuietZoneModules),
            QrRenderer.GetModulesPerSide(version));
    }

    [Fact]
    public void QuietZoneModules_IsTheFourTheStandardRequires()
        => Assert.Equal(4, QrRenderer.QuietZoneModules);

    // ---------------------------------------------------------------------------------------------------
    // Validation.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_MissingCode_Throws(string? code)
        => Assert.ThrowsAny<ArgumentException>(() => QrRenderer.Render(code!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetVersion_MissingCode_Throws(string? code)
        => Assert.ThrowsAny<ArgumentException>(() => QrRenderer.GetVersion(code!));

    // Longer than EncodeOptions.MaxChunkSizeInBytes allows, which is exactly why this cannot happen in
    // production — but the renderer still has to report it as a bad argument rather than leak QRCoder's own
    // DataTooLongException across the seam.
    [Fact]
    public void Render_CodeLongerThanAnySymbol_Throws()
    {
        string tooLong = LongestCode(EncodeOptions.MaxChunkSizeInBytes + 512);

        Assert.Throws<ArgumentException>(() => QrRenderer.Render(tooLong));
    }

    [Fact]
    public void GetVersion_CodeLongerThanAnySymbol_Throws()
    {
        string tooLong = LongestCode(EncodeOptions.MaxChunkSizeInBytes + 512);

        Assert.Throws<ArgumentException>(() => QrRenderer.GetVersion(tooLong));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(QrRenderOptions.MinPixelsPerModule - 1)]
    [InlineData(QrRenderOptions.MaxPixelsPerModule + 1)]
    [InlineData(int.MaxValue)]
    public void PixelsPerModule_OutOfRange_Throws(int pixelsPerModule)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new QrRenderOptions { PixelsPerModule = pixelsPerModule });

    [Theory]
    [InlineData(QrRenderOptions.MinPixelsPerModule)]
    [InlineData(8)]
    [InlineData(QrRenderOptions.MaxPixelsPerModule)]
    public void PixelsPerModule_WithinRange_IsAccepted(int pixelsPerModule)
        => Assert.Equal(pixelsPerModule, new QrRenderOptions { PixelsPerModule = pixelsPerModule }.PixelsPerModule);

    [Fact]
    public void Default_IsMediumErrorCorrectionAtEightPixelsPerModule()
    {
        Assert.Equal(QrErrorCorrectionLevel.Medium, QrRenderOptions.Default.ErrorCorrection);
        Assert.Equal(8, QrRenderOptions.Default.PixelsPerModule);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(41)]
    [InlineData(int.MaxValue)]
    public void GetModulesPerSide_VersionOutsideOneToForty_Throws(int version)
        => Assert.Throws<ArgumentOutOfRangeException>(() => QrRenderer.GetModulesPerSide(version));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void GetVersionForLength_LengthOutsideAnySymbol_Throws(int alphanumericLength)
        => Assert.Throws<ArgumentOutOfRangeException>(() => QrRenderer.GetVersionForLength(alphanumericLength));

    // The alphanumeric ceiling is 3391 characters at ECC M but only 1852 at ECC H, so the limit is per level
    // and the estimator has to be told about it in the currency it asked in.
    [Theory]
    [InlineData(3_392, QrErrorCorrectionLevel.Medium)]
    [InlineData(1_853, QrErrorCorrectionLevel.High)]
    public void GetVersionForLength_LengthBeyondTheLevelsCapacity_Throws(
        int alphanumericLength,
        QrErrorCorrectionLevel errorCorrection)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => QrRenderer.GetVersionForLength(alphanumericLength, errorCorrection));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1_451, 25)]
    [InlineData(1_452, 26)]
    [InlineData(3_391, 40)]
    public void GetVersionForLength_SizesToTheSmallestSymbolThatFits(int alphanumericLength, int expectedVersion)
        => Assert.Equal(expectedVersion, QrRenderer.GetVersionForLength(alphanumericLength));

    [Fact]
    public void Render_UndefinedErrorCorrectionLevel_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => QrRenderer.Render(
            LongestCode((int)ChunkSizePreset.Small),
            new QrRenderOptions { ErrorCorrection = (QrErrorCorrectionLevel)77 }));

    // ---------------------------------------------------------------------------------------------------
    // Helpers.
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Renders <paramref name="code"/>, decodes the PNG back with SkiaSharp plus ZXing.Net, and asserts the
    /// text came back character for character — <c>Assert.Equal</c> compares strings ordinally, which is what
    /// "identical" has to mean for a code whose CRC will be checked on recovery.
    /// </summary>
    private static void AssertRoundTripsThroughZxing(string code, QrRenderOptions? options = null)
    {
        QrSymbol symbol = QrRenderer.Render(code, options);

        using SKBitmap? bitmap = SKBitmap.Decode(symbol.Png);
        Assert.NotNull(bitmap);
        Assert.Equal(symbol.PixelSize, bitmap.Width);
        Assert.Equal(symbol.PixelSize, bitmap.Height);

        // PureBarcode and TryHarder both matter for a tightly cropped, unrotated symbol: the image is nothing
        // but the code and its quiet zone, so the locator has no surrounding page to work from.
        ZXing.SkiaSharp.BarcodeReader reader = new()
        {
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                PureBarcode = true,
                TryHarder = true,
            },
        };

        Result? result = reader.Decode(bitmap);

        Assert.NotNull(result);
        Assert.Equal(BarcodeFormat.QR_CODE, result.BarcodeFormat);
        Assert.Equal(code, result.Text);
    }

    /// <summary>
    /// The three code shapes a backup produces: the metadata block, a full data chunk, and the last chunk —
    /// which is normally partial and so renders as a smaller symbol.
    /// </summary>
    private static IReadOnlyList<string> Representative(EncodedBackup backup)
    {
        Assert.True(backup.Codes.Count >= 3, "the fixture must produce a metadata code and several data codes");

        return [backup.Codes[0], backup.Codes[1], backup.Codes[^1]];
    }

    private static async Task<EncodedBackup> EncodeAsync(ChunkSizePreset preset)
    {
        using MemoryStream stream = new(Incompressible(FixtureSizeInBytes));

        return await new BackupEncoder(new FixedBackupIdGenerator(), new FixedTimeProvider()).EncodeAsync(
            stream,
            FileName,
            EncodeOptions.FromPreset(preset),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The longest code a chunk of <paramref name="chunkSizeInBytes"/> bytes can produce: a full chunk behind
    /// the widest header the format allows (a four-digit index and total). This is the code the page layout has
    /// to budget for, so it is the code the capacity tests measure.
    /// </summary>
    private static string LongestCode(int chunkSizeInBytes)
        => HeaderCodec.FormatCode(
            new CodeHeader(FixedBackupIdGenerator.DefaultBackupId, 9_999, 9_999, 0xFFFFFFFFu),
            Base32.Encode(Incompressible(chunkSizeInBytes)));

    /// <summary>Random bytes — gzip cannot shrink them, so the fixture keeps its full chunk count.</summary>
    private static byte[] Incompressible(int length)
    {
        byte[] content = new byte[length];
        new Random(length).NextBytes(content);

        return content;
    }

    /// <summary>
    /// Reads the width and height straight out of the PNG's IHDR chunk — two big-endian 32-bit integers at
    /// offsets 16 and 20 — so the pixel assertions test the image itself instead of restating the arithmetic
    /// that produced <see cref="QrSymbol.PixelSize"/>.
    /// </summary>
    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        Assert.True(png.Length > 24, "the image is too short to hold a PNG signature and an IHDR chunk");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);

        return (
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
    }
}
