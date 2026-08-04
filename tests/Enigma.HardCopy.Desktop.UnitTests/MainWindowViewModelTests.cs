using System;
using System.Collections.Generic;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Title_IsTheApplicationName() => Assert.Equal(Strings.AppTitle, Create().Title);

    [Fact]
    public void Pages_AreBackupThenRecover()
    {
        MainWindowViewModel viewModel = Create();

        Assert.Collection(
            viewModel.Pages,
            page => Assert.Same(viewModel.Backup, page),
            page => Assert.Same(viewModel.Recover, page));
    }

    [Fact]
    public void CurrentPage_StartsOnBackup()
    {
        MainWindowViewModel viewModel = Create();

        Assert.Equal(0, viewModel.SelectedPageIndex);
        Assert.Same(viewModel.Backup, viewModel.CurrentPage);
    }

    [Fact]
    public void SelectedPageIndex_MovesCurrentPage_AndNotifiesBoth()
    {
        MainWindowViewModel viewModel = Create();
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SelectedPageIndex = 1;

        Assert.Same(viewModel.Recover, viewModel.CurrentPage);
        Assert.Contains(nameof(MainWindowViewModel.SelectedPageIndex), changed);
        Assert.Contains(nameof(MainWindowViewModel.CurrentPage), changed);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(7, 1)]
    public void SelectedPageIndex_OutsideTheRange_IsClamped(int requested, int expected)
    {
        MainWindowViewModel viewModel = Create();

        viewModel.SelectedPageIndex = requested;

        Assert.Equal(expected, viewModel.SelectedPageIndex);
        Assert.Same(viewModel.Pages[expected], viewModel.CurrentPage);
    }

    [Fact]
    public void PageTitles_ComeFromTheResources()
    {
        MainWindowViewModel viewModel = Create();

        Assert.Equal(Strings.NavBackup, viewModel.Backup.Title);
        Assert.Equal(Strings.NavRecover, viewModel.Recover.Title);
    }

    [Fact]
    public void Constructor_RejectsMissingPages()
    {
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(null!, CreateRecover()));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(CreateBackup(), null!));
    }

    private static MainWindowViewModel Create() => new(CreateBackup(), CreateRecover());

    private static BackupViewModel CreateBackup() => new(
        new BackupEncoder(new FixedBackupIdGenerator(BackupFixtures.BackupId), TimeProvider.System),
        new PdfComposer(),
        new FakeFileDialogService(),
        NullLogger<BackupViewModel>.Instance);

    private static RecoverViewModel CreateRecover()
        => new(new FakeFileDialogService(), NullLogger<RecoverViewModel>.Instance);
}
