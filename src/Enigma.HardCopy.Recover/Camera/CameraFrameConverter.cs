using System;

namespace Enigma.HardCopy.Recover.Camera;

/// <summary>
/// Turns a camera's YUV frame into the colour preview the screen shows: BGRA pixels, at half the sensor's
/// resolution, turned upright.
/// </summary>
/// <remarks>
/// <para>
/// <b>Half resolution, on purpose.</b> In 4:2:0 the chroma planes already are at half resolution, so sampling one
/// luminance pixel per two-by-two block lines every output pixel up with exactly one chroma sample — no
/// interpolation — and quarters the work. A 1920×1080 frame becomes a 540×960 preview, which is more than a phone's
/// preview area needs; the codes themselves are read from the full-resolution luminance, not from this.
/// </para>
/// <para>
/// The colour conversion is the full-range BT.601 transform cameras use (the JPEG/JFIF one), in integer arithmetic.
/// </para>
/// </remarks>
public static class CameraFrameConverter
{
    /// <summary>The bytes per output pixel: blue, green, red, alpha.</summary>
    public const int BytesPerPixel = 4;

    /// <summary>Gets the size of the preview a frame of the given shape turns into.</summary>
    /// <param name="width">The frame's width in pixels.</param>
    /// <param name="height">The frame's height in pixels.</param>
    /// <param name="rotationDegrees">The frame's rotation: 0, 90, 180 or 270.</param>
    /// <returns>The preview's width and height: half the frame's, swapped for a quarter turn.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A dimension is below 2, or <paramref name="rotationDegrees"/> is not a quarter turn.
    /// </exception>
    public static (int Width, int Height) GetPreviewSize(int width, int height, int rotationDegrees)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
        ThrowIfNotAQuarterTurn(rotationDegrees);

        return rotationDegrees is 90 or 270 ? (height / 2, width / 2) : (width / 2, height / 2);
    }

    /// <summary>Writes the upright, half-resolution BGRA preview of <paramref name="frame"/>.</summary>
    /// <param name="frame">The camera frame.</param>
    /// <param name="destination">
    /// Where the pixels go, rows packed with no padding: at least width × height × <see cref="BytesPerPixel"/>
    /// bytes of the size <see cref="GetPreviewSize"/> returns.
    /// </param>
    /// <exception cref="ArgumentNullException">A plane or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The frame's shape or strides are impossible.</exception>
    /// <exception cref="ArgumentException">A plane or <paramref name="destination"/> is too short.</exception>
    public static void ToBgraPreview(in YuvFrame frame, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        Validate(frame);

        (int outWidth, int outHeight) = GetPreviewSize(frame.Width, frame.Height, frame.RotationDegrees);
        if (destination.Length < outWidth * outHeight * BytesPerPixel)
        {
            throw new ArgumentException(
                $"A {outWidth}×{outHeight} preview needs {outWidth * outHeight * BytesPerPixel} bytes; "
                + $"the destination holds {destination.Length}.",
                nameof(destination));
        }

        int halfWidth = frame.Width / 2;
        int halfHeight = frame.Height / 2;

        // Where a row of the output starts in the half-resolution source, and which way the source is walked as the
        // output moves right. The inverse of turning the source clockwise by the frame's rotation.
        (int columnStepX, int columnStepY) = frame.RotationDegrees switch
        {
            90 => (0, -1),
            180 => (-1, 0),
            270 => (0, 1),
            _ => (1, 0),
        };

        byte[] y = frame.Y;
        byte[] u = frame.U;
        byte[] v = frame.V;
        int output = 0;
        for (int row = 0; row < outHeight; row++)
        {
            (int sourceX, int sourceY) = frame.RotationDegrees switch
            {
                90 => (row, halfHeight - 1),
                180 => (halfWidth - 1, halfHeight - 1 - row),
                270 => (halfWidth - 1 - row, 0),
                _ => (0, row),
            };

            for (int column = 0; column < outWidth; column++)
            {
                int luma = y[(2 * sourceY * frame.YRowStride) + (2 * sourceX)];
                int chroma = (sourceY * frame.ChromaRowStride) + (sourceX * frame.ChromaPixelStride);
                int blueDifference = u[chroma] - 128;
                int redDifference = v[chroma] - 128;

                // Full-range BT.601, scaled by 256: 1.402, 0.344, 0.714 and 1.772.
                destination[output] = Clamp(luma + ((454 * blueDifference) >> 8));
                destination[output + 1] = Clamp(luma - (((88 * blueDifference) + (183 * redDifference)) >> 8));
                destination[output + 2] = Clamp(luma + ((359 * redDifference) >> 8));
                destination[output + 3] = 0xFF;
                output += BytesPerPixel;

                sourceX += columnStepX;
                sourceY += columnStepY;
            }
        }
    }

    private static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);

    private static void Validate(in YuvFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame.Y);
        ArgumentNullException.ThrowIfNull(frame.U);
        ArgumentNullException.ThrowIfNull(frame.V);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.Width, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.Height, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.YRowStride, frame.Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frame.ChromaPixelStride);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.ChromaRowStride, (frame.Width / 2) * frame.ChromaPixelStride);
        ThrowIfNotAQuarterTurn(frame.RotationDegrees);

        // A plane's last row may stop at its last sample rather than at the stride, as camera buffers commonly do.
        long lumaLength = ((long)frame.YRowStride * (frame.Height - 1)) + frame.Width;
        long chromaLength = ((long)frame.ChromaRowStride * ((frame.Height / 2) - 1))
            + ((long)frame.ChromaPixelStride * ((frame.Width / 2) - 1)) + 1;

        ThrowIfShorter(frame.Y, lumaLength, "Y");
        ThrowIfShorter(frame.U, chromaLength, "U");
        ThrowIfShorter(frame.V, chromaLength, "V");
    }

    private static void ThrowIfShorter(byte[] plane, long required, string name)
    {
        if (plane.Length < required)
        {
            throw new ArgumentException(
                $"The {name} plane holds {plane.Length} bytes; the frame's shape needs at least {required}.",
                nameof(plane));
        }
    }

    private static void ThrowIfNotAQuarterTurn(int rotationDegrees)
    {
        if (rotationDegrees is not (0 or 90 or 180 or 270))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees),
                rotationDegrees,
                "A camera frame's rotation is 0, 90, 180 or 270 degrees.");
        }
    }
}
