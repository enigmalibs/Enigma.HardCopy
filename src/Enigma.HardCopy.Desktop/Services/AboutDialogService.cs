using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IAboutDialogService"/> backed by the control library's <c>ContentDialog</c>: the About view in
/// the window's dialog host.
/// </summary>
/// <remarks>
/// On <see cref="IContentDialogService"/>, the host every question is asked on: About is a dialog of the same
/// kind — shown, read, closed.
/// </remarks>
internal sealed class AboutDialogService : IAboutDialogService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>Initializes a new instance of the <see cref="AboutDialogService"/> class.</summary>
    /// <param name="dialogs">The library's dialog service, whose host the shell registered at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialogs"/> is <see langword="null"/>.</exception>
    public AboutDialogService(IContentDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        _dialogs = dialogs;
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        AboutView view = new()
        {
            DataContext = new AboutViewModel(),
        };

        // The service resets the host before configuring it — buttons, commands, icon and its brush — so only
        // what this dialog has is set.
        await _dialogs.ShowAsync(dialog =>
        {
            // No title and no icon: the card draws them as a column beside the content, and the view already
            // shows the application's own icon and name above everything else.
            dialog.Title = null;
            dialog.Content = view;

            // One button, and it only dismisses: the dialog asks nothing.
            dialog.CloseButtonText = Strings.AboutClose;
            dialog.DefaultButton = DefaultButton.Close;
        });
    }
}
