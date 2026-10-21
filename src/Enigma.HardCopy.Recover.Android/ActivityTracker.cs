using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// Knows which activity is current, for the services that need one — the camera binds to its lifecycle and asks
/// for permissions through it, and haptics play on its window.
/// </summary>
/// <remarks>
/// Android creates activities itself, outside the container, so they cannot be injected. The application object
/// registers this tracker for every activity's lifecycle and hands the same instance to the container — one owner,
/// no static state.
/// </remarks>
internal sealed class ActivityTracker : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
{
    /// <summary>Gets the activity in front, or <see langword="null"/> when there is none.</summary>
    public AppCompatActivity? Current { get; private set; }

    /// <inheritdoc/>
    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState) => Track(activity);

    /// <inheritdoc/>
    public void OnActivityStarted(Activity activity) => Track(activity);

    /// <inheritdoc/>
    public void OnActivityResumed(Activity activity) => Track(activity);

    /// <inheritdoc/>
    public void OnActivityPaused(Activity activity)
    {
    }

    /// <inheritdoc/>
    public void OnActivityStopped(Activity activity)
    {
    }

    /// <inheritdoc/>
    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {
    }

    /// <inheritdoc/>
    public void OnActivityDestroyed(Activity activity)
    {
        if (ReferenceEquals(Current, activity))
        {
            Current = null;
        }
    }

    private void Track(Activity activity)
    {
        if (activity is AppCompatActivity current)
        {
            Current = current;
        }
    }
}
