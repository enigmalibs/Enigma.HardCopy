using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.HardCopy.Desktop.Diagnostics;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.Settings;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// The settings page: the appearance of the application — the one preference it keeps — and the way to its About
/// dialog.
/// </summary>
/// <remarks>
/// <para>
/// The barcode density is not here. It is chosen per run, on the backup page, because the page layout is
/// tuned around the default and a density remembered from a month ago would silently change how many pages
/// the next backup takes.
/// </para>
/// <para>
/// Choosing a variant does two things, in this order and exactly once each: it repaints the application
/// through <see cref="IThemeService"/>, and it writes the choice through <see cref="IAppSettingsStore"/>. The
/// stored choice is read once here, into the backing field rather than through the property, because
/// <c>App</c> has already applied it before the window was shown — going through the setter would repaint
/// what is already painted and rewrite the file on every first visit to this page.
/// </para>
/// </remarks>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _theme;
    private readonly IAppSettingsStore _store;
    private readonly IAboutDialogService _about;

    private ThemeOption _selectedTheme;

    /// <summary>Initializes a new instance of the <see cref="SettingsViewModel"/> class.</summary>
    /// <param name="theme">Repaints the application when the choice changes.</param>
    /// <param name="store">Holds the choice between runs.</param>
    /// <param name="about">Shows the About dialog from the about row.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public SettingsViewModel(IThemeService theme, IAppSettingsStore store, IAboutDialogService about)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(about);

        _theme = theme;
        _store = store;
        _about = about;

        OpenAboutCommand = new AsyncRelayCommand(_about.ShowAsync);

        // A page that throws while being built is a blank window, so a stored value outside the enum falls
        // back here as well — the store already guards its own file, this guards the contract.
        AppTheme stored = store.Load().Theme;
        _selectedTheme = Enum.IsDefined(stored) ? ThemeOption.For(stored) : ThemeOption.Default;
    }

    /// <summary>Gets the about row's description: the product's name and the version of the running build.</summary>
    public string AboutNote { get; } = string.Format(
        CultureInfo.CurrentCulture,
        Strings.SettingsAboutNoteFormat,
        ProductInformation.DisplayName,
        ProductInformation.Version);

    /// <summary>Gets the command that opens the About dialog.</summary>
    public AsyncRelayCommand OpenAboutCommand { get; }

    /// <summary>Gets the variants on offer.</summary>
    public IReadOnlyList<ThemeOption> Themes => ThemeOption.All;

    /// <summary>Gets or sets the variant the application is painted in.</summary>
    public ThemeOption SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            // A selector can hand back null while its items are being replaced, and re-selecting what is
            // already selected is not a change: neither is worth a repaint or a write.
            if (value is null || !SetProperty(ref _selectedTheme, value))
            {
                return;
            }

            _theme.Apply(value.Theme);
            _store.Save(new AppSettings { Theme = value.Theme });
        }
    }
}
