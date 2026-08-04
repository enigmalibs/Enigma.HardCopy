using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class RecoverViewModelTests
{
    [Fact]
    public void SplitCodes_OfNothing_FindsNothing()
    {
        Assert.Empty(RecoverViewModel.SplitCodes(null));
        Assert.Empty(RecoverViewModel.SplitCodes(string.Empty));
        Assert.Empty(RecoverViewModel.SplitCodes("   \r\n  "));
        Assert.Empty(RecoverViewModel.SplitCodes("just some prose with no code in it"));
    }

    [Fact]
    public void SplitCodes_OfOneCodeWrappedOverLines_FindsOneCode()
    {
        IReadOnlyList<string> codes = RecoverViewModel.SplitCodes("EHC1:ABCD:1/2:\nDEADBEEF:AAAA\nBBBB");

        Assert.Single(codes);
        Assert.StartsWith("EHC1:ABCD:1/2:", codes[0], StringComparison.Ordinal);
    }

    [Fact]
    public void SplitCodes_OfSeveralCodes_SplitsOnTheFormatsOwnPrefix()
    {
        IReadOnlyList<string> codes = RecoverViewModel.SplitCodes(
            "EHC1:ABCD:1/2:DEADBEEF:AAAA\nEHC1:ABCD:2/2:CAFEBABE:BBBB");

        Assert.Equal(2, codes.Count);
        Assert.Contains("1/2", codes[0], StringComparison.Ordinal);
        Assert.Contains("2/2", codes[1], StringComparison.Ordinal);
    }

    [Fact]
    public void SplitCodes_IsCaseInsensitive_BecauseTheSessionNormalizesAnyway()
        => Assert.Single(RecoverViewModel.SplitCodes("ehc1:abcd:1/2:deadbeef:aaaa"));

    [Fact]
    public void SplitCodes_IgnoresWhateverPrecedesTheFirstCode()
    {
        IReadOnlyList<string> codes = RecoverViewModel.SplitCodes("scanned from page 2:\nEHC1:ABCD:1/2:DEADBEEF:AAAA");

        Assert.Single(codes);
        Assert.StartsWith("EHC1", codes[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ANewSession_KnowsNothing()
    {
        (RecoverViewModel viewModel, _) = Create();

        Assert.False(viewModel.HasAnyCode);
        Assert.False(viewModel.HasMetadata);
        Assert.False(viewModel.IsComplete);
        Assert.Null(viewModel.BackupId);
        Assert.Null(viewModel.FileName);
        Assert.Equal(Strings.RecoverChunksUnknown, viewModel.CodesText);
        Assert.Equal(Strings.RecoverMissingNothingKnown, viewModel.MissingText);
        Assert.Equal(0d, viewModel.ProgressPercent);
        Assert.False(viewModel.AssembleCommand.CanExecute(null));
        Assert.False(viewModel.AddCodesCommand.CanExecute(null));
        Assert.False(viewModel.CanSaveAnyway);
        Assert.Empty(viewModel.Activity);
    }

    [Fact]
    public async Task AddCodes_TakesTheWholeBackup_AndReportsItComplete()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, backup.Codes);

        Assert.True(viewModel.HasAnyCode);
        Assert.True(viewModel.HasMetadata);
        Assert.True(viewModel.IsComplete);
        Assert.Equal(BackupFixtures.BackupId, viewModel.BackupId);
        Assert.Equal(BackupFixtures.FileName, viewModel.FileName);
        Assert.Equal(backup.Metadata.Sha256Hex, viewModel.ExpectedSha256);
        Assert.Equal(Strings.RecoverMissingNone, viewModel.MissingText);
        Assert.Equal(100d, viewModel.ProgressPercent);
        Assert.True(viewModel.AssembleCommand.CanExecute(null));
        Assert.Null(viewModel.ManualEntry);
    }

    [Fact]
    public async Task AddCodes_OutOfOrder_TracksWhatIsStillMissing()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        // Last data code first, metadata block last — the order a pile of scanned sheets arrives in.
        AddCodes(viewModel, [backup.Codes[^1], backup.Codes[0]]);

        Assert.True(viewModel.HasMetadata);
        Assert.False(viewModel.IsComplete);
        Assert.False(viewModel.AssembleCommand.CanExecute(null));
        Assert.Contains("1", viewModel.MissingText, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.RecoverMissingNone, viewModel.MissingText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCodes_Twice_ReportsTheSecondAsADuplicate_AndChangesNothing()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, backup.Codes);
        string missingBefore = viewModel.MissingText;
        AddCodes(viewModel, [backup.Codes[1]]);

        Assert.Equal(MessageSeverity.Information, viewModel.Activity[0].Severity);
        Assert.Equal(missingBefore, viewModel.MissingText);
        Assert.True(viewModel.IsComplete);
    }

    [Fact]
    public async Task AddCodes_FromAnotherBackup_RejectsThemAndNamesTheForeignId()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup mine = await BackupFixtures.EncodeAsync(content);
        EncodedBackup theirs = await BackupFixtures.EncodeAsync(content, backupId: "WXYZ");
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, [mine.Codes[0]]);
        AddCodes(viewModel, [theirs.Codes[1]]);

        Assert.Equal(MessageSeverity.Warning, viewModel.Activity[0].Severity);
        Assert.Contains("WXYZ", viewModel.Activity[0].Text, StringComparison.Ordinal);
        Assert.Equal(BackupFixtures.BackupId, viewModel.BackupId);
        Assert.Equal(0, ReceivedChunks(viewModel));
    }

    [Fact]
    public async Task AddCodes_WithADamagedPayload_ReportsTheIndexToRescan()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, [backup.Codes[0], BackupFixtures.DamagePayload(backup.Codes[1])]);

        Assert.Equal(MessageSeverity.Error, viewModel.Activity[0].Severity);
        Assert.Contains("1", viewModel.Activity[0].Text, StringComparison.Ordinal);
        Assert.Equal(0, ReceivedChunks(viewModel));
        Assert.False(viewModel.IsComplete);
    }

    [Fact]
    public void AddCodes_WithTextThatIsNoCode_SaysSoWithoutTouchingTheSession()
    {
        (RecoverViewModel viewModel, _) = Create();
        viewModel.ManualEntry = "the third line of the second page";

        Assert.True(viewModel.AddCodesCommand.CanExecute(null));
        viewModel.AddCodesCommand.Execute(null);

        Assert.NotNull(viewModel.Message);
        Assert.Equal(Strings.RecoverManualNoCodesFound, viewModel.Message.Text);
        Assert.Empty(viewModel.Activity);
        Assert.False(viewModel.HasAnyCode);
        Assert.Equal("the third line of the second page", viewModel.ManualEntry);
    }

    [Fact]
    public void AddCodes_WithNothingTyped_SaysThereIsNothingToAdd()
    {
        (RecoverViewModel viewModel, _) = Create();

        viewModel.AddCodesCommand.Execute(null);

        Assert.NotNull(viewModel.Message);
        Assert.Equal(Strings.RecoverManualEmpty, viewModel.Message.Text);
    }

    [Fact]
    public void AddCodes_WithSomethingThatOnlyLooksLikeACode_IsMalformed()
    {
        (RecoverViewModel viewModel, _) = Create();
        viewModel.ManualEntry = "EHC1:nonsense";

        viewModel.AddCodesCommand.Execute(null);

        Assert.Single(viewModel.Activity);
        Assert.Equal(MessageSeverity.Error, viewModel.Activity[0].Severity);
        Assert.Equal(Strings.CodeMalformed, viewModel.Activity[0].Text);
        Assert.False(viewModel.HasAnyCode);
    }

    [Fact]
    public async Task Assemble_WhenTheHashMatches_SavesTheOriginalBytes()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        dialogs.RecoveredDestination = target;

        AddCodes(viewModel, backup.Codes);
        await viewModel.AssembleCommand.ExecuteAsync(null);

        Assert.Equal(BackupFixtures.FileName, dialogs.SuggestedRecoveredName);
        Assert.Equal(content, target.Written);
        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Success, viewModel.Message.Severity);
        Assert.False(viewModel.CanSaveAnyway);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Assemble_WhenTheSaveDialogIsDismissed_StillReportsTheRecoveryVerified()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        dialogs.RecoveredDestination = null;

        AddCodes(viewModel, backup.Codes);
        await viewModel.AssembleCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.Message);
        Assert.Equal(Strings.AssemblyVerified, viewModel.Message.Text);
        Assert.Equal(MessageSeverity.Success, viewModel.Message.Severity);
    }

    [Fact]
    public async Task Assemble_WhenTheHashDoesNotMatch_SavesNothing_AndOffersTheEscapeHatch()
    {
        MismatchedRecovery recovery = await CreateWithMismatchedHashAsync();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        recovery.Dialogs.RecoveredDestination = target;

        await recovery.ViewModel.AssembleCommand.ExecuteAsync(null);

        Assert.Null(target.Written);
        Assert.Equal(0, recovery.Dialogs.RecoveredDestinationCalls);
        Assert.True(recovery.ViewModel.CanSaveAnyway);
        Assert.True(recovery.ViewModel.SaveAnywayCommand.CanExecute(null));
        StatusMessage? message = recovery.ViewModel.Message;
        Assert.NotNull(message);
        Assert.Equal(MessageSeverity.Warning, message.Severity);

        // Both hashes: the one recorded in the backup, and the one the rebuilt bytes actually have.
        Assert.Contains(recovery.Tampered.Sha256Hex, message.Text, StringComparison.Ordinal);
        Assert.Contains(recovery.Backup.Metadata.Sha256Hex, message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAnyway_WritesTheUnverifiedBytes_AndClosesTheEscapeHatch()
    {
        MismatchedRecovery recovery = await CreateWithMismatchedHashAsync();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        recovery.Dialogs.RecoveredDestination = target;

        await recovery.ViewModel.AssembleCommand.ExecuteAsync(null);
        await recovery.ViewModel.SaveAnywayCommand.ExecuteAsync(null);

        Assert.Equal(recovery.Content, target.Written);
        StatusMessage? message = recovery.ViewModel.Message;
        Assert.NotNull(message);
        Assert.Equal(MessageSeverity.Warning, message.Severity);
        Assert.False(recovery.ViewModel.CanSaveAnyway);
    }

    [Fact]
    public async Task AnyNewCode_InvalidatesTheUnverifiedResult()
    {
        MismatchedRecovery recovery = await CreateWithMismatchedHashAsync();

        await recovery.ViewModel.AssembleCommand.ExecuteAsync(null);
        Assert.True(recovery.ViewModel.CanSaveAnyway);

        AddCodes(recovery.ViewModel, [recovery.Backup.Codes[1]]);

        Assert.False(recovery.ViewModel.CanSaveAnyway);
        Assert.False(recovery.ViewModel.SaveAnywayCommand.CanExecute(null));
    }

    [Fact]
    public async Task Assemble_WhenTheBackupIsMarkedEncrypted_RefusesIt()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        dialogs.RecoveredDestination = target;

        AddCodes(viewModel, [.. backup.Codes.Skip(1)]);
        AddCodes(viewModel, [BackupFixtures.MetadataCodeWith(backup, backup.Metadata with { IsEncrypted = true })]);
        await viewModel.AssembleCommand.ExecuteAsync(null);

        Assert.Null(target.Written);
        Assert.False(viewModel.CanSaveAnyway);
        Assert.NotNull(viewModel.Message);
        Assert.Equal(Strings.AssemblyEncryptionUnsupported, viewModel.Message.Text);
        Assert.Equal(MessageSeverity.Error, viewModel.Message.Severity);
    }

    [Fact]
    public async Task Assemble_WhenTheWriteFails_ReportsIt()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        dialogs.RecoveredDestination = FakeSaveTarget.Failing(
            BackupFixtures.FileName,
            new IOException("no space left on device"));

        AddCodes(viewModel, backup.Codes);
        await viewModel.AssembleCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Error, viewModel.Message.Severity);
        Assert.Contains("no space left on device", viewModel.Message.Text, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ImportImages_ReadsTheCodesOffThePages()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        dialogs.Images = [.. backup.Codes.Select((code, index) =>
            new FakePickedFile($"page-{index}.png", BackupFixtures.RenderPng(code)))];

        await viewModel.ImportImagesCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsComplete);
        Assert.Equal(BackupFixtures.BackupId, viewModel.BackupId);
        Assert.True(viewModel.AssembleCommand.CanExecute(null));
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ImportFiles_IsTheSamePathAsThePicker_ForDroppedFiles()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();
        IReadOnlyList<IPickedFile> dropped = [.. backup.Codes.Select((code, index) =>
            new FakePickedFile($"drop-{index}.png", BackupFixtures.RenderPng(code)))];

        await viewModel.ImportFilesCommand.ExecuteAsync(dropped);

        Assert.True(viewModel.IsComplete);
    }

    [Fact]
    public async Task ImportImages_WhenAFileIsNotAnImage_SaysSo()
    {
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        dialogs.Images = [new FakePickedFile("notes.txt", Encoding.UTF8.GetBytes("this is not a picture"))];

        await viewModel.ImportImagesCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Activity);
        Assert.Equal(MessageSeverity.Error, viewModel.Activity[0].Severity);
        Assert.Contains("notes.txt", viewModel.Activity[0].Text, StringComparison.Ordinal);
        Assert.False(viewModel.HasAnyCode);
    }

    [Fact]
    public async Task ImportImages_WhenOneFileCannotBeRead_ReportsItAndKeepsGoing()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();
        dialogs.Images =
        [
            FakePickedFile.Failing("page-1.png", new IOException("the scanner went away")),
            new FakePickedFile("page-2.png", BackupFixtures.RenderPng(backup.Codes[0])),
        ];

        await viewModel.ImportImagesCommand.ExecuteAsync(null);

        Assert.Contains(
            viewModel.Activity,
            entry => entry.Severity is MessageSeverity.Error
                && entry.Text.Contains("the scanner went away", StringComparison.Ordinal));
        Assert.True(viewModel.HasMetadata);
    }

    [Fact]
    public async Task StartOver_ForgetsEverything()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, backup.Codes);
        Assert.True(viewModel.IsComplete);

        viewModel.StartOverCommand.Execute(null);

        Assert.False(viewModel.HasAnyCode);
        Assert.False(viewModel.HasMetadata);
        Assert.False(viewModel.IsComplete);
        Assert.Null(viewModel.BackupId);
        Assert.Null(viewModel.Message);
        Assert.Null(viewModel.ManualEntry);
        Assert.Empty(viewModel.Activity);
        Assert.Equal(Strings.RecoverMissingNothingKnown, viewModel.MissingText);
        Assert.False(viewModel.AssembleCommand.CanExecute(null));
    }

    [Fact]
    public async Task MissingText_IsTruncated_WhenTooManyCodesAreMissing()
    {
        // One code per chunk, with far more chunks than the list is willing to print.
        byte[] content = BackupFixtures.IncompressibleBytes(512 * (RecoverViewModel.MaxListedMissingIndexes + 5));
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        (RecoverViewModel viewModel, _) = Create();

        AddCodes(viewModel, [backup.Codes[0]]);

        Assert.True(backup.Codes.Count - 1 > RecoverViewModel.MaxListedMissingIndexes, "the fixture is too small");
        Assert.Contains("5 more", viewModel.MissingText, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new RecoverViewModel(null!, NullLogger<RecoverViewModel>.Instance));
        Assert.Throws<ArgumentNullException>(() => new RecoverViewModel(new FakeFileDialogService(), null!));
    }

    private static (RecoverViewModel ViewModel, FakeFileDialogService Dialogs) Create()
    {
        FakeFileDialogService dialogs = new();

        return (new RecoverViewModel(dialogs, NullLogger<RecoverViewModel>.Instance), dialogs);
    }

    /// <summary>
    /// Builds a session holding every data code plus a metadata block that records the wrong hash — a recovery
    /// that rebuilds bytes which are provably not the file that was backed up.
    /// </summary>
    private static async Task<MismatchedRecovery> CreateWithMismatchedHashAsync()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        BackupMetadata tampered = backup.Metadata with { Sha256Hex = new string('0', 64) };
        (RecoverViewModel viewModel, FakeFileDialogService dialogs) = Create();

        AddCodes(viewModel, [.. backup.Codes.Skip(1)]);
        AddCodes(viewModel, [BackupFixtures.MetadataCodeWith(backup, tampered)]);
        Assert.True(viewModel.IsComplete);

        return new MismatchedRecovery(viewModel, dialogs, backup, tampered, content);
    }

    /// <summary>Feeds codes in the way a user pasting them does — one text block, split by the ViewModel.</summary>
    private static void AddCodes(RecoverViewModel viewModel, IReadOnlyList<string> codes)
    {
        viewModel.ManualEntry = string.Join(Environment.NewLine, codes);
        viewModel.AddCodesCommand.Execute(null);
    }

    private static int ReceivedChunks(RecoverViewModel viewModel)
        => viewModel.CodesText == Strings.RecoverChunksUnknown
            ? 0
            : int.Parse(viewModel.CodesText.Split(' ')[0], CultureInfo.CurrentCulture);

    /// <summary>A recovery sitting on rebuilt bytes whose hash does not match what the backup recorded.</summary>
    private sealed record MismatchedRecovery(
        RecoverViewModel ViewModel,
        FakeFileDialogService Dialogs,
        EncodedBackup Backup,
        BackupMetadata Tampered,
        byte[] Content);
}
