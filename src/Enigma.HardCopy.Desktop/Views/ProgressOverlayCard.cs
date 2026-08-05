using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Enigma.HardCopy.Desktop.Views;

/// <summary>
/// The card the modal overlay puts on screen while a long operation runs: what is being done, which stage it
/// has reached, and — only where the operation can actually be abandoned — the way to stop it.
/// </summary>
/// <remarks>
/// <para>
/// The control library ships no progress card of its own: <c>IOverlayService.ShowAsync</c> takes any
/// <see cref="Control"/>, and this is the one this application hands it. Its theme is a
/// <c>ControlTheme</c> in <c>ProgressOverlayCard.axaml</c>, merged into <c>App.axaml</c> <b>after</b> the
/// library's dictionary so its keys resolve against the same <c>Enigma*</c> brushes.
/// </para>
/// <para>
/// <see cref="CancelCommand"/> is what tells the card whether the run is abandonable. Left
/// <see langword="null"/> — which is the case for importing, rebuilding and saving, none of which take a
/// cancellation token today — the button is not rendered at all, rather than shown disabled: a button that
/// cannot ever work is worse than no button.
/// </para>
/// </remarks>
public sealed class ProgressOverlayCard : ContentControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ProgressOverlayCard, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ProgressOverlayCard, string?>(nameof(Message));

    /// <summary>Defines the <see cref="IsIndeterminate"/> property.</summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressOverlayCard, bool>(nameof(IsIndeterminate), defaultValue: true);

    /// <summary>Defines the <see cref="Progress"/> property.</summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressOverlayCard, double>(nameof(Progress));

    /// <summary>Defines the <see cref="CancelCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ProgressOverlayCard, ICommand?>(nameof(CancelCommand));

    /// <summary>Gets or sets the operation the card stands for.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the stage currently running. The line is hidden while it is empty.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the progress bar is indeterminate. It is by default: Core's
    /// encoder and composer are single calls with no progress callback, so a percentage would be invented.
    /// </summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Gets or sets how far the operation has got, from 0 to 100. Ignored while indeterminate.</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that abandons the operation, or <see langword="null"/> when it cannot be
    /// abandoned — in which case no cancel button is rendered.
    /// </summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }
}
