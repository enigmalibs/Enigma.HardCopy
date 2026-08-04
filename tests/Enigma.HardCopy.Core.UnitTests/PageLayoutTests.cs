using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class PageLayoutTests
{
    private const string FileName = "keys.kdbx";

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task ForBackup_ModuleSizeNeverFallsBelowTheFloor(ChunkSizePreset preset)
    {
        PageLayout layout = PageLayout.ForBackup(await EncodeAsync(preset));

        Assert.True(
            layout.ModuleSizeInMillimetres >= PdfOptions.Default.MinimumModuleSizeInMillimetres,
            $"{layout.ModuleSizeInMillimetres} mm per module is below the {PdfOptions.Default.MinimumModuleSizeInMillimetres} mm floor.");
    }

    // The invariant the whole grid computation exists to protect: a page it claims can hold Columns ×
    // RowsPerPage codes must actually be able to, or the composer's page breaks land in the wrong place and
    // the printed "Page n / m" becomes a lie.
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task ForBackup_TheGridFitsTheSheet(ChunkSizePreset preset)
    {
        PageLayout layout = PageLayout.ForBackup(await EncodeAsync(preset));

        float usableWidth = PageLayout.PageWidthInMillimetres - (2f * layout.MarginInMillimetres);
        float contentHeight = PageLayout.PageHeightInMillimetres
            - (2f * layout.MarginInMillimetres)
            - PageLayout.HeaderHeightInMillimetres
            - PageLayout.FooterHeightInMillimetres;

        Assert.True(GridWidth(layout, layout.Columns) <= usableWidth, "the grid is wider than the page.");
        Assert.True(GridHeight(layout, layout.RowsPerPage) <= contentHeight, "the grid is taller than the page.");
        Assert.True(
            GridHeight(layout, layout.RowsOnFirstPage) <= contentHeight - PageLayout.InstructionsHeightInMillimetres,
            "page 1's grid does not leave room for the recovery instructions.");
    }

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task GetCodeCountOnPage_AccountsForEveryCodeExactlyOnce(ChunkSizePreset preset)
    {
        PageLayout layout = PageLayout.ForBackup(await EncodeAsync(preset));

        int placed = 0;
        for (int pageNumber = 1; pageNumber <= layout.PageCount; pageNumber++)
        {
            int onPage = layout.GetCodeCountOnPage(pageNumber);
            int capacity = pageNumber == 1 ? layout.CodesOnFirstPage : layout.CodesPerPage;
            Assert.InRange(onPage, 0, capacity);
            placed += onPage;
        }

        Assert.Equal(layout.CodeCount, placed);
    }

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task GetCodeCountOnPage_OnlyTheLastPageIsAllowedToBeShortOfCapacity(ChunkSizePreset preset)
    {
        PageLayout layout = PageLayout.ForBackup(await EncodeAsync(preset));

        for (int pageNumber = 1; pageNumber < layout.PageCount; pageNumber++)
        {
            int capacity = pageNumber == 1 ? layout.CodesOnFirstPage : layout.CodesPerPage;
            Assert.Equal(capacity, layout.GetCodeCountOnPage(pageNumber));
        }
    }

    [Fact]
    public async Task ForBackup_MatchesForCodes()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium);

        PageLayout fromBackup = PageLayout.ForBackup(backup);
        PageLayout fromCodes = PageLayout.ForCodes(backup.Codes);

        Assert.Equal(fromCodes.PageCount, fromBackup.PageCount);
        Assert.Equal(fromCodes.Columns, fromBackup.Columns);
        Assert.Equal(fromCodes.RowsPerPage, fromBackup.RowsPerPage);
        Assert.Equal(fromCodes.CellSizeInMillimetres, fromBackup.CellSizeInMillimetres);
    }

    [Fact]
    public void ForCodes_ModulesPerSideAgreesWithTheReportedVersion()
    {
        PageLayout layout = PageLayout.ForCodes(["EHC1:K7QA:1/1:1A2B3C4D:MZXW6YTB"]);

        Assert.Equal(QrRenderer.GetModulesPerSide(layout.QrVersion), layout.ModulesPerSide);
    }

    // The default configuration is what the plan's capacity budget was drawn up for: at least the 3 × 4 grid
    // that keeps an A4 page carrying more than ten codes. Tightening either bound would mean the layout has
    // silently become less dense than the plan assumed.
    [Fact]
    public async Task ForBackup_DefaultOptions_KeepsTheGridAtLeastAsDenseAsPlanned()
    {
        PageLayout layout = PageLayout.ForBackup(await EncodeAsync(ChunkSizePreset.Medium));

        Assert.True(layout.Columns >= 3, $"{layout.Columns} columns is thinner than the planned 3.");
        Assert.True(layout.RowsPerPage >= 4, $"{layout.RowsPerPage} rows is fewer than the planned 4.");
        Assert.True(layout.RowsOnFirstPage >= 1, "the recovery instructions crowded every code off page 1.");
    }

    [Fact]
    public async Task ForBackup_SmallerChunks_PutMoreCodesOnAPage()
    {
        PageLayout small = PageLayout.ForBackup(await EncodeAsync(ChunkSizePreset.Small));
        PageLayout large = PageLayout.ForBackup(await EncodeAsync(ChunkSizePreset.Large));

        Assert.True(
            small.CodesPerPage > large.CodesPerPage,
            $"small chunks gave {small.CodesPerPage} codes per page, large ones {large.CodesPerPage}.");
    }

    [Fact]
    public async Task ForBackup_ARaisedModuleFloor_TradesCodesPerPageForSize()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium);

        PageLayout tight = PageLayout.ForBackup(backup);
        PageLayout generous = PageLayout.ForBackup(backup, new PdfOptions { MinimumModuleSizeInMillimetres = 0.6f });

        Assert.True(generous.CodesPerPage < tight.CodesPerPage);
        Assert.True(generous.ModuleSizeInMillimetres > tight.ModuleSizeInMillimetres);
        Assert.True(generous.ModuleSizeInMillimetres >= 0.6f);
    }

    [Fact]
    public async Task ForBackup_EmptyFile_FitsOnOnePage()
    {
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, content: []);

        PageLayout layout = PageLayout.ForBackup(backup);

        Assert.Equal(1, layout.CodeCount);
        Assert.Equal(1, layout.PageCount);
        Assert.Equal(1, layout.GetCodeCountOnPage(1));
    }

    [Fact]
    public void ForCodes_AModuleFloorNoSheetCanHonour_Throws()
    {
        // A full-chunk code needs 137 modules per side; at 2 mm each that is a 274 mm symbol, which no A4
        // sheet can print however the grid is arranged.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PageLayout.ForCodes(
                [FullChunkCode()],
                new PdfOptions { MinimumModuleSizeInMillimetres = PdfOptions.MaxModuleSizeInMillimetres }));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void ForCodes_NullCodes_Throws()
        => Assert.Throws<ArgumentNullException>(() => PageLayout.ForCodes(null!));

    [Fact]
    public void ForCodes_NoCodes_Throws()
        => Assert.Throws<ArgumentException>(() => PageLayout.ForCodes([]));

    [Fact]
    public void ForCodes_AnEmptyCode_Throws()
        => Assert.Throws<ArgumentException>(() => PageLayout.ForCodes(["EHC1:K7QA:1/1:1A2B3C4D:MZXW6YTB", ""]));

    [Fact]
    public void ForBackup_NullBackup_Throws()
        => Assert.Throws<ArgumentNullException>(() => PageLayout.ForBackup(null!));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetCodeCountOnPage_BelowTheFirstPage_Throws(int pageNumber)
    {
        PageLayout layout = PageLayout.ForCodes(["EHC1:K7QA:1/1:1A2B3C4D:MZXW6YTB"]);

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetCodeCountOnPage(pageNumber));
    }

    [Fact]
    public void GetCodeCountOnPage_PastTheLastPage_Throws()
    {
        PageLayout layout = PageLayout.ForCodes(["EHC1:K7QA:1/1:1A2B3C4D:MZXW6YTB"]);

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetCodeCountOnPage(layout.PageCount + 1));
    }

    // The estimate is what the UI warns a user with before any work happens, so it may be pessimistic but
    // must never be optimistic — a promise of five pages that turns into nine is worse than no promise.
    [Theory]
    [InlineData(ChunkSizePreset.Small, true)]
    [InlineData(ChunkSizePreset.Medium, true)]
    [InlineData(ChunkSizePreset.Large, true)]
    [InlineData(ChunkSizePreset.Small, false)]
    [InlineData(ChunkSizePreset.Medium, false)]
    [InlineData(ChunkSizePreset.Large, false)]
    public async Task Estimate_NeverPromisesFewerPagesThanTheRealBackupNeeds(ChunkSizePreset preset, bool compressible)
    {
        byte[] content = compressible ? Compressible() : Incompressible(60_000);
        EncodedBackup backup = await EncodeAsync(preset, content);

        PageLayout estimated = PageLayout.Estimate(content.Length, EncodeOptions.FromPreset(preset));
        PageLayout actual = PageLayout.ForBackup(backup);

        Assert.True(
            estimated.PageCount >= actual.PageCount,
            $"estimated {estimated.PageCount} pages, the document needs {actual.PageCount}.");
        Assert.True(estimated.CodeCount >= actual.CodeCount);
    }

    [Fact]
    public async Task Estimate_AnIncompressibleFile_LandsOnTheRealPageCount()
    {
        byte[] content = Incompressible(60_000);
        EncodedBackup backup = await EncodeAsync(ChunkSizePreset.Medium, content);

        PageLayout estimated = PageLayout.Estimate(content.Length);

        Assert.Equal(PageLayout.ForBackup(backup).PageCount, estimated.PageCount);
    }

    [Fact]
    public void Estimate_ZeroBytes_IsOnePage()
    {
        PageLayout layout = PageLayout.Estimate(0);

        Assert.Equal(1, layout.CodeCount);
        Assert.Equal(1, layout.PageCount);
    }

    [Theory]
    [InlineData(1_000L)]
    [InlineData(100_000L)]
    [InlineData(10_000_000L)]
    public void Estimate_MoreBytes_NeverMeansFewerPages(long fileSizeInBytes)
    {
        PageLayout smaller = PageLayout.Estimate(fileSizeInBytes);
        PageLayout larger = PageLayout.Estimate(fileSizeInBytes * 2);

        Assert.True(larger.PageCount >= smaller.PageCount);
        Assert.True(larger.CodeCount > smaller.CodeCount);
    }

    [Fact]
    public void Estimate_NegativeSize_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => PageLayout.Estimate(-1));

    /// <summary>A structurally valid code carrying a full default-size chunk — the longest a backup produces.</summary>
    private static string FullChunkCode()
        => HeaderCodec.FormatCode(
            new CodeHeader(FixedBackupIdGenerator.DefaultBackupId, 1, 1, 0x1A2B3C4D),
            new string('A', Base32.GetSymbolCount((int)ChunkSizePreset.Medium)));

    private static float GridWidth(PageLayout layout, int columns)
        => columns == 0 ? 0f : (columns * layout.CellSizeInMillimetres) + ((columns - 1) * layout.SpacingInMillimetres);

    private static float GridHeight(PageLayout layout, int rows)
        => rows == 0 ? 0f : (rows * layout.CellHeightInMillimetres) + ((rows - 1) * layout.SpacingInMillimetres);

    private static async Task<EncodedBackup> EncodeAsync(ChunkSizePreset preset, byte[]? content = null)
    {
        using MemoryStream stream = new(content ?? Incompressible(30_000));
        BackupEncoder encoder = new(new FixedBackupIdGenerator(), new FixedTimeProvider());

        return await encoder.EncodeAsync(
            stream,
            FileName,
            EncodeOptions.FromPreset(preset),
            TestContext.Current.CancellationToken);
    }

    private static byte[] Incompressible(int length)
    {
        byte[] content = new byte[length];
        new Random(length).NextBytes(content);

        return content;
    }

    private static byte[] Compressible()
        => Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 2_000)));
}
