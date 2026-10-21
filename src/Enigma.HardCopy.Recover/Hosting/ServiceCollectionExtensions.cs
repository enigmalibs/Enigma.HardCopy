using Enigma.HardCopy.Recover.Services;
using Enigma.HardCopy.Recover.ViewModels;
using Enigma.HardCopy.Recover.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Enigma.HardCopy.Recover.Hosting;

/// <summary>
/// Registers everything the application itself resolves.
/// </summary>
/// <remarks>
/// An extension rather than a private method on <see cref="App"/>, so that a test can assert the wiring without
/// standing up Avalonia. The library's services are registered separately, by
/// <see cref="EnigmaAvaloniaServiceCollectionExtensions.AddEnigmaAvaloniaDesktop"/>.
/// </remarks>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the main view, its ViewModel and the services behind them.</summary>
        /// <remarks>
        /// <para>
        /// <b>The two platform seams are defaults.</b> The camera and the haptics are registered with
        /// <c>TryAdd</c>, after the platform head has had its turn (see <see cref="App"/>), so a head's own
        /// implementation wins and a platform without one still gets a working, honest application.
        /// </para>
        /// <para>
        /// <b>The ViewModel is a singleton and the view transient:</b> the session must survive the Android
        /// lifetime building a new view for a recreated activity, while a fresh control each time is what keeps the
        /// old visual tree from leaking.
        /// </para>
        /// </remarks>
        public void AddEnigmaHardCopyRecover()
        {
            services.TryAddSingleton<ICameraScanner, UnavailableCameraScanner>();
            services.TryAddSingleton<IHapticFeedback, NoHapticFeedback>();

            services.AddSingleton<IRecoveredFileSaver, StorageProviderRecoveredFileSaver>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IConfirmationService, ConfirmationService>();

            services.AddSingleton<ScanSessionViewModel>();
            services.AddTransient<MainView>();
        }
    }
}
