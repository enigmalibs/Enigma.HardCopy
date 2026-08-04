using Avalonia.Controls;

namespace Enigma.HardCopy.Desktop.Views;

/// <summary>
/// The backup page. Its <see cref="Control.DataContext"/> is the
/// <see cref="ViewModels.BackupViewModel"/> the shell binds into it.
/// </summary>
public partial class BackupView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="BackupView"/> class.</summary>
    public BackupView() => InitializeComponent();
}
