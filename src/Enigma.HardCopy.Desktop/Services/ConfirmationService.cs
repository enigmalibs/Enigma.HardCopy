using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IConfirmationService"/> backed by the control library's <c>ContentDialog</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neither dialog may ever carry code text.</b> A <c>ContentDialog</c> keeps its content until the next one
/// is shown, so anything put in it outlives the question; the codes are the secret this application exists to
/// protect. Both confirmations therefore carry prose and hashes only.
/// </para>
/// <para>
/// Only <see cref="DialogResult.Primary"/> counts as agreement. Dismissing with <c>Escape</c> or a click on
/// the scrim yields <see cref="DialogResult.None"/> — not <see cref="DialogResult.Close"/> — so anything other
/// than an explicit press of the primary button leaves the guarded action undone.
/// </para>
/// </remarks>
internal sealed class ConfirmationService : IConfirmationService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>Initializes a new instance of the <see cref="ConfirmationService"/> class.</summary>
    /// <param name="dialogs">The library's dialog service, whose host the shell registered at startup.</param>
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

            // Declares intent only — the control has no Enter handling — but it is the right intent: the
            // safe answer is the one a hurried user should land on.
            dialog.DefaultButton = DefaultButton.Close;
        });

        return result == DialogResult.Primary;
    }

    /// <inheritdoc/>
    public async Task<bool> ConfirmStartOverAsync()
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = Strings.ConfirmStartOverTitle;
            dialog.IconData = AppIcons.ConfirmStartOver;
            dialog.IconBrush = FindBrush("EnigmaWarningBrush");
            dialog.Content = new TextBlock
            {
                Text = Strings.ConfirmStartOverBody,
                TextWrapping = TextWrapping.Wrap,
            };
            dialog.PrimaryButtonText = Strings.ConfirmStartOverConfirm;
            dialog.CloseButtonText = Strings.ConfirmStartOverCancel;
            dialog.DefaultButton = DefaultButton.Close;
        });

        return result == DialogResult.Primary;
    }

    /// <summary>The prose, then the two hashes side by side — which is the whole point of asking.</summary>
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

    /// <summary>One labelled hash, set in the same fixed pitch the pages use for one.</summary>
    /// <param name="label">What the hash is.</param>
    /// <param name="hash">The hash itself.</param>
    /// <returns>The row.</returns>
    private static Control HashRow(string label, string hash)
    {
        TextBlock caption = new() { Text = label };
        caption.Classes.Add("field");

        // Selectable, so a user can copy it into a terminal and check for themselves.
        SelectableTextBlock value = new() { Text = hash, TextWrapping = TextWrapping.Wrap };
        value.Classes.Add("value");
        value.Classes.Add("mono");

        StackPanel row = new() { Spacing = 2 };
        row.Children.Add(caption);
        row.Children.Add(value);

        return row;
    }

    /// <summary>
    /// Resolves one of the theme's brushes by key. The brush instance is shared and its colour follows the
    /// active variant on its own, so holding the result across a variant switch is safe.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush, or <see langword="null"/> when the theme is not loaded — the icon then inherits.</returns>
    private static IBrush? FindBrush(string key)
        => Application.Current is { } application && application.TryFindResource(key, out object? value)
            ? value as IBrush
            : null;
}
