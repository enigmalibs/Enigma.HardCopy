using System;
using Avalonia.Controls;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.Navigation;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// The shell's navigation rail: two items, in order, each declaring the view and the ViewModel of its page,
/// settings pinned to the footer — and a constructor that navigates nowhere, which is both a design rule and
/// what keeps the rail assertable with no windowing platform behind it.
/// </summary>
/// <remarks>
/// The icons are passed in as sentinels rather than resolved from the Phosphor pack: a real icon
/// <see cref="Geometry"/> is a <c>StreamGeometry</c>, whose construction needs Avalonia's platform render
/// interface, and this suite deliberately stands up no platform. A <see cref="PathGeometry"/> needs none, so
/// each item can still be checked against the exact instance it was given — which proves the wiring, not just
/// that something was set.
/// </remarks>
public sealed class MainWindowViewModelTests
{
    private static readonly Geometry BackupIcon = new PathGeometry();
    private static readonly Geometry RecoverIcon = new PathGeometry();
    private static readonly Geometry SettingsIcon = new PathGeometry();

    [Fact]
    public void Title_IsTheApplicationName() => Assert.Equal(Strings.AppTitle, Create().Title);

    [Fact]
    public void Navigation_IsTheServiceItWasGiven()
    {
        NavigationService navigation = new();

        MainWindowViewModel viewModel = Create(navigation);

        Assert.Same(navigation, viewModel.Navigation);
    }

    [Fact]
    public void Items_AreBackupThenRecover_WithTheirViewsAndViewModels()
    {
        MainWindowViewModel viewModel = Create();

        Assert.Collection(
            viewModel.Navigation.Items,
            item =>
            {
                Assert.Equal(Strings.NavBackup, item.Header);
                Assert.Equal(typeof(BackupView), item.PageType);
                Assert.Equal(typeof(BackupViewModel), item.PageViewModelType);
                Assert.Same(BackupIcon, item.IconData);
            },
            item =>
            {
                Assert.Equal(Strings.NavRecover, item.Header);
                Assert.Equal(typeof(RecoverView), item.PageType);
                Assert.Equal(typeof(RecoverViewModel), item.PageViewModelType);
                Assert.Same(RecoverIcon, item.IconData);
            });
    }

    /// <summary>
    /// Settings is pinned to the footer rather than added to the list: it is not one of the two things this
    /// application is for, and which collection it lands in is the whole of that statement.
    /// </summary>
    [Fact]
    public void FooterItems_AreSettingsAlone_WithItsViewAndViewModel()
    {
        MainWindowViewModel viewModel = Create();

        NavigationItem item = Assert.Single(viewModel.Navigation.FooterItems);
        Assert.Equal(Strings.NavSettings, item.Header);
        Assert.Equal(typeof(SettingsView), item.PageType);
        Assert.Equal(typeof(SettingsViewModel), item.PageViewModelType);
        Assert.Same(SettingsIcon, item.IconData);
    }

    [Fact]
    public void Constructor_NavigatesNowhere()
    {
        MainWindowViewModel viewModel = Create();

        Assert.Null(viewModel.Navigation.SelectedItem);
        Assert.Null(viewModel.Navigation.CurrentPage);
    }

    [Fact]
    public void PageFactory_ResolvesThePageAndItsViewModelFromTheContainer()
    {
        ServiceProvider services = new ServiceCollection()
            .AddTransient<StubPage>()
            .AddSingleton<StubPageViewModel>()
            .BuildServiceProvider();

        MainWindowViewModel viewModel = Create(services: services);
        NavigationItem item = new()
        {
            PageType = typeof(StubPage),
            PageViewModelType = typeof(StubPageViewModel),
        };

        Control page = viewModel.Navigation.PageFactory(item);

        Assert.IsType<StubPage>(page);
        Assert.Same(services.GetRequiredService<StubPageViewModel>(), page.DataContext);
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(null!, new NavigationService(), BackupIcon, RecoverIcon, SettingsIcon));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(services, null!, BackupIcon, RecoverIcon, SettingsIcon));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(services, new NavigationService(), null!, RecoverIcon, SettingsIcon));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(services, new NavigationService(), BackupIcon, null!, SettingsIcon));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(services, new NavigationService(), BackupIcon, RecoverIcon, null!));
    }

    private static MainWindowViewModel Create(
        INavigationService? navigation = null,
        IServiceProvider? services = null)
        => new(
            services ?? new ServiceCollection().BuildServiceProvider(),
            navigation ?? new NavigationService(),
            BackupIcon,
            RecoverIcon,
            SettingsIcon);

    private sealed class StubPage : UserControl;

    private sealed class StubPageViewModel;
}
