using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Enigma.HardCopy.Recover.Views;

/// <summary>
/// The application's single view: the page, plus the info bar and dialog hosts drawn over it.
/// </summary>
public partial class MainView : UserControl
{
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
        _insets = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insets is null)
        {
            return;
        }

        _insets.SafeAreaChanged += OnSafeAreaChanged;
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
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => ApplySafeArea();

    private void ApplySafeArea()
    {
        if (_insets is not null)
        {
            Padding = _insets.SafeAreaPadding;
        }
    }
}
