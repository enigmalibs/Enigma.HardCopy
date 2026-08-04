using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop;

/// <summary>
/// The Avalonia application. Owns the <see cref="IHost"/> that provides configuration, logging and
/// dependency injection to the whole app.
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

        ConfigureServices(builder.Services);

        _host = builder.Build();
        _host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The window is resolved rather than constructed: the file-dialog service needs this very window to
            // own its dialogs, so the container has to be the one place that knows which window that is.
            MainWindow window = _host.Services.GetRequiredService<MainWindow>();
            window.DataContext = _host.Services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = window;
            desktop.Exit += (_, _) => _host.StopAsync().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Registers everything the application resolves. Core's services are singletons because they are
    /// stateless, and both pages are singletons because each holds work in progress — a file chosen for backup,
    /// a recovery half fed with codes — that must survive moving between tabs.
    /// </summary>
    /// <param name="services">The collection to register into.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Core.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBackupIdGenerator, RandomBackupIdGenerator>();
        services.AddSingleton<IBackupEncoder, BackupEncoder>();
        services.AddSingleton<IPdfComposer, PdfComposer>();

        // Shell and platform services. The dialog service is built by hand because its constructor is
        // internal, and the container's activator only considers public ones.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IFileDialogService>(provider =>
            new StorageProviderFileDialogService(provider.GetRequiredService<MainWindow>()));

        // Pages and shell.
        services.AddSingleton<BackupViewModel>();
        services.AddSingleton<RecoverViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }
}
