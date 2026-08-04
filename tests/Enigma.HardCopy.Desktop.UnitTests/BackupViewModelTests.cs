using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class BackupViewModelTests
{
    [Fact]
    public void Generate_IsDisabled_UntilBothAFileAndADestinationAreChosen()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();

        Assert.False(viewModel.GenerateCommand.CanExecute(null));
        Assert.False(viewModel.HasFile);
        Assert.False(viewModel.HasOutput);
        Assert.False(viewModel.CancelCommand.CanExecute(null));

        dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        Assert.False(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public async Task ChooseFile_PublishesTheNameSizeHashAndEstimate()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(4096);
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("keys.kdbx", content);

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasFile);
        Assert.Equal("keys.kdbx", viewModel.FileName);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), viewModel.Sha256Hex);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, Strings.BackupBytesFormat, content.Length),
            viewModel.SizeText);
        Assert.NotNull(viewModel.EstimateText);
        Assert.Null(viewModel.Message);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ChooseFile_WhenTheDialogIsDismissed_ChangesNothing()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = null;

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasFile);
        Assert.Null(viewModel.EstimateText);
        Assert.Null(viewModel.Message);
    }

    [Fact]
    public async Task ChooseFile_WhenTheReadFails_ReportsItAndKeepsNoFile()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = FakePickedFile.Failing("locked.key", new IOException("device is busy"));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasFile);
        Assert.False(viewModel.GenerateCommand.CanExecute(null));
        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Error, viewModel.Message.Severity);
        Assert.Contains("device is busy", viewModel.Message.Text, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SelectedChunkSize_Change_RecomputesTheEstimate()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("keys.kdbx", BackupFixtures.IncompressibleBytes(16384));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        string? atMedium = viewModel.EstimateText;

        viewModel.SelectedChunkSize = ChunkSizeOption.For(ChunkSizePreset.Small);

        Assert.NotNull(atMedium);
        Assert.NotNull(viewModel.EstimateText);
        Assert.NotEqual(atMedium, viewModel.EstimateText);
    }

    [Fact]
    public void SelectedChunkSize_RejectsNull()
    {
        (BackupViewModel viewModel, _, _) = Create();

        Assert.Throws<ArgumentNullException>(() => viewModel.SelectedChunkSize = null!);
    }

    [Fact]
    public async Task ASmallFile_RaisesNoLargeInputWarning()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("keys.kdbx", BackupFixtures.IncompressibleBytes(2048));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasLargeInputWarning);
        Assert.Null(viewModel.LargeInputWarning);
    }

    [Fact]
    public async Task ALargeFile_RaisesTheLargeInputWarning()
    {
        // Sized against the estimator itself rather than a guessed byte count, so the test states the
        // behaviour — "past the threshold, warn" — and not a number that a layout change would invalidate.
        int length = 512 * 1024;
        PageLayout layout = PageLayout.Estimate(length, EncodeOptions.FromPreset(ChunkSizePreset.Small));
        Assert.True(layout.PageCount >= BackupViewModel.LargeInputPageThreshold, "the fixture is not large enough");

        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("big.bin", BackupFixtures.IncompressibleBytes(length));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        viewModel.SelectedChunkSize = ChunkSizeOption.For(ChunkSizePreset.Small);

        Assert.True(viewModel.HasLargeInputWarning);
        Assert.Contains(
            layout.PageCount.ToString(CultureInfo.CurrentCulture),
            viewModel.LargeInputWarning!,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChooseOutput_SuggestsAPdfNameBuiltFromTheChosenFile()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        dialogs.PdfDestination = new FakeSaveTarget("secret.hardcopy.pdf");

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ChooseOutputCommand.ExecuteAsync(null);

        Assert.Equal("secret.hardcopy.pdf", dialogs.SuggestedPdfName);
        Assert.True(viewModel.HasOutput);
        Assert.Equal("/tmp/secret.hardcopy.pdf", viewModel.OutputLocation);
        Assert.True(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public async Task Generate_ComposesARealPdf_AndWritesIt()
    {
        byte[] content = BackupFixtures.IncompressibleBytes(1500);
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create(composer: new PdfComposer());
        FakeSaveTarget target = new("secret.hardcopy.pdf");
        dialogs.FileToBackUp = new FakePickedFile("secret.key", content);
        dialogs.PdfDestination = target;

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ChooseOutputCommand.ExecuteAsync(null);
        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.NotNull(target.Written);
        Assert.Equal("%PDF"u8.ToArray(), target.Written.AsSpan(0, 4).ToArray());
        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Success, viewModel.Message.Severity);
        Assert.Contains("secret.hardcopy.pdf", viewModel.Message.Text, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
        Assert.Null(viewModel.ProgressText);
    }

    [Fact]
    public async Task Generate_WhenTheWriteFails_ReportsItAsAWriteFailure()
    {
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create();
        dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        dialogs.PdfDestination = FakeSaveTarget.Failing("out.pdf", new UnauthorizedAccessException("read-only volume"));

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ChooseOutputCommand.ExecuteAsync(null);
        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Error, viewModel.Message.Severity);
        Assert.Contains("read-only volume", viewModel.Message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_WhenCompositionFails_ReportsItAndWritesNothing()
    {
        FakePdfComposer composer = new()
        {
            OnCompose = _ => throw new InvalidOperationException("the layout does not fit"),
        };
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create(composer: composer);
        FakeSaveTarget target = new("out.pdf");
        dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        dialogs.PdfDestination = target;

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ChooseOutputCommand.ExecuteAsync(null);
        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Null(target.Written);
        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Error, viewModel.Message.Severity);
        Assert.Contains("the layout does not fit", viewModel.Message.Text, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Generate_WhenCancelled_SaysSo_AndWritesNothing()
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
        (BackupViewModel viewModel, FakeFileDialogService dialogs, _) = Create(composer: composer);
        FakeSaveTarget target = new("out.pdf");
        dialogs.FileToBackUp = new FakePickedFile("secret.key", [1, 2, 3]);
        dialogs.PdfDestination = target;

        await viewModel.ChooseFileCommand.ExecuteAsync(null);
        await viewModel.ChooseOutputCommand.ExecuteAsync(null);

        Task generation = viewModel.GenerateCommand.ExecuteAsync(null);
        composing.Wait(TestContext.Current.CancellationToken);
        Assert.True(viewModel.IsBusy);
        Assert.True(viewModel.CancelCommand.CanExecute(null));
        viewModel.CancelCommand.Execute(null);
        await generation;

        Assert.Null(target.Written);
        Assert.NotNull(viewModel.Message);
        Assert.Equal(MessageSeverity.Information, viewModel.Message.Severity);
        Assert.Equal(Strings.BackupCancelled, viewModel.Message.Text);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        BackupEncoder encoder = new(new FixedBackupIdGenerator(BackupFixtures.BackupId), TimeProvider.System);
        FakePdfComposer composer = new();
        FakeFileDialogService dialogs = new();

        Assert.Throws<ArgumentNullException>(() => new BackupViewModel(null!, composer, dialogs, NullLogger<BackupViewModel>.Instance));
        Assert.Throws<ArgumentNullException>(() => new BackupViewModel(encoder, null!, dialogs, NullLogger<BackupViewModel>.Instance));
        Assert.Throws<ArgumentNullException>(() => new BackupViewModel(encoder, composer, null!, NullLogger<BackupViewModel>.Instance));
        Assert.Throws<ArgumentNullException>(() => new BackupViewModel(encoder, composer, dialogs, null!));
    }

    private static (BackupViewModel ViewModel, FakeFileDialogService Dialogs, IPdfComposer Composer) Create(
        IPdfComposer? composer = null)
    {
        FakeFileDialogService dialogs = new();
        IPdfComposer pdf = composer ?? new FakePdfComposer();
        BackupViewModel viewModel = new(
            new BackupEncoder(new FixedBackupIdGenerator(BackupFixtures.BackupId), TimeProvider.System),
            pdf,
            dialogs,
            NullLogger<BackupViewModel>.Instance);

        return (viewModel, dialogs, pdf);
    }
}
