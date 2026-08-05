using Avalonia.Media;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

namespace Enigma.HardCopy.Desktop.Resources;

/// <summary>
/// The Phosphor icon outlines this application resolves from C#.
/// </summary>
/// <remarks>
/// <para>
/// XAML reaches the same artwork more briefly through <c>{ei:IconGeometry …}</c>, and that is the route to
/// prefer for a fixed icon written straight into markup. This class is for the geometries that have to be
/// chosen in code — a navigation item's icon is a <c>Geometry</c> property, not markup — and it is where a
/// view picks one up by <c>x:Static</c>.
/// </para>
/// <para>
/// Each outline is resolved once and kept: <c>ToGeometry()</c> re-parses the path data on every call and
/// hands back a fresh, mutable <see cref="Geometry"/>, so resolving per binding evaluation would be pure
/// waste. One consumer per icon means the shared instance is safe; an icon that needed its own
/// <see cref="Geometry.Transform"/> would have to resolve its own.
/// </para>
/// </remarks>
public static class AppIcons
{
    /// <summary>The backup page, on the navigation rail: what a printed backup looks like.</summary>
    public static Geometry Backup { get; } = Resolve(PhosphorIcon.QrCode);

    /// <summary>Abandoning a run that is already under way.</summary>
    public static Geometry Cancel { get; } = Resolve(PhosphorIcon.XCircle);

    /// <summary>The recovery page, on the navigation rail: the backup being turned back into the file.</summary>
    public static Geometry Recover { get; } = Resolve(PhosphorIcon.ArrowsCounterClockwise);

    /// <summary>Resolves one Phosphor glyph, at the regular weight, into its outline.</summary>
    /// <param name="icon">The icon to resolve.</param>
    /// <returns>The glyph's outline, ready for a <c>PathIcon.Data</c> or any other geometry slot.</returns>
    private static Geometry Resolve(PhosphorIcon icon) =>
        PhosphorIconSet.Instance.GetGlyph(icon, PhosphorWeight.Regular).ToGeometry();
}
