using System;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Settings;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// The appearance selector's entries: every variant the enum has is offered, in the order the page shows them,
/// each carrying the words a user reads rather than the enum's own name.
/// </summary>
public sealed class ThemeOptionTests
{
    [Fact]
    public void All_OffersEveryVariantTheEnumHas()
        => Assert.Equal(Enum.GetValues<AppTheme>().Length, ThemeOption.All.Count);

    [Fact]
    public void All_StartsWithFollowTheSystem_BecauseThatIsTheDefault()
        => Assert.Equal(AppTheme.System, ThemeOption.All[0].Theme);

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void For_ReturnsTheOfferedOption(AppTheme theme)
    {
        ThemeOption option = ThemeOption.For(theme);

        Assert.Equal(theme, option.Theme);
        Assert.Contains(option, ThemeOption.All);
    }

    [Fact]
    public void For_RejectsAVariantThatIsNotOffered()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ThemeOption.For((AppTheme)9));

    [Fact]
    public void Default_IsFollowTheSystem() => Assert.Equal(AppTheme.System, ThemeOption.Default.Theme);

    [Fact]
    public void Labels_AreTheTranslatedStrings_NotTheEnumNames()
    {
        Assert.Equal(Strings.SettingsThemeSystem, ThemeOption.For(AppTheme.System).Label);
        Assert.Equal(Strings.SettingsThemeLight, ThemeOption.For(AppTheme.Light).Label);
        Assert.Equal(Strings.SettingsThemeDark, ThemeOption.For(AppTheme.Dark).Label);
    }

    /// <summary>A combo box with no item template falls back to this, so it has to be the label.</summary>
    [Fact]
    public void ToString_IsTheLabel()
        => Assert.Equal(Strings.SettingsThemeDark, ThemeOption.For(AppTheme.Dark).ToString());
}
