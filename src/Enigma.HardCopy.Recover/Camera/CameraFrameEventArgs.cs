using System;

namespace Enigma.HardCopy.Recover.Camera;

/// <summary>
/// One frame from the camera, for the preview.
/// </summary>
public sealed class CameraFrameEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="CameraFrameEventArgs"/> class.</summary>
    /// <param name="frame">The frame.</param>
    public CameraFrameEventArgs(YuvFrame frame)
    {
        Frame = frame;
    }

    /// <summary>
    /// Gets the frame. Its buffers are reused for the next frame as soon as the handler returns: read them now, or
    /// copy what you need.
    /// </summary>
    public YuvFrame Frame { get; }
}
