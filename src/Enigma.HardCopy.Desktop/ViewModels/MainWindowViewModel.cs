using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// ViewModel of the application shell. A placeholder in PHASE01 — the Backup/Recover navigation it
/// will host is built in PHASE05.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>The window title.</summary>
    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    } = "Enigma.HardCopy";
}
