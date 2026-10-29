using System;
using System.Reflection;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.Diagnostics;

/// <summary>
/// Identity of the Enigma.HardCopy product as the About dialog and the Settings page state it: its name, the
/// version and revision of the running build, and its copyright.
/// </summary>
/// <remarks>
/// In the desktop app rather than in Core: only this application shows it, and Core is the format library the
/// Android app ships as well.
/// </remarks>
public static class ProductInformation
{
    /// <summary>
    /// The version and the revision this build was cut from, read once from this assembly.
    /// </summary>
    /// <remarks>
    /// From <b>this</b> assembly rather than the entry assembly: under a test host the entry assembly is the
    /// runner. Core and the desktop app carry one version between them (<c>Directory.Build.props</c>).
    /// </remarks>
    private static readonly ProductVersion Product = ProductVersion.From(
        typeof(ProductInformation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        FallbackVersion());

    /// <summary>Gets the product's name in words, as the About dialog and the Settings page show it.</summary>
    public static string DisplayName => Strings.AboutProductName;

    /// <summary>Gets the displayable version of the running build.</summary>
    public static string Version => Product.Version;

    /// <summary>
    /// Gets the short commit this build was cut from, or <see langword="null"/> when it recorded none — a build
    /// from a source drop rather than from the repository.
    /// </summary>
    public static string? BuildSha => Product.BuildSha;

    /// <summary>
    /// Gets the copyright notice <c>Directory.Build.props</c> stamps on the assembly, or an empty string when it
    /// carries none.
    /// </summary>
    public static string Copyright { get; } =
        typeof(ProductInformation).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright?.Trim() ?? string.Empty;

    /// <summary>
    /// The plain assembly version, for when the informational one says nothing usable, trimmed to three
    /// components: the fourth is a zero nobody wrote.
    /// </summary>
    private static string? FallbackVersion()
    {
        Version? version = typeof(ProductInformation).Assembly.GetName().Version;

        return version is null ? null : version.ToString(version.Build >= 0 ? 3 : 2);
    }
}
