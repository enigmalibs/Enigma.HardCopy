using System;
using Android.Views;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// The <see cref="IHapticFeedback"/> of an Android phone: the system's own confirm and reject ticks, played on the
/// current activity's window.
/// </summary>
/// <remarks>
/// <c>View.PerformHapticFeedback</c> needs no permission and honours the user's touch-feedback setting, which a raw
/// vibration would not. The dedicated confirm and reject patterns exist from Android 11; older phones get the
/// closest standard ticks.
/// </remarks>
internal sealed class ViewHapticFeedback : IHapticFeedback
{
    private readonly ActivityTracker _activities;

    /// <summary>Initializes a new instance of the <see cref="ViewHapticFeedback"/> class.</summary>
    /// <param name="activities">Knows the activity whose window plays the tick.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activities"/> is <see langword="null"/>.</exception>
    public ViewHapticFeedback(ActivityTracker activities)
    {
        ArgumentNullException.ThrowIfNull(activities);

        _activities = activities;
    }

    /// <inheritdoc/>
    public void Accepted()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Perform(FeedbackConstants.Confirm);
        }
        else
        {
            Perform(FeedbackConstants.VirtualKey);
        }
    }

    /// <inheritdoc/>
    public void Rejected()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Perform(FeedbackConstants.Reject);
        }
        else
        {
            Perform(FeedbackConstants.LongPress);
        }
    }

    private void Perform(FeedbackConstants feedback) => _activities.Current?.Window?.DecorView.PerformHapticFeedback(feedback);
}
