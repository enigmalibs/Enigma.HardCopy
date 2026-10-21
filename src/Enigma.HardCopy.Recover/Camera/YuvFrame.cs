namespace Enigma.HardCopy.Recover.Camera;

/// <summary>
/// One camera frame in the YUV 4:2:0 layout every Android camera delivers (<c>YUV_420_888</c>): a full-resolution
/// luminance plane and two half-resolution chroma planes, each with its own strides.
/// </summary>
/// <param name="Y">The luminance plane, one byte per pixel, rows <paramref name="YRowStride"/> bytes apart.</param>
/// <param name="YRowStride">The distance in bytes between the starts of two luminance rows.</param>
/// <param name="U">The blue-difference chroma plane, at half the frame's width and height.</param>
/// <param name="V">The red-difference chroma plane, laid out exactly like <paramref name="U"/>.</param>
/// <param name="ChromaRowStride">The distance in bytes between the starts of two chroma rows, in either plane.</param>
/// <param name="ChromaPixelStride">
/// The distance in bytes between two neighbouring chroma samples: 1 for planar layouts, 2 when the camera
/// interleaves U and V in one buffer (the common case on Android).
/// </param>
/// <param name="Width">The frame's width in pixels, as the sensor delivers it.</param>
/// <param name="Height">The frame's height in pixels, as the sensor delivers it.</param>
/// <param name="RotationDegrees">
/// How far the frame must be turned clockwise — 0, 90, 180 or 270 — to appear upright on the screen. A phone held in
/// portrait typically reports 90: its sensor is mounted in landscape.
/// </param>
/// <remarks>
/// Android guarantees that the two chroma planes share their row and pixel strides, which is why there is one pair
/// of chroma strides rather than two. The buffers belong to the producer and are reused for the next frame: a
/// consumer reads them while it is being handed the frame, and copies anything it wants to keep.
/// </remarks>
public readonly record struct YuvFrame(
    byte[] Y,
    int YRowStride,
    byte[] U,
    byte[] V,
    int ChromaRowStride,
    int ChromaPixelStride,
    int Width,
    int Height,
    int RotationDegrees);
