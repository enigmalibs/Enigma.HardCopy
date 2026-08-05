using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// What the recovery page hands to the shell: the modal card each run goes behind, the one notification each
/// outcome produces, and the two questions asked before something that cannot be taken back.
/// </summary>
/// <remarks>
/// The unverified save is the case that matters most. Declining has to cost the user nothing — no picker, no
/// bytes, and the offer still on screen — because the whole reason the escape hatch is guarded is that it is
/// one click away from the button a frustrated user has just been told not to press.
/// </remarks>
public sealed class RecoverViewModelShellTests
{
    [Fact]
    public async Task ImportImages_RaisesTheImportCardOnce_WithNoWayOut()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();
        shell.Dialogs.Images = [.. backup.Codes.Select((code, index) =>
            new FakePickedFile($"page-{index}.png", BackupFixtures.RenderPng(code)))];

        await shell.ViewModel.ImportImagesCommand.ExecuteAsync(null);

        Assert.Equal(1, shell.Overlay.Shows);
        Assert.Equal(1, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(Strings.OverlayImportTitle, Assert.Single(shell.Overlay.Titles));
        Assert.Contains(Strings.RecoverStageImporting, shell.Overlay.Stages);

        // Importing takes no cancellation token, so the card must not pretend otherwise.
        Assert.False(shell.Overlay.LastCardOfferedCancel);
    }

    [Fact]
    public async Task ImportImages_WithNothingChosen_RaisesNothing()
    {
        Shell shell = Create();
        shell.Dialogs.Images = [];

        await shell.ViewModel.ImportImagesCommand.ExecuteAsync(null);

        Assert.Equal(0, shell.Overlay.Shows);
        Assert.Empty(shell.Notifications.Published);
    }

    [Fact]
    public async Task Assemble_RaisesTheRebuildCardThenTheSaveCard_AndTakesBothDown()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();
        shell.Dialogs.RecoveredDestination = new FakeSaveTarget(BackupFixtures.FileName);

        AddCodes(shell.ViewModel, backup.Codes);
        await shell.ViewModel.AssembleCommand.ExecuteAsync(null);

        Assert.Equal([Strings.OverlayAssembleTitle, Strings.OverlaySaveTitle], shell.Overlay.Titles);
        Assert.Equal(2, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.False(shell.Overlay.LastCardOfferedCancel);
    }

    [Fact]
    public async Task Assemble_WhenVerified_PublishesExactlyOneSuccess()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();
        shell.Dialogs.RecoveredDestination = new FakeSaveTarget(BackupFixtures.FileName);

        AddCodes(shell.ViewModel, backup.Codes);
        await shell.ViewModel.AssembleCommand.ExecuteAsync(null);

