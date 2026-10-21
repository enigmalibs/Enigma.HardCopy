namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// Where a scan session stands, from the camera's point of view.
/// </summary>
public enum ScanState
{
    /// <summary>
    /// The camera is off and the session can take more codes — before the first scan, after the user paused, or
    /// after the camera could not start.
    /// </summary>
    Ready = 0,

    /// <summary>The camera has been asked to start — the permission prompt may be up.</summary>
    Starting,

    /// <summary>The camera is running and every code it reads goes into the session.</summary>
    Scanning,

    /// <summary>Every code is in. The camera has stopped by itself; the file can be saved.</summary>
    Complete,
}
