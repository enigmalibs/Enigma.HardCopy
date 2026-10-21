using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Enigma.HardCopy.Recover.Views;

/// <summary>
/// The application's single view: the page, plus the info bar and dialog hosts drawn over it.
/// </summary>
public partial class MainView : UserControl
{
    private TopLevel? _topLevel;
    private IInsetsManager? _insets;

    /// <summary>Initializes a new instance of the <see cref="MainView"/> class.</summary>
    public MainView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Android 15 and later always draw edge to edge: keep the content clear of the status bar, the display
        // cut-out and the gesture bar. A platform with no insets manager has nothing to keep clear of.
        _topLevel = TopLevel.GetTopLevel(this);
        _insets = _topLevel?.InsetsManager;
        if (_topLevel is null || _insets is null)
        {
            return;
        }

        _insets.SafeAreaChanged += OnSafeAreaChanged;
        _topLevel.ScalingChanged += OnScalingChanged;
        ApplySafeArea();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        if (_topLevel is not null)
        {
            _topLevel.ScalingChanged -= OnScalingChanged;
            _topLevel = null;
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => ApplySafeArea();

    // Avalonia.Android computes the safe area as the system insets divided by the render scaling, and raises
    // SafeAreaChanged only when the insets change. At startup the insets can arrive before the display density is
    // known — the scaling is still 1 — so the value then is in pixels, two to three times too large, and no later
    // event corrects it. Re-reading the property once the scaling is known does.
    private void OnScalingChanged(object? sender, EventArgs e) => ApplySafeArea();

    private void ApplySafeArea()
    {
        if (_insets is not null)
        {
            Padding = _insets.SafeAreaPadding;
        }
    }
}
