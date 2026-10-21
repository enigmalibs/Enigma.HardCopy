using Enigma.Avalonia.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Enigma.HardCopy.Recover.Hosting;

/// <summary>
/// Registers the <c>Enigma.Avalonia.Desktop</c> control library's services this application uses.
/// </summary>
/// <remarks>
/// <para>
/// Three of the library's six: the content dialog and the info bar, each driving a host placed in the main view,
/// and the file picker, which saves the recovered file. A single-screen phone app has no navigation rail, no
/// long run to put behind an overlay, and never picks a folder, so the other three are not registered.
/// </para>
/// <para>
/// All three are singletons: two own a host control and one owns the top level's storage provider, and none of
/// that survives being rebuilt per resolution. This lives in its own file, apart from the application's own
/// registrations, so that every name below is unambiguously the library's.
/// </para>
/// </remarks>
public static class EnigmaAvaloniaServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the library's dialog, info bar and file picker services, unless the caller has already
        /// registered its own.
        /// </summary>
        public void AddEnigmaAvaloniaDesktop()
        {
            services.TryAddSingleton<IContentDialogService, ContentDialogService>();
            services.TryAddSingleton<IInfoBarService, InfoBarService>();
            services.TryAddSingleton<IFileDialogService, FileDialogService>();
        }
    }
}
