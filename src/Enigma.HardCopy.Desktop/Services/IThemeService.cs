using Enigma.HardCopy.Desktop.Settings;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Repaints the whole application in a chosen colour variant.
/// </summary>
/// <remarks>
/// The application's own seam over Avalonia's theme variant, for the same reason
/// <see cref="IFileDialogService"/> and <see cref="IProgressOverlay"/> exist: the one place that names a
/// framework type is the implementation, so a ViewModel can offer the choice — and be tested offering it —
/// with no windowing platform behind it.
/// </remarks>
public interface IThemeService
{
    /// <summary>Repaints the application. Takes effect immediately, on every window already open.</summary>
    /// <param name="theme">The variant to paint in.</param>
    void Apply(AppTheme theme);
}
