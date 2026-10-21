namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// What came of asking the camera to start.
/// </summary>
/// <remarks>
/// None of these is exceptional: a user may refuse the permission, and a phone may have no usable camera. The
/// page shows each differently, so they are values rather than exceptions. <see cref="Unavailable"/> is the zero
/// value, so an uninitialised result can never read as a running camera.
/// </remarks>
public enum CameraStartResult
{
    /// <summary>
    /// There is no camera this application can use — none on the device, none this build supports, or one that
    /// failed to open.
    /// </summary>
    Unavailable = 0,

    /// <summary>The user refused the camera permission, now or for good.</summary>
    PermissionDenied,

    /// <summary>The camera is running and codes will be reported as they are seen.</summary>
    Started,
}
