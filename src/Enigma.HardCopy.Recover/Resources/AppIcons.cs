using Avalonia.Media;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

namespace Enigma.HardCopy.Recover.Resources;

/// <summary>
/// The Phosphor icon outlines this application uses, resolved once.
/// </summary>
/// <remarks>
/// As in the desktop app: <c>ToGeometry()</c> re-parses the path data on every call and hands back a fresh,
/// mutable <see cref="Geometry"/>, so each outline is resolved once and kept. A view picks one up by
/// <c>x:Static</c>; one consumer per icon keeps the shared instance safe.
/// </remarks>
public static class AppIcons
{
    /// <summary>The application itself, in its header: what it reads.</summary>
    public static Geometry App { get; } = Resolve(PhosphorIcon.QrCode);

    /// <summary>Resolves one Phosphor glyph, at the regular weight, into its outline.</summary>
    /// <param name="icon">The icon to resolve.</param>
    /// <returns>The glyph's outline, ready for a <c>PathIcon.Data</c> or any other geometry slot.</returns>
    private static Geometry Resolve(PhosphorIcon icon) =>
        PhosphorIconSet.Instance.GetGlyph(icon, PhosphorWeight.Regular).ToGeometry();
}
