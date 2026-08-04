using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// The whole point of the project, end to end: a file is encoded, printed as symbols, read back off the
/// paper and rebuilt — with its SHA-256 proving it is the same file.
/// </summary>
/// <remarks>
/// These are the plan's overall acceptance criteria 1 to 4 (<c>docs/plan/FEATURE-79FF.md</c>) at the level a
/// user experiences them. The scans are simulated by rendering the symbols the PDF composer would print and
/// laying them out on a page the way <see cref="PageLayout"/> does, so the images the reader is given have
/// the shape of a real sheet rather than of a convenient fixture.
/// </remarks>
public sealed class RecoveryRoundTripTests
{
    /// <summary>Sized so that a default-chunk backup fills one full page grid and spills onto a second.</summary>
    private const int PagedFixtureSizeInBytes = 11_000;

    // ---------------------------------------------------------------------------------------------------
    // Criterion 2, and criteria 3 to 4 along the way: the codes typed in rather than scanned, in an order
    // nobody chose, at every chunk size, for a file that compresses and one that does not.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(ChunkSizePreset.Small, false)]
    [InlineData(ChunkSizePreset.Medium, false)]
    [InlineData(ChunkSizePreset.Large, false)]
    [InlineData(ChunkSizePreset.Small, true)]
    [InlineData(ChunkSizePreset.Medium, true)]
    [InlineData(ChunkSizePreset.Large, true)]
    public async Task Recover_FromCodeStringsInRandomOrder_RebuildsTheFileExactly(
        ChunkSizePreset preset,
        bool compressible)
    {
        byte[] content = compressible ? RecoveryFixtures.Compressible() : RecoveryFixtures.Incompressible(20_000);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(
            content,
            options: EncodeOptions.FromPreset(preset));
        RecoverySession session = new();

        IReadOnlyList<AddCodeResult> results = RecoveryFixtures.AddAll(
            session,
            RecoveryFixtures.Shuffled(backup.Codes));

        Assert.All(results, result => Assert.True(result.IsAccepted, $"code {result.Index} was {result.Outcome}"));
        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(content, assembled.Content);
        Assert.Equal(RecoveryFixtures.Sha256Hex(content), assembled.Sha256Hex);
        Assert.Equal(content.Length, assembled.Metadata?.OriginalSizeInBytes);
        Assert.Equal(compressible, assembled.Metadata?.IsCompressed);
    }

    // ---------------------------------------------------------------------------------------------------
    // Criterion 1: through the real renderer and the real reader, one image per symbol — the shape you get
    // from photographing codes one at a time, or from exporting a single symbol.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public async Task Recover_FromOneImagePerSymbol_RebuildsTheFile(ChunkSizePreset preset)
    {
        byte[] content = RecoveryFixtures.Incompressible(4_000);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(
            content,
            options: EncodeOptions.FromPreset(preset));
        RecoverySession session = new();

        foreach (string code in RecoveryFixtures.Shuffled(backup.Codes))
        {
            ImageScanResult result = RecoveryFixtures.AddImage(session, RecoveryFixtures.RenderSymbol(code));

            Assert.True(result.IsImageReadable);
            Assert.Equal(1, result.CodesFound);
            Assert.Equal(1, result.AcceptedCount);
        }

        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(content, assembled.Content);
        Assert.Equal(backup.Metadata, assembled.Metadata);
    }

    // Criterion 1 again, in the shape a recovery really arrives in: whole sheets, each holding the grid of
    // symbols the page layout puts on it, imported one page at a time.
    [Fact]
    public async Task Recover_FromScannedPages_RebuildsTheFile()
    {
        byte[] content = RecoveryFixtures.Incompressible(PagedFixtureSizeInBytes);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content);
        PageLayout layout = PageLayout.ForBackup(backup);
        Assert.True(layout.PageCount > 1, "the fixture must span more than one sheet");
        RecoverySession session = new();

        int printed = 0;
        for (int page = 1; page <= layout.PageCount; page++)
        {
            int count = layout.GetCodeCountOnPage(page);

            ImageScanResult result = RecoveryFixtures.AddImage(session, RenderPage(backup, layout, printed, count));

            Assert.True(result.IsImageReadable);
            Assert.Equal(count, result.CodesFound);
            Assert.Equal(count, result.AcceptedCount);
            printed += count;
        }

