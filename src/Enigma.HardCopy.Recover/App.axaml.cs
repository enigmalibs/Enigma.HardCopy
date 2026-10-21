using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Recover.Hosting;
using Enigma.HardCopy.Recover.Services;
using Enigma.HardCopy.Recover.ViewModels;
using Enigma.HardCopy.Recover.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Recover;

/// <summary>
/// The recovery application. Owns the container, and performs the one-time wiring the control library's
/// services need each time a main view is created.
/// </summary>
/// <remarks>
/// <para>
/// A platform head contributes its own services — the camera, haptics — through the registration callback it
/// passes to the constructor. That keeps this project free of any platform type while the composition root
/// stays here, in one place.
/// </para>
/// <para>
/// The host is built but never started: there are no hosted services to run, and Android ends an application
/// by killing its process rather than by asking it to shut down.
/// </para>
/// </remarks>
public partial class App : Application
{
    private readonly Action<IServiceCollection> _addPlatformServices;
    private IServiceProvider? _services;

    /// <summary>Initializes a new instance of the <see cref="App"/> class with no platform services.</summary>
    /// <remarks>What the XAML previewer uses; a platform head always passes its own registrations.</remarks>
    public App()
        : this(static _ => { })
    {
    }

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="addPlatformServices">Registers the services only the platform head can provide.</param>
    /// <exception cref="ArgumentNullException"><paramref name="addPlatformServices"/> is <see langword="null"/>.</exception>
    public App(Action<IServiceCollection> addPlatformServices)
    {
        ArgumentNullException.ThrowIfNull(addPlatformServices);

        _addPlatformServices = addPlatformServices;
    }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // The empty builder: no configuration files, no environment variables, no console — none of which
        // means anything inside an Android process. The Debug provider writes only while a debugger is attached,
        // so a user's phone keeps no log of what this application did.
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "Enigma.HardCopy.Recover",
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Logging.AddDebug();

        // The platform head first: the application's own platform seams are registered with TryAdd, as defaults
        // that a head's implementation pre-empts.
        _addPlatformServices(builder.Services);

        builder.Services.AddEnigmaAvaloniaDesktop();
        builder.Services.AddEnigmaHardCopyRecover();

        _services = builder.Build().Services;

        switch (ApplicationLifetime)
        {
            // Android: the factory runs for every activity (re)creation, so each new view takes the hosts over.
            case IActivityApplicationLifetime activity:
                activity.MainViewFactory = CreateMainView;
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = CreateMainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds a main view and hands its hosts — and, once it is on screen, its storage provider — to the
    /// library's services.
    /// </summary>
    /// <returns>The view.</returns>
    /// <remarks>
    /// <c>RegisterHost</c> and <c>SetStorageProvider</c> simply replace whatever they held, so a view built for a
    /// recreated activity takes over from the one before it. The storage provider belongs to the top level the
    /// view is attached to, which does not exist until it is.
    /// </remarks>
    private Control CreateMainView()
    {
        IServiceProvider services = _services
            ?? throw new InvalidOperationException("The main view was requested before the container was built.");

        MainView view = services.GetRequiredService<MainView>();
        view.DataContext = services.GetRequiredService<ScanSessionViewModel>();

        // The preview draws the camera's frames. Wired here rather than through the ViewModel, which has no business
        // handing a camera to a control: it only ever sees the codes.
        view.Preview.Source = services.GetRequiredService<ICameraScanner>();

        services.GetRequiredService<IContentDialogService>().RegisterHost(view.HostDialog);
        services.GetRequiredService<IInfoBarService>().RegisterHost(view.HostInfoBar);

        IFileDialogService fileDialogs = services.GetRequiredService<IFileDialogService>();
        view.AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(view)?.StorageProvider is { } storage)
            {
                fileDialogs.SetStorageProvider(storage);
            }
        };

        return view;
    }
}
