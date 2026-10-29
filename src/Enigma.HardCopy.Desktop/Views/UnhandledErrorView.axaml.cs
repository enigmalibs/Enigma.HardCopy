using Avalonia.Controls;

namespace Enigma.HardCopy.Desktop.Views;

/// <summary>
/// The content of the safety net's dialog.
/// </summary>
/// <remarks>
/// Deliberately logic-free: everything it shows comes from <see cref="ViewModels.UnhandledErrorViewModel"/>.
/// </remarks>
public partial class UnhandledErrorView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="UnhandledErrorView"/> class.</summary>
    public UnhandledErrorView() => InitializeComponent();
}
