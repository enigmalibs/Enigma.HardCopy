using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class PdfComposerTests
{
    private const string FileName = "keys.kdbx";

    [Fact]
    public async Task Compose_ProducesAPdfFile()
    {
        byte[] pdf = new PdfComposer().Compose(await EncodeAsync(ChunkSizePreset.Medium), null, Cancellation);

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf, 0, 8), StringComparison.Ordinal);
        Assert.True(pdf.Length > 10_000, $"a multi-page document of QR symbols cannot be {pdf.Length} bytes.");
    }

    // The seam between the arithmetic and the renderer. PageLayout promises a page count before anything is
    // drawn — the UI shows it and the footer prints it — so the document really has to come out that long.
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task Compose_ProducesExactlyThePageCountTheLayoutPromised(ChunkSizePreset preset)
    {
        EncodedBackup backup = await EncodeAsync(preset);
        PageLayout layout = PageLayout.ForBackup(backup);

        byte[] pdf = new PdfComposer().Compose(backup, null, Cancellation);

        Assert.True(layout.PageCount > 1, "the fixture must be big enough to exercise page breaks.");
        Assert.Equal(layout.PageCount, CountPages(pdf));
    }

    [Fact]
    public async Task Compose_EmptyFile_ProducesTheInstructionsAndOneCodeOnASinglePage()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, content: []);

        byte[] pdf = new PdfComposer().Compose(backup, null, Cancellation);

        Assert.Equal(1, CountPages(pdf));
    }

    // Straddles the page boundaries, where an over-tall grid would silently spill into an extra page and a
    // too-short one would leave a blank sheet.
    [Theory]
    [InlineData(1)]
    [InlineData(1_024)]
    [InlineData(6_000)]
    [InlineData(12_000)]
    [InlineData(20_000)]
    [InlineData(45_000)]
    public async Task Compose_ThePageCountHoldsAcrossPageBoundaries(int fileSizeInBytes)
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, Incompressible(fileSizeInBytes));

        byte[] pdf = new PdfComposer().Compose(backup, null, Cancellation);

        Assert.Equal(PageLayout.ForBackup(backup).PageCount, CountPages(pdf));
    }

    // The QR symbols are the payload of this document; a lossily re-compressed one may simply not scan.
    // QuestPDF re-encodes images as JPEG unless told otherwise, so the absence of a DCT-decoded image is
    // what proves UseOriginalImage is still in force.
    [Fact]
    public async Task Compose_DoesNotReEncodeTheSymbolsAsJpeg()
    {
        byte[] pdf = new PdfComposer().Compose(await EncodeAsync(ChunkSizePreset.Medium), null, Cancellation);

        Assert.DoesNotContain("DCTDecode", Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
    }

    // Page furniture must never be the thing that loses a backup: the bundled font has no glyph for an emoji
    // or a CJK ideograph, and the file name is printed verbatim in the header.
    [Theory]
    [InlineData("keys.kdbx")]
    [InlineData("clé privée — sauvegarde.pem")]
    [InlineData("файл-ключа.txt")]
    [InlineData("鍵のバックアップ.kdbx")]
    [InlineData("emoji 🔐 backup.key")]
    [InlineData("100%|pure.txt")]
    public async Task Compose_AnExoticFileName_StillComposes(string fileName)
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, Incompressible(3_000), fileName);

        byte[] pdf = new PdfComposer().Compose(backup, null, Cancellation);

        Assert.Equal(PageLayout.ForBackup(backup).PageCount, CountPages(pdf));
    }

    [Fact]
    public async Task Compose_AFileNameTooLongForTheHeader_StillComposes()
    {
        EncodedBackup backup = await EncodeAsync(
            ChunkSizePreset.Medium,
            Incompressible(3_000),
            new string('n', 400) + ".kdbx");

        byte[] pdf = new PdfComposer().Compose(backup, null, Cancellation);

        Assert.Equal(PageLayout.ForBackup(backup).PageCount, CountPages(pdf));
    }

    [Theory]
    [InlineData(0.35f)]
    [InlineData(0.5f)]
    [InlineData(0.8f)]
    public async Task Compose_ARaisedModuleFloor_StillComposes(float minimumModuleSize)
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, Incompressible(6_000));
        PdfOptions options = new() { MinimumModuleSizeInMillimetres = minimumModuleSize };

        byte[] pdf = new PdfComposer().Compose(backup, options, Cancellation);

        Assert.Equal(PageLayout.ForBackup(backup, options).PageCount, CountPages(pdf));
    }

    [Theory]
    [InlineData(QrErrorCorrectionLevel.Low)]
    [InlineData(QrErrorCorrectionLevel.Medium)]
    [InlineData(QrErrorCorrectionLevel.Quartile)]
    [InlineData(QrErrorCorrectionLevel.High)]
    public async Task Compose_AnyErrorCorrectionLevel_StillComposes(QrErrorCorrectionLevel level)
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Small, Incompressible(4_000));
        PdfOptions options = new() { Qr = new QrRenderOptions { ErrorCorrection = level } };

        byte[] pdf = new PdfComposer().Compose(backup, options, Cancellation);

        Assert.Equal(PageLayout.ForBackup(backup, options).PageCount, CountPages(pdf));
    }

    [Fact]
    public async Task Compose_NullOptions_UsesTheDefaults()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, Incompressible(6_000));
        PdfComposer composer = new();

        Assert.Equal(
            CountPages(composer.Compose(backup, PdfOptions.Default, Cancellation)),
            CountPages(composer.Compose(backup, null, Cancellation)));
    }

    [Fact]
    public void Compose_NullBackup_Throws()
        => Assert.Throws<ArgumentNullException>(() => new PdfComposer().Compose(null!, null, Cancellation));

    [Fact]
    public async Task Compose_AModuleFloorNoSheetCanHonour_Throws()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, Incompressible(3_000));
        PdfOptions options = new() { MinimumModuleSizeInMillimetres = PdfOptions.MaxModuleSizeInMillimetres };

        Assert.Throws<ArgumentException>(() => new PdfComposer().Compose(backup, options, Cancellation));
    }

    [Fact]
    public async Task Compose_AlreadyCancelled_Throws()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        Assert.ThrowsAny<OperationCanceledException>(() => new PdfComposer().Compose(backup, null, cancellation.Token));
    }

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// Counts the page objects in a PDF. The page tree's dictionaries are written as plain text even when
    /// the content streams are compressed, so the <c>/Type /Page</c> entries can simply be counted — taking
    /// care not to count the single <c>/Type /Pages</c> node that holds them.
    /// </summary>
    private static int CountPages(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        int count = 0;
        int index = text.IndexOf("/Page", StringComparison.Ordinal);
        while (index >= 0)
        {
            int after = index + "/Page".Length;
            if (after >= text.Length || text[after] != 's')
            {
                count++;
            }

            index = text.IndexOf("/Page", after, StringComparison.Ordinal);
        }

        return count;
    }

    private static async Task<EncodedBackup> EncodeAsync(
        ChunkSizePreset preset,
        byte[]? content = null,
        string fileName = FileName)
    {
        using MemoryStream stream = new(content ?? Incompressible(30_000));
        BackupEncoder encoder = new(new FixedBackupIdGenerator(), new FixedTimeProvider());

        return await encoder.EncodeAsync(stream, fileName, EncodeOptions.FromPreset(preset), Cancellation);
    }

    private static byte[] Incompressible(int length)
    {
        byte[] content = new byte[length];
        new Random(length).NextBytes(content);

        return content;
    }
}
