using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// What the backup page hands to the shell: the modal card a run goes behind, and the one notification each
/// outcome produces.
/// </summary>
/// <remarks>
/// The scrim is the dangerous half. Left up, it blocks the whole window and the application looks hung, so
/// every path out of a run — success, refusal, failure and cancellation alike — is asserted to take it down,
/// and to have raised it exactly once on the way in.
/// </remarks>
public sealed class BackupViewModelShellTests
{
    [Fact]
    public async Task ChooseFile_RaisesTheReadingCard_AndTakesItDown()
    {
        Shell shell = Create();
        shell.Dialogs.FileToBackUp = new FakePickedFile("keys.kdbx", BackupFixtures.IncompressibleBytes(2048));

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal(1, shell.Overlay.Shows);
        Assert.Equal(1, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(Strings.OverlayReadTitle, Assert.Single(shell.Overlay.Titles));
        Assert.Contains(Strings.BackupStageReading, shell.Overlay.Stages);

        // Reading has no cancellation token behind it, so the card must offer no way out.
        Assert.False(shell.Overlay.LastCardOfferedCancel);
    }

    [Fact]
    public async Task ChooseFile_WhenTheDialogIsDismissed_RaisesNothingAndSaysNothing()
    {
        Shell shell = Create();
        shell.Dialogs.FileToBackUp = null;

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal(0, shell.Overlay.Shows);
        Assert.Empty(shell.Notifications.Published);
    }

    [Fact]
    public async Task ChooseFile_WhenTheReadFails_StillTakesTheCardDown_AndReportsItOnce()
    {
        Shell shell = Create();
        shell.Dialogs.FileToBackUp = FakePickedFile.Failing("locked.key", new System.IO.IOException("device is busy"));

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal(1, shell.Overlay.Shows);
        Assert.Equal(1, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(MessageSeverity.Error, Assert.Single(shell.Notifications.Published).Severity);
    }

    [Fact]
    public async Task Generate_RaisesTheGenerationCardOnce_WithAWayOut_AndCarriesEveryStage()
    {
        Shell shell = Create(new PdfComposer());
        shell.Dialogs.FileToBackUp = new FakePickedFile("secret.key", BackupFixtures.IncompressibleBytes(1500));
        shell.Dialogs.PdfDestination = new FakeSaveTarget("secret.hardcopy.pdf");

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);
        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);
        int showsAfterReading = shell.Overlay.Shows;
        await shell.ViewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(showsAfterReading + 1, shell.Overlay.Shows);
        Assert.Equal(shell.Overlay.Shows, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(Strings.OverlayGenerateTitle, shell.Overlay.Titles[^1]);

        // Generation is the one operation with a CancellationTokenSource, so the one card that offers cancel.
        Assert.True(shell.Overlay.LastCardOfferedCancel);
        Assert.Same(shell.ViewModel.CancelCommand, shell.Overlay.LastCancelCommand);

        Assert.Contains(Strings.BackupStageEncoding, shell.Overlay.Stages);
        Assert.Contains(Strings.BackupStageComposing, shell.Overlay.Stages);
        Assert.Contains(Strings.BackupStageWriting, shell.Overlay.Stages);
    }

    [Fact]
    public async Task Generate_PublishesExactlyOneSuccess()
    {
        Shell shell = Create(new PdfComposer());
        shell.Dialogs.FileToBackUp = new FakePickedFile("secret.key", BackupFixtures.IncompressibleBytes(1500));
        shell.Dialogs.PdfDestination = new FakeSaveTarget("secret.hardcopy.pdf");

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);
        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);
        await shell.ViewModel.GenerateCommand.ExecuteAsync(null);

        StatusMessage published = Assert.Single(shell.Notifications.Published);
        Assert.Equal(MessageSeverity.Success, published.Severity);
        Assert.Same(shell.ViewModel.Message, published);
    }

    [Fact]
    public async Task Generate_WhenTheWriteFails_TakesTheCardDown_AndPublishesExactlyOneError()
    {
        Shell shell = Create();
        shell.Dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        shell.Dialogs.PdfDestination = FakeSaveTarget.Failing(
            "out.pdf",
            new UnauthorizedAccessException("read-only volume"));

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);
        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);
        await shell.ViewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(shell.Overlay.Shows, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(MessageSeverity.Error, Assert.Single(shell.Notifications.Published).Severity);
    }

    [Fact]
    public async Task Generate_WhenCompositionThrows_StillTakesTheCardDown()
    {
        FakePdfComposer composer = new()
        {
            OnCompose = _ => throw new InvalidOperationException("the layout does not fit"),
        };
        Shell shell = Create(composer);
        shell.Dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        shell.Dialogs.PdfDestination = new FakeSaveTarget("out.pdf");

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);
        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);
        await shell.ViewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(shell.Overlay.Shows, shell.Overlay.Hides);
        Assert.False(shell.Overlay.IsShowing);
    }

    [Fact]
    public async Task Generate_WhenCancelled_StillTakesTheCardDown()
    {
        using ManualResetEventSlim composing = new();
        FakePdfComposer composer = new()
        {
            OnCompose = token =>
            {
                composing.Set();
                token.WaitHandle.WaitOne();
            },
        };
        Shell shell = Create(composer);
        shell.Dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        shell.Dialogs.PdfDestination = new FakeSaveTarget("out.pdf");

        await shell.ViewModel.ChooseFileCommand.ExecuteAsync(null);
        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);

        Task generation = shell.ViewModel.GenerateCommand.ExecuteAsync(null);
        composing.Wait(TestContext.Current.CancellationToken);
        Assert.True(shell.Overlay.IsShowing);
        shell.ViewModel.CancelCommand.Execute(null);
        await generation;

        Assert.False(shell.Overlay.IsShowing);
        Assert.Equal(shell.Overlay.Shows, shell.Overlay.Hides);
        Assert.Equal(MessageSeverity.Information, Assert.Single(shell.Notifications.Published).Severity);
    }

    [Fact]
    public async Task ChoosingADestination_IsNotARun_AndSaysNothing()
    {
        Shell shell = Create();
        shell.Dialogs.PdfDestination = new FakeSaveTarget("out.pdf");

        await shell.ViewModel.ChooseOutputCommand.ExecuteAsync(null);

        Assert.Equal(0, shell.Overlay.Shows);
        Assert.Empty(shell.Notifications.Published);
    }

    private static Shell Create(IPdfComposer? composer = null)
    {
        FakeFileDialogService dialogs = new();
        FakeProgressOverlay overlay = new();
        FakeNotificationService notifications = new();
        BackupViewModel viewModel = new(
            new BackupEncoder(new FixedBackupIdGenerator(BackupFixtures.BackupId), TimeProvider.System),
            composer ?? new FakePdfComposer(),
            dialogs,
            NullLogger<BackupViewModel>.Instance,
            overlay,
            notifications);

        return new Shell(viewModel, dialogs, overlay, notifications);
    }

    /// <summary>The page under test and the three doubles it reports through.</summary>
    private sealed record Shell(
        BackupViewModel ViewModel,
        FakeFileDialogService Dialogs,
        FakeProgressOverlay Overlay,
        FakeNotificationService Notifications);
}
