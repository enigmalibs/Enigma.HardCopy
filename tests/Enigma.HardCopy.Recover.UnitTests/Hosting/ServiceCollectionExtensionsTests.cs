using System;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Recover.Hosting;
using Enigma.HardCopy.Recover.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enigma.HardCopy.Recover.UnitTests.Hosting;

/// <summary>
/// The container wiring, asserted rather than discovered on a phone: a missing registration is a crash at the
/// first tap, and a wrong lifetime is worse — a singleton view would be handed to a recreated activity while
/// still attached to the old one.
/// </summary>
/// <remarks>
/// Registrations are checked as descriptors. The view is deliberately left unresolved: a control needs a
/// rendering platform, which the running app proves.
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(typeof(IContentDialogService), typeof(ContentDialogService))]
    [InlineData(typeof(IInfoBarService), typeof(InfoBarService))]
    [InlineData(typeof(IFileDialogService), typeof(FileDialogService))]
    public void AddEnigmaAvaloniaDesktop_RegistersTheLibrarysService_AsASingleton(Type service, Type implementation)
    {
        ServiceCollection services = [];

        services.AddEnigmaAvaloniaDesktop();

        ServiceDescriptor descriptor = DescriptorFor(services, service);
        Assert.Equal(implementation, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // A phone app with one screen has no rail to navigate, no run to put behind an overlay and no folder to pick:
    // registering those services would only invite a call whose host was never placed.
    [Fact]
    public void AddEnigmaAvaloniaDesktop_RegistersOnlyWhatThisAppUses()
    {
        ServiceCollection services = [];

        services.AddEnigmaAvaloniaDesktop();

        Assert.Equal(3, services.Count);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(INavigationService));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOverlayService));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IFolderDialogService));
    }

    [Fact]
    public void AddEnigmaAvaloniaDesktop_LeavesAnEarlierRegistrationAlone()
    {
        InfoBarService mine = new();
        ServiceCollection services = [];
        services.AddSingleton<IInfoBarService>(mine);

        services.AddEnigmaAvaloniaDesktop();

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(mine, provider.GetRequiredService<IInfoBarService>());
    }

    [Fact]
    public void AddEnigmaHardCopyRecover_RegistersTheMainView_AsTransient()
    {
        ServiceCollection services = [];

        services.AddEnigmaHardCopyRecover();

        Assert.Equal(ServiceLifetime.Transient, DescriptorFor(services, typeof(MainView)).Lifetime);
    }

    private static ServiceDescriptor DescriptorFor(ServiceCollection services, Type service)
        => Assert.Single(services, descriptor => descriptor.ServiceType == service);
}
