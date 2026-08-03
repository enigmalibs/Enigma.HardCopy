using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Title_DefaultsToTheApplicationName()
        => Assert.Equal("Enigma.HardCopy", new MainWindowViewModel().Title);

    [Fact]
    public void Title_RaisesPropertyChanged()
    {
        MainWindowViewModel viewModel = new();
        string? changed = null;
        viewModel.PropertyChanged += (_, e) => changed = e.PropertyName;

        viewModel.Title = "Recover";

        Assert.Equal(nameof(MainWindowViewModel.Title), changed);
    }
}
