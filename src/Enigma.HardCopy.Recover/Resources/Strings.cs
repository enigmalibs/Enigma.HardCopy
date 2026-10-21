using System.Globalization;
using System.Resources;

namespace Enigma.HardCopy.Recover.Resources;

/// <summary>
/// Every user-facing string of the application, read from <c>Strings.resx</c>.
/// </summary>
/// <remarks>
/// <para>
/// Written by hand rather than generated, as in the desktop app: the generated accessor is
/// <see langword="internal"/> and lands in the intermediate output, which makes it awkward to reach from XAML and
/// impossible to review; one line per string keeps the surface public, documented and diffable.
/// </para>
/// <para>
/// Every property name <b>is</b> its resource key, and <see cref="Get"/> throws when a key is absent, so a string
/// missing from the <c>.resx</c> is a loud failure rather than a blank label. The accompanying test walks this
/// class by reflection and reads every property, which turns that failure into a build-time one.
/// </para>
/// <para>
/// Names ending in <c>Format</c> are composite format strings, and their arguments are documented in the
/// <c>.resx</c> comment beside each one.
/// </para>
/// </remarks>
public static class Strings
{
    private const string ResourceBaseName = "Enigma.HardCopy.Recover.Resources.Strings";

    private static readonly ResourceManager _resources = new(ResourceBaseName, typeof(Strings).Assembly);

    // ===== Shell =====

    /// <summary>The application's name, shown in its header.</summary>
    public static string AppTitle => Get(nameof(AppTitle));

    /// <summary>The line under the name saying what the application is for.</summary>
    public static string AppSubtitle => Get(nameof(AppSubtitle));

    // ===== Introduction =====

    /// <summary>The paragraph explaining what a recovery involves.</summary>
    public static string IntroText => Get(nameof(IntroText));

    /// <summary>What the application keeps on the phone — nothing — and what it blocks.</summary>
    public static string PrivacyNote => Get(nameof(PrivacyNote));

    /// <summary>
    /// Reads one string from the resources. Throws rather than returning a placeholder: a key that is not there
    /// is a defect in this file, and a blank label in the running application would hide it.
    /// </summary>
    /// <param name="name">The resource key, which is always the calling property's own name.</param>
    /// <returns>The string for the current UI culture.</returns>
    /// <exception cref="MissingManifestResourceException">The key is not in the resources.</exception>
    private static string Get(string name)
        => _resources.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"'{name}' is missing from {ResourceBaseName}.");
}
