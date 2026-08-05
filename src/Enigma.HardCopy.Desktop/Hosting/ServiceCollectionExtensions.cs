using System;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using LibraryFileDialogs = Enigma.Avalonia.Desktop.Services.IFileDialogService;
using LibraryNavigation = Enigma.Avalonia.Desktop.Services.INavigationService;

namespace Enigma.HardCopy.Desktop.Hosting;

/// <summary>
/// Registers everything the application itself resolves.
/// </summary>
/// <remarks>
/// It is an extension rather than a private method on <see cref="App"/> so that a test can assert the wiring
/// without standing up Avalonia: the lifetimes here are load-bearing, and getting one wrong is invisible until
/// a user loses work. The library's own services are registered separately by
/// <see cref="EnigmaAvaloniaServiceCollectionExtensions.AddEnigmaAvaloniaDesktop"/> — see that file for why the
/// two cannot share one.
/// </remarks>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Core's services, the shell, the two pages and the platform services behind them.
        /// </summary>
        /// <remarks>
        /// Core's services are singletons because they are stateless. <b>Page ViewModels are singletons and
        /// their views transient:</b> each ViewModel holds work in progress — a file chosen for backup, a
        /// recovery half fed with codes — that must survive leaving the page and coming back, while a fresh
        /// control per navigation is what keeps the visual tree from leaking. Swapping either lifetime looks
        /// like a working app and silently breaks one of those two things.
        /// </remarks>
        public void AddEnigmaHardCopyDesktop()
        {
            // Core.
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IBackupIdGenerator, RandomBackupIdGenerator>();
            services.AddSingleton<IBackupEncoder, BackupEncoder>();
            services.AddSingleton<IPdfComposer, PdfComposer>();

            // Shell and platform services. The dialog service is built by hand because its constructor is
            // internal, and the container's activator only considers public ones. It adapts the library's
            // picker service into the IPickedFile / ISaveTarget shapes the ViewModels are tested against.
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IFileDialogService>(provider =>
                new StorageProviderFileDialogService(provider.GetRequiredService<LibraryFileDialogs>()));

            // Pages: the view fresh each time, the ViewModel kept.
            services.AddTransient<BackupView>();
            services.AddTransient<RecoverView>();
            services.AddSingleton<BackupViewModel>();
            services.AddSingleton<RecoverViewModel>();

            // The shell's rail icons are passed in rather than resolved by the container: turning a Phosphor
            // glyph into a Geometry needs Avalonia's platform render interface, so resolving them from a type
            // would tie this registration to a live rendering platform. This factory runs at first resolution,
            // by which time the app has one.
            services.AddSingleton(provider => new MainWindowViewModel(
                provider,
                provider.GetRequiredService<LibraryNavigation>(),
                AppIcons.Backup,
                AppIcons.Recover));
        }
    }
}