        Assert.Equal(backup.Codes.Count, printed);
        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(content, assembled.Content);
    }

    // The mixed-source case the plan asks for, and the one a real recovery hits: one symbol on the sheet will
    // not read — a fold, a smudge, a coffee ring — so the user types that code in instead.
    [Fact]
    public async Task Recover_MixingAScannedPageWithACodeTypedByHand_RebuildsTheFile()
    {
        byte[] content = RecoveryFixtures.Incompressible(5_000);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content);
        const int unreadable = 3;
        RecoverySession session = new();

        RecoveryFixtures.AddImage(
            session,
            RecoveryFixtures.RenderPage([.. backup.Codes.Where((_, index) => index != unreadable)]));

        RecoveryStatus status = session.Status;
        Assert.False(status.IsComplete);
        Assert.Equal(new[] { unreadable }, status.MissingIndexes);
        Assert.Equal(AssemblyOutcome.Incomplete, session.TryAssemble().Outcome);

        // Typed off the page, in lower case, as anyone would.
        Assert.True(session.AddCode(backup.Codes[unreadable].ToLowerInvariant()).IsAccepted);

        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(content, assembled.Content);
    }

    [Fact]
    public async Task Recover_TheSamePageImportedTwice_AddsNothingTheSecondTime()
    {
        byte[] content = RecoveryFixtures.Incompressible(5_000);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content);
        byte[] page = RecoveryFixtures.RenderPage(backup.Codes);
        RecoverySession session = new();

        ImageScanResult first = RecoveryFixtures.AddImage(session, page);
        ImageScanResult second = RecoveryFixtures.AddImage(session, page);

        Assert.Equal(backup.Codes.Count, first.AcceptedCount);
        Assert.Equal(backup.Codes.Count, second.CodesFound);
        Assert.Equal(0, second.AcceptedCount);
        Assert.All(second.Results, result => Assert.Equal(AddCodeOutcome.Duplicate, result.Outcome));
        Assert.Equal(backup.Codes.Count - 1, session.Status.ReceivedChunks);
        Assert.True(session.TryAssemble().HashVerified);
    }

    // Criterion 3's wrong-backup scenario at the scale it actually happens: a whole sheet from a different
    // backup, scanned into the pile by mistake. Every code on it is refused, and the recovery in progress is
    // untouched.
    [Fact]
    public async Task Recover_APageFromAnotherBackup_IsRejectedCodeByCode()
    {
        byte[] content = RecoveryFixtures.Incompressible(5_000);
        EncodedBackup mine = await RecoveryFixtures.EncodeAsync(content);
        EncodedBackup theirs = await RecoveryFixtures.EncodeAsync(
            RecoveryFixtures.Incompressible(5_001),
            fileName: "someone-elses.key",
            backupId: RecoveryFixtures.ForeignBackupId);
        RecoverySession session = new();
        RecoveryFixtures.AddImage(session, RecoveryFixtures.RenderPage(mine.Codes));

        ImageScanResult intruder = RecoveryFixtures.AddImage(session, RecoveryFixtures.RenderPage(theirs.Codes));

        Assert.True(intruder.IsImageReadable);
        Assert.Equal(theirs.Codes.Count, intruder.CodesFound);
        Assert.Equal(0, intruder.AcceptedCount);
        Assert.All(
            intruder.Results,
            result =>
            {
                Assert.Equal(AddCodeOutcome.WrongBackup, result.Outcome);
                Assert.Equal(RecoveryFixtures.ForeignBackupId, result.BackupId);
            });

        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(content, assembled.Content);
        Assert.Equal(RecoveryFixtures.FileName, assembled.Metadata?.FileName);
    }

    // Criterion 4: the compression decision survives the round trip in both directions, and it is what
    // decides how much paper the backup takes.
    [Fact]
    public async Task Recover_ACompressibleFile_TravelsInFewerCodesAndStillVerifies()
    {
        byte[] text = RecoveryFixtures.Compressible();
        EncodedBackup compressed = await RecoveryFixtures.EncodeAsync(text);
        EncodedBackup stored = await RecoveryFixtures.EncodeAsync(RecoveryFixtures.Incompressible(text.Length));
        RecoverySession session = new();

        RecoveryFixtures.AddAll(session, compressed.Codes);

        Assert.True(compressed.Metadata.IsCompressed);
        Assert.False(stored.Metadata.IsCompressed);
        Assert.True(
            compressed.Codes.Count < stored.Codes.Count,
            $"{compressed.Codes.Count} codes should be fewer than {stored.Codes.Count} for the same {text.Length} bytes");
        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(text, assembled.Content);
    }

    // The file name is the one metadata field a recovery has to hand back verbatim, and it is the field most
    // able to break the format: the separator, the escape character, and bytes that are not ASCII at all.
    [Theory]
    [InlineData("keys.kdbx")]
    [InlineData("my|weird|name.key")]
    [InlineData("100%|pure.txt")]
    [InlineData("clé privée — sauvegarde.pem")]
    [InlineData("файл-ключа.txt")]
    [InlineData("emoji 🔐 backup.key")]
    public async Task Recover_AnExoticFileName_ComesBackIntact(string fileName)
    {
        byte[] content = RecoveryFixtures.Incompressible(2_000);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content, fileName);
        RecoverySession session = new();

        RecoveryFixtures.AddAll(session, RecoveryFixtures.Shuffled(backup.Codes));

        AssemblyResult assembled = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, assembled.Outcome);
        Assert.Equal(fileName, assembled.Metadata?.FileName);
        Assert.Equal(content, assembled.Content);
    }

    /// <summary>
    /// Renders the codes that belong on one sheet, in the grid the page layout puts them in.
    /// </summary>
    private static byte[] RenderPage(EncodedBackup backup, PageLayout layout, int printed, int count)
        => RecoveryFixtures.RenderPage(
            [.. backup.Codes.Skip(printed).Take(count)],
            columns: layout.Columns);
}
