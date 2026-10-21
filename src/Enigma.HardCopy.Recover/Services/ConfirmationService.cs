using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Recover.Resources;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The <see cref="IConfirmationService"/> backed by the control library's <c>ContentDialog</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neither dialog may ever carry code text.</b> A <c>ContentDialog</c> keeps its content until the next one is
/// shown, so anything put in it outlives the question; the codes are the secret being recovered. Both
/// confirmations carry prose and hashes only.
/// </para>
/// <para>
/// Only <see cref="DialogResult.Primary"/> counts as agreement: a dismissal — the back gesture or a tap on the
/// scrim — yields <see cref="DialogResult.None"/>, and leaves the guarded action undone.
/// </para>
/// </remarks>
internal sealed class ConfirmationService : IConfirmationService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>Initializes a new instance of the <see cref="ConfirmationService"/> class.</summary>
    /// <param name="dialogs">The library's dialog service, whose host the app registered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialogs"/> is <see langword="null"/>.</exception>
    public ConfirmationService(IContentDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        _dialogs = dialogs;
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmUnverifiedSaveAsync(string expectedSha256, string actualSha256)
    {
        Control content = BuildUnverifiedSaveContent(expectedSha256, actualSha256);

        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = Strings.ConfirmUnverifiedSaveTitle;
            dialog.IconData = AppIcons.ConfirmUnverifiedSave;
            dialog.IconBrush = FindBrush("EnigmaWarningBrush");
            dialog.Content = content;
            dialog.PrimaryButtonText = Strings.ConfirmUnverifiedSaveConfirm;
            dialog.CloseButtonText = Strings.ConfirmUnverifiedSaveCancel;
            dialog.DefaultButton = DefaultButton.Close;
        });

        return result == DialogResult.Primary;
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmNewSessionAsync()
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = Strings.ConfirmNewSessionTitle;
            dialog.IconData = AppIcons.ConfirmNewSession;
            dialog.IconBrush = FindBrush("EnigmaWarningBrush");
            dialog.Content = new TextBlock
            {
                Text = Strings.ConfirmNewSessionBody,
                TextWrapping = TextWrapping.Wrap,
            };
            dialog.PrimaryButtonText = Strings.ConfirmNewSessionConfirm;
            dialog.CloseButtonText = Strings.ConfirmNewSessionCancel;
            dialog.DefaultButton = DefaultButton.Close;
        });

        return result == DialogResult.Primary;
    }

    /// <summary>The prose, then the two hashes one under the other — which is the whole point of asking.</summary>
    /// <param name="expectedSha256">The hash recorded in the backup.</param>
    /// <param name="actualSha256">The hash the rebuilt bytes actually have.</param>
    /// <returns>The dialog's content.</returns>
    private static Control BuildUnverifiedSaveContent(string expectedSha256, string actualSha256)
    {
        StackPanel panel = new() { Spacing = 12 };

        panel.Children.Add(new TextBlock
        {
            Text = Strings.ConfirmUnverifiedSaveBody,
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(HashRow(Strings.ConfirmUnverifiedSaveExpectedLabel, expectedSha256));
        panel.Children.Add(HashRow(Strings.ConfirmUnverifiedSaveActualLabel, actualSha256));

        return panel;
    }

    /// <summary>One labelled hash, in the fixed pitch the page uses for one.</summary>
    /// <param name="label">What the hash is.</param>
    /// <param name="hash">The hash itself.</param>
    /// <returns>The row.</returns>
    private static Control HashRow(string label, string hash)
    {
        TextBlock caption = new() { Text = label };
        caption.Classes.Add("field");

        SelectableTextBlock value = new() { Text = hash, TextWrapping = TextWrapping.Wrap };
        value.Classes.Add("value");
        value.Classes.Add("mono");

        StackPanel row = new() { Spacing = 2 };
        row.Children.Add(caption);
        row.Children.Add(value);

        return row;
    }

    /// <summary>
    /// Resolves one of the theme's brushes by key. The brush instance is shared and follows the active variant on
    /// its own, so holding it across a light/dark switch is safe.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush, or <see langword="null"/> when the theme is not loaded — the icon then inherits.</returns>
    private static IBrush? FindBrush(string key)
        => Application.Current is { } application && application.TryFindResource(key, out object? value)
            ? value as IBrush
            : null;
}