        StatusMessage published = Assert.Single(shell.Notifications.Published);
        Assert.Equal(MessageSeverity.Success, published.Severity);
        Assert.Same(shell.ViewModel.Message, published);
    }

    [Fact]
    public async Task Assemble_WhenTheHashDoesNotMatch_PublishesExactlyOneWarning()
    {
        Mismatch mismatch = await CreateMismatchAsync();

        await mismatch.Shell.ViewModel.AssembleCommand.ExecuteAsync(null);

        StatusMessage published = Assert.Single(mismatch.Shell.Notifications.Published);
        Assert.Equal(MessageSeverity.Warning, published.Severity);
    }

    [Fact]
    public async Task AddCodes_PublishesNothing_BecauseTheLogIsNotABanner()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1400);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();

        AddCodes(shell.ViewModel, backup.Codes);

        Assert.NotEmpty(shell.ViewModel.Activity);
        Assert.Empty(shell.Notifications.Published);
    }

    [Fact]
    public async Task SaveAnyway_AsksBeforeThePicker_AndNamesBothHashes()
    {
        Mismatch mismatch = await CreateMismatchAsync();
        mismatch.Shell.Dialogs.RecoveredDestination = new FakeSaveTarget(BackupFixtures.FileName);

        await mismatch.Shell.ViewModel.AssembleCommand.ExecuteAsync(null);
        await mismatch.Shell.ViewModel.SaveAnywayCommand.ExecuteAsync(null);

        (string expected, string actual) = Assert.Single(mismatch.Shell.Confirmations.UnverifiedSaveAsks);
        Assert.Equal(mismatch.Tampered.Sha256Hex, expected);
        Assert.Equal(mismatch.ActualSha256Hex, actual);
    }

    [Fact]
    public async Task SaveAnyway_WhenDeclined_WritesNothing_AndLeavesTheOfferStanding()
    {
        Mismatch mismatch = await CreateMismatchAsync();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        mismatch.Shell.Dialogs.RecoveredDestination = target;
        mismatch.Shell.Confirmations.AgreeToUnverifiedSave = false;

        await mismatch.Shell.ViewModel.AssembleCommand.ExecuteAsync(null);
        int picksAfterAssembly = mismatch.Shell.Dialogs.RecoveredDestinationCalls;
        await mismatch.Shell.ViewModel.SaveAnywayCommand.ExecuteAsync(null);

        Assert.Equal(1, mismatch.Shell.Confirmations.UnverifiedSaveCalls);
        Assert.Null(target.Written);

        // Refusing must not even open the destination picker: the decision is made before the file system is.
        Assert.Equal(picksAfterAssembly, mismatch.Shell.Dialogs.RecoveredDestinationCalls);

        Assert.True(mismatch.Shell.ViewModel.CanSaveAnyway);
        Assert.True(mismatch.Shell.ViewModel.SaveAnywayCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAnyway_WhenAgreed_WritesTheBytes_AndWithdrawsTheOffer()
    {
        Mismatch mismatch = await CreateMismatchAsync();
        FakeSaveTarget target = new(BackupFixtures.FileName);
        mismatch.Shell.Dialogs.RecoveredDestination = target;

        await mismatch.Shell.ViewModel.AssembleCommand.ExecuteAsync(null);
        await mismatch.Shell.ViewModel.SaveAnywayCommand.ExecuteAsync(null);

        Assert.Equal(1, mismatch.Shell.Confirmations.UnverifiedSaveCalls);
        Assert.Equal(mismatch.Content, target.Written);
        Assert.False(mismatch.Shell.ViewModel.CanSaveAnyway);
        Assert.Equal(MessageSeverity.Warning, mismatch.Shell.Notifications.Published[^1].Severity);
    }

    [Fact]
    public void StartOver_WithNothingRead_AsksNothing()
    {
        Shell shell = Create();

        shell.ViewModel.StartOverCommand.Execute(null);

        Assert.Equal(0, shell.Confirmations.StartOverCalls);
        Assert.False(shell.ViewModel.HasAnyCode);
    }

    [Fact]
    public async Task StartOver_WithCodesRead_Asks_AndKeepsEverythingWhenDeclined()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();
        shell.Confirmations.AgreeToStartOver = false;

        AddCodes(shell.ViewModel, backup.Codes);
        int entries = shell.ViewModel.Activity.Count;
        await shell.ViewModel.StartOverCommand.ExecuteAsync(null);

        Assert.Equal(1, shell.Confirmations.StartOverCalls);
        Assert.True(shell.ViewModel.HasAnyCode);
        Assert.True(shell.ViewModel.IsComplete);
        Assert.Equal(entries, shell.ViewModel.Activity.Count);
    }

    [Fact]
    public async Task StartOver_WithCodesRead_ForgetsEverythingWhenAgreed()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        Shell shell = Create();

        AddCodes(shell.ViewModel, backup.Codes);
        await shell.ViewModel.StartOverCommand.ExecuteAsync(null);

        Assert.Equal(1, shell.Confirmations.StartOverCalls);
        Assert.False(shell.ViewModel.HasAnyCode);
        Assert.Empty(shell.ViewModel.Activity);
    }

    private static Shell Create()
    {
        FakeFileDialogService dialogs = new();
        FakeProgressOverlay overlay = new();
        FakeNotificationService notifications = new();
        FakeConfirmationService confirmations = new();
        RecoverViewModel viewModel = new(
            dialogs,
            NullLogger<RecoverViewModel>.Instance,
            overlay,
            notifications,
            confirmations);

        return new Shell(viewModel, dialogs, overlay, notifications, confirmations);
    }

    /// <summary>
    /// A session holding every code, whose metadata block records a hash the rebuilt bytes do not have — the
    /// state in which the escape hatch, and therefore its guard, exists at all.
    /// </summary>
    private static async Task<Mismatch> CreateMismatchAsync()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(600);
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        BackupMetadata tampered = backup.Metadata with { Sha256Hex = new string('0', 64) };
        Shell shell = Create();

        AddCodes(shell.ViewModel, [.. backup.Codes.Skip(1)]);
        AddCodes(shell.ViewModel, [BackupFixtures.MetadataCodeWith(backup, tampered)]);
        Assert.True(shell.ViewModel.IsComplete);

        return new Mismatch(shell, tampered, backup.Metadata.Sha256Hex, content);
    }

    private static void AddCodes(RecoverViewModel viewModel, IReadOnlyList<string> codes)
    {
        viewModel.ManualEntry = string.Join(Environment.NewLine, codes);
        viewModel.AddCodesCommand.Execute(null);
    }

    /// <summary>The page under test and the four doubles it reports through.</summary>
    private sealed record Shell(
        RecoverViewModel ViewModel,
        FakeFileDialogService Dialogs,
        FakeProgressOverlay Overlay,
        FakeNotificationService Notifications,
        FakeConfirmationService Confirmations);

    /// <summary>A recovery sitting on bytes that failed their check, and the two hashes that prove it.</summary>
    /// <param name="Shell">The page and its doubles.</param>
    /// <param name="Tampered">The metadata the session read, carrying the wrong hash.</param>
    /// <param name="ActualSha256Hex">The hash the rebuilt bytes actually have — the real backup's.</param>
    /// <param name="Content">Those bytes.</param>
    private sealed record Mismatch(Shell Shell, BackupMetadata Tampered, string ActualSha256Hex, byte[] Content);
}
