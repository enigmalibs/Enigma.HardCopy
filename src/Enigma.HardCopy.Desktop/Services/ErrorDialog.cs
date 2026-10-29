using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Enigma.HardCopy.Desktop.Views;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IErrorDialog"/> backed by the control library's <c>ContentDialog</c>: the error in the window's
/// dialog host.
/// </summary>
/// <remarks>
/// <para>
/// The host is the one every question shares, so the dialog never replaces a question on screen: while one is
/// open it waits for that question to be answered. Before the window hands its host over
/// (<see cref="RegisterHost"/>), it waits for that.
/// </para>
/// <para>
/// <b>The content is dropped when the dialog closes.</b> A <c>ContentDialog</c> keeps its content until the next
/// one is shown, and the details are an exception's own words: nothing about a backup may outlive the dialog that
/// showed it, which is the rule <see cref="ConfirmationService"/> follows for the codes.
/// </para>
/// </remarks>
internal sealed class ErrorDialog : IErrorDialog
{
    private readonly IContentDialogService _dialogs;

    private ContentDialog? _host;

    // The host whose question the next dialog waits behind, while it waits.
    private ContentDialog? _waitingFor;

    /// <summary>Initializes a new instance of the <see cref="ErrorDialog"/> class.</summary>
    /// <param name="dialogs">The library's dialog service, whose host the shell registers at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialogs"/> is <see langword="null"/>.</exception>
    public ErrorDialog(IContentDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        _dialogs = dialogs;
    }

    /// <inheritdoc/>
    public event EventHandler? Available;

    /// <summary>
    /// Takes the window's dialog host — the one <see cref="IContentDialogService"/> shows on — so the dialog can
    /// wait for a question on it, and lets what waited for it be shown.
    /// </summary>
    /// <param name="host">The window's dialog host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="host"/> is <see langword="null"/>.</exception>
    public void RegisterHost(ContentDialog host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _host = host;

        Dispatcher.UIThread.Post(RaiseAvailable, DispatcherPriority.Background);
    }

    /// <inheritdoc/>
    public bool IsAvailable()
    {
        if (_host is not { } host)
        {
            // RegisterHost raises Available.
            return false;
        }

        if (!host.IsOpen)
        {
            return true;
        }

        if (!ReferenceEquals(_waitingFor, host))
        {
            _waitingFor = host;
            host.Closed += OnWaitedClosed;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task ShowAsync(UnhandledErrorBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        ContentDialog host = _host
            ?? throw new InvalidOperationException("The error dialog was shown before the window handed its host over.");

        UnhandledErrorView view = new()
        {
            DataContext = new UnhandledErrorViewModel(batch, text => CopyAsync(host, text)),
        };

        try
        {
            await _dialogs.ShowAsync(dialog =>
            {
                dialog.Title = Strings.UnhandledErrorTitle;
                dialog.IconData = AppIcons.UnhandledError;
                dialog.IconBrush = FindBrush("EnigmaWarningBrush");
                dialog.Content = view;
                dialog.CloseButtonText = Strings.UnhandledErrorClose;
                dialog.DefaultButton = DefaultButton.Close;
            });
        }
        finally
        {
            // Only while it is still this dialog's: a question asked straight after it has its own content.
            if (ReferenceEquals(host.Content, view))
            {
                host.Content = null;
            }
        }
    }

    private void OnWaitedClosed(object? sender, DialogResult e)
    {
        if (sender is ContentDialog host)
        {
            host.Closed -= OnWaitedClosed;
        }

        _waitingFor = null;

        // Posted: the question's own continuation runs first, and may ask the next question at once.
        Dispatcher.UIThread.Post(RaiseAvailable, DispatcherPriority.Background);
    }

    private void RaiseAvailable() => Available?.Invoke(this, EventArgs.Empty);

    private static async Task CopyAsync(ContentDialog host, string text)
    {
        if (TopLevel.GetTopLevel(host)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    /// <summary>
    /// Resolves one of the theme's brushes by key. The brush instance is shared and its colour follows the active
    /// variant on its own.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush, or <see langword="null"/> when the theme is not loaded — the icon then inherits.</returns>
    private static IBrush? FindBrush(string key)
        => Application.Current is { } application && application.TryFindResource(key, out object? value)
            ? value as IBrush
            : null;
}
