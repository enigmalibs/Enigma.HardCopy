using Enigma.HardCopy.Recover.Views;
using Microsoft.Extensions.DependencyInjection;

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
        /// <summary>Registers the main view.</summary>
        /// <remarks>
        /// <b>The view is transient:</b> the Android lifetime builds a new view for every recreated activity, and a
        /// fresh control each time is what keeps the old visual tree from leaking.
        /// </remarks>
        public void AddEnigmaHardCopyRecover()
        {
            services.AddTransient<MainView>();
        }
    }
}
