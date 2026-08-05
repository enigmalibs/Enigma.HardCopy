using System;
using System.Linq;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Hosting;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppConfirmations = Enigma.HardCopy.Desktop.Services.IConfirmationService;
using AppFileDialogs = Enigma.HardCopy.Desktop.Services.IFileDialogService;
using AppNotifications = Enigma.HardCopy.Desktop.Services.INotificationService;
using AppProgressOverlay = Enigma.HardCopy.Desktop.Services.IProgressOverlay;

namespace Enigma.HardCopy.Desktop.UnitTests.Hosting;

/// <summary>
/// The container wiring, asserted rather than discovered at run time: a missing registration is a blank window
/// with a line in the log, and a wrong <b>lifetime</b> is worse — a transient page ViewModel silently discards
/// the user's chosen file on every page switch, and a singleton view leaks its visual tree instead.
/// </summary>
/// <remarks>
/// <para>
/// The application's own <c>IFileDialogService</c> is reached through an alias: the control library declares an
/// interface of the same name, and a file that imports both namespaces without one does not compile
/// (<c>CS0104</c>). Bare names here are therefore the library's.
/// </para>
/// <para>
/// Registrations are checked as descriptors, and only the services that need no windowing platform are
/// actually resolved. <see cref="MainWindow"/>, the two views and <see cref="MainWindowViewModel"/> are
/// deliberately left unresolved: a <c>Window</c> needs a windowing platform, and the shell ViewModel's factory
/// resolves Phosphor icon geometry, which needs a render interface. Both are verified in the running app.
/// </para>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(typeof(INavigationService), typeof(NavigationService))]
    [InlineData(typeof(IContentDialogService), typeof(ContentDialogService))]
    [InlineData(typeof(IOverlayService), typeof(OverlayService))]
    [InlineData(typeof(IInfoBarService), typeof(InfoBarService))]
    [InlineData(typeof(IFileDialogService), typeof(FileDialogService))]
    [InlineData(typeof(IFolderDialogService), typeof(FolderDialogService))]
    public void AddEnigmaAvaloniaDesktop_RegistersTheLibrarysService_AsASingleton(Type service, Type implementation)
    {
        ServiceCollection services = [];

        services.AddEnigmaAvaloniaDesktop();

        ServiceDescriptor descriptor = DescriptorFor(services, service);
        Assert.Equal(implementation, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddEnigmaAvaloniaDesktop_RegistersNothingElse()
    {
        ServiceCollection services = [];

        services.AddEnigmaAvaloniaDesktop();

        Assert.Equal(6, services.Count);
    }

    [Fact]
    public void AddEnigmaAvaloniaDesktop_LeavesAnEarlierRegistrationAlone()
    {
        NavigationService mine = new();
        ServiceCollection services = [];
        services.AddSingleton<INavigationService>(mine);

        services.AddEnigmaAvaloniaDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(mine, provider.GetRequiredService<INavigationService>());
    }

    [Fact]
    public void AddEnigmaAvaloniaDesktop_RegistersServicesThatCanBeBuilt()
    {
        ServiceCollection services = [];
        services.AddEnigmaAvaloniaDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<INavigationService>());
        Assert.NotNull(provider.GetRequiredService<IContentDialogService>());
        Assert.NotNull(provider.GetRequiredService<IOverlayService>());
        Assert.NotNull(provider.GetRequiredService<IInfoBarService>());
        Assert.NotNull(provider.GetRequiredService<IFileDialogService>());
        Assert.NotNull(provider.GetRequiredService<IFolderDialogService>());
    }

    [Theory]
    [InlineData(typeof(TimeProvider), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBackupIdGenerator), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBackupEncoder), ServiceLifetime.Singleton)]
    [InlineData(typeof(IPdfComposer), ServiceLifetime.Singleton)]
    [InlineData(typeof(AppFileDialogs), ServiceLifetime.Singleton)]
    [InlineData(typeof(AppProgressOverlay), ServiceLifetime.Singleton)]
    [InlineData(typeof(AppNotifications), ServiceLifetime.Singleton)]
    [InlineData(typeof(AppConfirmations), ServiceLifetime.Singleton)]
    [InlineData(typeof(MainWindow), ServiceLifetime.Singleton)]
    [InlineData(typeof(MainWindowViewModel), ServiceLifetime.Singleton)]
    public void AddEnigmaHardCopyDesktop_RegistersTheShellsService_WithTheRightLifetime(Type service, ServiceLifetime lifetime)
    {
        ServiceCollection services = [];

        services.AddEnigmaHardCopyDesktop();

        Assert.Equal(lifetime, DescriptorFor(services, service).Lifetime);
    }

    /// <summary>
    /// The lifetime split that carries the page-state guarantee: the ViewModel outlives the navigation, the
    /// control does not.
    /// </summary>
    /// <param name="view">The page's view.</param>
    /// <param name="viewModel">The page's ViewModel.</param>
    [Theory]
    [InlineData(typeof(BackupView), typeof(BackupViewModel))]
    [InlineData(typeof(RecoverView), typeof(RecoverViewModel))]
    public void AddEnigmaHardCopyDesktop_RegistersPageViewsTransient_AndPageViewModelsSingleton(Type view, Type viewModel)
    {
        ServiceCollection services = [];

        services.AddEnigmaHardCopyDesktop();

        Assert.Equal(ServiceLifetime.Transient, DescriptorFor(services, view).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, DescriptorFor(services, viewModel).Lifetime);
    }

    [Fact]
    public void AddEnigmaHardCopyDesktop_RegistersTheApplicationsOwnPickerService_OverTheLibrarys()
    {
        ServiceCollection services = [];
        services.AddEnigmaAvaloniaDesktop();
        services.AddEnigmaHardCopyDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();

        AppFileDialogs dialogs = provider.GetRequiredService<AppFileDialogs>();
        Assert.NotSame(provider.GetRequiredService<IFileDialogService>(), dialogs);
    }

    [Fact]
    public void ThePagesAndTheirDependencies_AreResolvable()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddEnigmaAvaloniaDesktop();
        services.AddEnigmaHardCopyDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<TimeProvider>());
        Assert.NotNull(provider.GetRequiredService<IBackupIdGenerator>());
        Assert.NotNull(provider.GetRequiredService<IBackupEncoder>());
        Assert.NotNull(provider.GetRequiredService<IPdfComposer>());
        Assert.NotNull(provider.GetRequiredService<AppFileDialogs>());
        Assert.NotNull(provider.GetRequiredService<BackupViewModel>());
        Assert.NotNull(provider.GetRequiredService<RecoverViewModel>());
    }

    /// <summary>
    /// The three shell seams are required constructor dependencies of both pages, so a missing one is a
    /// startup failure rather than a page that quietly stops reporting anything.
    /// </summary>
    [Fact]
    public void TheShellSeams_AreResolvable_OverTheLibrarysHostBackedServices()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddEnigmaAvaloniaDesktop();
        services.AddEnigmaHardCopyDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<AppProgressOverlay>());
        Assert.NotNull(provider.GetRequiredService<AppNotifications>());
        Assert.NotNull(provider.GetRequiredService<AppConfirmations>());
    }

    [Fact]
    public void ThePageViewModels_AreTheSameInstanceEveryTime()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddEnigmaAvaloniaDesktop();
        services.AddEnigmaHardCopyDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<BackupViewModel>(), provider.GetRequiredService<BackupViewModel>());
        Assert.Same(provider.GetRequiredService<RecoverViewModel>(), provider.GetRequiredService<RecoverViewModel>());
    }

    private static ServiceDescriptor DescriptorFor(IServiceCollection services, Type service)
        => Assert.Single(services, descriptor => descriptor.ServiceType == service);
}
