using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// ViewModel of the application shell: the window title, and which of the two pages is showing.
/// </summary>
/// <remarks>
/// Navigation is ViewModel-first — the shell selects a <see cref="PageViewModel"/> and the
/// <see cref="ViewLocator"/> finds the view for it. Both pages are constructed once, at startup, and kept:
/// each holds work in progress — a file chosen for backup, a recovery half fed with codes — and moving between
/// tabs must not discard it.
/// <para>
/// The selection is held as an index rather than as a page reference so that it has a valid default with no
/// constructor gymnastics, and <see cref="CurrentPage"/> is derived from it: there is one piece of navigation
/// state, and it cannot get out of step with itself.
/// </para>
/// </remarks>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel"/> class.</summary>
    /// <param name="backup">The backup page.</param>
    /// <param name="recover">The recovery page.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public MainWindowViewModel(BackupViewModel backup, RecoverViewModel recover)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(recover);

        Backup = backup;
        Recover = recover;
        Pages = [backup, recover];
    }

    /// <summary>Gets the window title.</summary>
    public string Title => Strings.AppTitle;

    /// <summary>Gets the backup page.</summary>
    public BackupViewModel Backup { get; }

    /// <summary>Gets the recovery page.</summary>
    public RecoverViewModel Recover { get; }

    /// <summary>Gets both pages, in navigation order.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>
    /// Gets or sets the index of the page showing. Values outside the range of <see cref="Pages"/> are clamped
    /// into it rather than throwing: this is bound to a control's selection, and an empty selection arriving as
    /// <c>-1</c> during template application should leave the shell on a valid page.
    /// </summary>
    public int SelectedPageIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Clamp(value, 0, Pages.Count - 1)))
            {
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
    }

    /// <summary>Gets the page currently showing.</summary>
    public PageViewModel CurrentPage => Pages[SelectedPageIndex];
}
