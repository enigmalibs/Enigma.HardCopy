using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia.Android;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// The single activity; Avalonia renders the whole application inside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Portrait only.</b> A scanner is held one-handed over a sheet of paper, and a fixed orientation keeps the
/// camera frame's rotation constant for the whole session. Resizing, split screen and dark-mode switches are
/// handled in place rather than by recreating the activity, so Avalonia simply lays out again.
/// </para>
/// <para>
/// <b>The window is secure.</b> The screen shows codes and file details that are the secret being recovered.
/// Android saves a screenshot of every recent task to disk for its app switcher, and would let any screen
/// recorder capture the camera preview; <c>FLAG_SECURE</c> stops both, which is the only way an application that
/// promises to store nothing can keep that promise about the system's own copies.
/// </para>
/// </remarks>
[Activity(
    Theme = "@style/AppTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Before anything is drawn, so not even the first frame can be captured.
        Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);

        base.OnCreate(savedInstanceState);
    }
}
