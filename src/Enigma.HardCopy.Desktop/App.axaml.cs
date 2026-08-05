using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Hosting;
using Enigma.HardCopy.Desktop.Settings;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AppTheming = Enigma.HardCopy.Desktop.Services.IThemeService;

namespace Enigma.HardCopy.Desktop;

/// <summary>
/// The Avalonia application. Owns the <see cref="IHost"/> that provides configuration, logging and
/// dependency injection to the whole app, and performs the one-time wiring the control library's services
/// need before any of them is used.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Console + Debug sinks only — this app writes no log files (validated decision).
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddEnigmaAvaloniaDesktop();
        builder.Services.AddEnigmaHardCopyDesktop();

        _host = builder.Build();
        _host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IServiceProvider services = _host.Services;

            // The window is resolved rather than constructed: the library's services need this very window's
            // hosts and its storage provider, so the container has to be the one place that knows which
            // window that is.
            MainWindow window = services.GetRequiredService<MainWindow>();
            window.DataContext = services.GetRequiredService<MainWindowViewModel>();

            // All five run before the window is shown. Each of these services throws
            // InvalidOperationException from its first call if its host — or its storage provider — was never
            // handed over, and the first call is a user clicking a button.
            //
            // IFileDialogService below is the LIBRARY's: this file imports Enigma.Avalonia.Desktop.Services and
            // not the application's own Services namespace, which declares an interface of the same name.
            // The application's own picker service wraps this one and is never touched here.
            services.GetRequiredService<IOverlayService>().RegisterHost(window.HostOverlay);
            services.GetRequiredService<IContentDialogService>().RegisterHost(window.HostDialog);
            services.GetRequiredService<IInfoBarService>().RegisterHost(window.HostInfoBar);
            services.GetRequiredService<IFileDialogService>().SetStorageProvider(window.StorageProvider);
            services.GetRequiredService<IFolderDialogService>().SetStorageProvider(window.StorageProvider);

            ApplyStoredTheme(services);
            StartNavigation(services);

            desktop.MainWindow = window;
            desktop.Exit += (_, _) => _host.StopAsync().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Paints the application in the variant the user last chose.
    /// </summary>
    /// <param name="services">The container the shell was built from.</param>
    /// <remarks>
    /// Before the window is shown, so the first frame is already in the right colours rather than flashing
    /// the operating system's variant and then correcting itself. <c>App.axaml</c> declares
    /// <c>RequestedThemeVariant="Default"</c>, which is what the application looks like until this runs — and
    /// what it goes on looking like when the preference is <see cref="AppTheme.System"/> or there is no
    /// readable preference file at all.
    /// </remarks>
    private static void ApplyStoredTheme(IServiceProvider services)
    {
        AppSettings settings = services.GetRequiredService<IAppSettingsStore>().Load();

        services.GetRequiredService<AppTheming>().Apply(settings.Theme);
    }

    /// <summary>
    /// Subscribes to navigation failures and shows the first page.
    /// </summary>
    /// <param name="services">The container the shell was built from.</param>
    /// <remarks>
    /// <para>
    /// Nothing on <see cref="INavigationService"/> throws: a page that cannot be built is reported on
    /// <see cref="INavigationService.NavigationFailed"/> and leaves the content area empty. Without this
    /// subscription a wiring mistake would show as a blank window and no error anywhere, so the handler is
    /// registered <i>before</i> the first navigation is asked for.
    /// </para>
    /// <para>
    /// The first selection is made here, and not in <see cref="MainWindowViewModel"/>'s constructor, so that
    /// the shell ViewModel stays constructible — and its rail assertable — with no windowing platform behind
    /// it. It is also the only navigation requested at startup: the service serializes navigation with a
    /// zero-timeout semaphore, so a second one fired straight after this would be dropped, not queued.
    /// </para>
    /// </remarks>
    private static void StartNavigation(IServiceProvider services)
    {
        INavigationService navigation = services.GetRequiredService<INavigationService>();
        ILogger<App> logger = services.GetRequiredService<ILogger<App>>();

        navigation.NavigationFailed += (_, e) =>
            logger.LogError(e.Exception, "Navigation failed in phase {Phase}.", e.Phase);

        navigation.SelectedItem = navigation.Items[0];
    }
}
