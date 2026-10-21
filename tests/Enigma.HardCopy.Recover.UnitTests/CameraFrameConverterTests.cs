using System;
using Enigma.HardCopy.Recover.Camera;
using Xunit;

namespace Enigma.HardCopy.Recover.UnitTests;

/// <summary>
/// The camera preview's pixels: the right size, the right way up, the right colours, whatever strides the camera
/// chose.
/// </summary>
/// <remarks>
/// The orientation tests use a 4×4 frame — a 2×2 preview — whose four luminance blocks are 40, 80, 120 and 160 from
/// top-left in reading order, with neutral chroma so each output pixel is plain grey at its block's level. Turning
/// the frame then moves those four greys to predictable corners.
/// </remarks>
public sealed class CameraFrameConverterTests
{
    private const byte TopLeft = 40;
    private const byte TopRight = 80;
    private const byte BottomLeft = 120;
    private const byte BottomRight = 160;

    [Theory]
    [InlineData(1920, 1080, 0, 960, 540)]
    [InlineData(1920, 1080, 90, 540, 960)]
    [InlineData(1920, 1080, 180, 960, 540)]
    [InlineData(1920, 1080, 270, 540, 960)]
    [InlineData(641, 481, 0, 320, 240)]
    public void GetPreviewSize_HalvesTheFrame_AndSwapsItForAQuarterTurn(
        int width, int height, int rotation, int expectedWidth, int expectedHeight)
        => Assert.Equal((expectedWidth, expectedHeight), CameraFrameConverter.GetPreviewSize(width, height, rotation));

    [Theory]
    [InlineData(0, new[] { TopLeft, TopRight, BottomLeft, BottomRight })]
    [InlineData(90, new[] { BottomLeft, TopLeft, BottomRight, TopRight })]
    [InlineData(180, new[] { BottomRight, BottomLeft, TopRight, TopLeft })]
    [InlineData(270, new[] { TopRight, BottomRight, TopLeft, BottomLeft })]
    public void ToBgraPreview_TurnsTheFrameUpright(int rotation, byte[] expectedReadingOrder)
    {
        byte[] preview = new byte[2 * 2 * CameraFrameConverter.BytesPerPixel];

        CameraFrameConverter.ToBgraPreview(QuadrantFrame(rotation), preview);

        Assert.Equal(expectedReadingOrder, GreyLevels(preview));
    }

    // Android commonly interleaves the chroma samples (pixel stride 2) and pads every row; neither may change what
    // the preview shows.
    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(1, 16, 8)]
    [InlineData(2, 16, 8)]
    public void ToBgraPreview_ReadsAnyStrides(int chromaPixelStride, int lumaPadding, int chromaPadding)
    {
        byte[] preview = new byte[2 * 2 * CameraFrameConverter.BytesPerPixel];

        CameraFrameConverter.ToBgraPreview(QuadrantFrame(0, chromaPixelStride, lumaPadding, chromaPadding), preview);

        Assert.Equal([TopLeft, TopRight, BottomLeft, BottomRight], GreyLevels(preview));
    }

    [Fact]
    public void ToBgraPreview_ConvertsColour_FullRangeBt601()
    {
        // Y=76, U=85, V=255 is the JFIF encoding of pure red: R=255, G=0, B=0, give or take integer rounding.
        byte[] preview = new byte[CameraFrameConverter.BytesPerPixel];

        CameraFrameConverter.ToBgraPreview(SolidFrame(luma: 76, blueDifference: 85, redDifference: 255), preview);

        Assert.InRange(preview[2], 250, 255);
        Assert.InRange(preview[1], 0, 5);
        Assert.InRange(preview[0], 0, 5);
        Assert.Equal(0xFF, preview[3]);
    }

    [Fact]
    public void ToBgraPreview_ClampsOutOfGamutValues()
    {
        byte[] preview = new byte[CameraFrameConverter.BytesPerPixel];

        CameraFrameConverter.ToBgraPreview(SolidFrame(luma: 250, blueDifference: 255, redDifference: 255), preview);

        Assert.Equal(255, preview[0]);
        Assert.Equal(255, preview[2]);
    }

    // A plane's buffer commonly ends at its last sample rather than at its last stride.
    [Fact]
    public void ToBgraPreview_AcceptsPlanesWhoseLastRowIsUnpadded()
    {
        YuvFrame frame = QuadrantFrame(0, chromaPixelStride: 2, lumaPadding: 16, chromaPadding: 8);
        YuvFrame trimmed = frame with
        {
            Y = frame.Y[..((frame.YRowStride * 3) + 4)],
            U = frame.U[..(frame.ChromaRowStride + frame.ChromaPixelStride + 1)],
            V = frame.V[..(frame.ChromaRowStride + frame.ChromaPixelStride + 1)],
        };
        byte[] preview = new byte[2 * 2 * CameraFrameConverter.BytesPerPixel];

        CameraFrameConverter.ToBgraPreview(trimmed, preview);

        Assert.Equal([TopLeft, TopRight, BottomLeft, BottomRight], GreyLevels(preview));
    }

    [Fact]
    public void ToBgraPreview_ATooShortPlane_Throws()
    {
        YuvFrame frame = QuadrantFrame(0);

        Assert.Throws<ArgumentException>(() => CameraFrameConverter.ToBgraPreview(frame with { Y = new byte[10] }, new byte[16]));
        Assert.Throws<ArgumentException>(() => CameraFrameConverter.ToBgraPreview(frame with { V = new byte[2] }, new byte[16]));
    }

    [Fact]
    public void ToBgraPreview_ATooShortDestination_Throws()
        => Assert.Throws<ArgumentException>(() => CameraFrameConverter.ToBgraPreview(QuadrantFrame(0), new byte[15]));

    [Theory]
    [InlineData(45)]
    [InlineData(-90)]
    [InlineData(360)]
    public void ToBgraPreview_ARotationThatIsNotAQuarterTurn_Throws(int rotation)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => CameraFrameConverter.ToBgraPreview(QuadrantFrame(0) with { RotationDegrees = rotation }, new byte[16]));

    [Fact]
    public void ToBgraPreview_AStrideBelowTheWidth_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => CameraFrameConverter.ToBgraPreview(QuadrantFrame(0) with { YRowStride = 3 }, new byte[16]));

    /// <summary>
    /// A 4×4 frame whose four 2×2 luminance blocks carry the four corner levels, with neutral chroma.
    /// </summary>
    private static YuvFrame QuadrantFrame(int rotation, int chromaPixelStride = 1, int lumaPadding = 0, int chromaPadding = 0)
    {
        const int size = 4;
        int lumaStride = size + lumaPadding;
        byte[] y = new byte[lumaStride * size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                y[(row * lumaStride) + column] = (row < 2, column < 2) switch
                {
                    (true, true) => TopLeft,
                    (true, false) => TopRight,
                    (false, true) => BottomLeft,
                    _ => BottomRight,
                };
            }
        }

        int chromaStride = (2 * chromaPixelStride) + chromaPadding;
        byte[] u = new byte[chromaStride * 2];
        byte[] v = new byte[chromaStride * 2];

        // Every byte neutral: with an interleaved layout the gaps hold the other plane's samples, which are neutral
        // too, so a reader that mixed the planes up would still be caught by the colour test instead.
        Array.Fill(u, (byte)128);
        Array.Fill(v, (byte)128);

        return new YuvFrame(y, lumaStride, u, v, chromaStride, chromaPixelStride, size, size, rotation);
    }

    /// <summary>A 2×2 frame of one colour — a single preview pixel.</summary>
    private static YuvFrame SolidFrame(byte luma, byte blueDifference, byte redDifference)
        => new([luma, luma, luma, luma], 2, [blueDifference], [redDifference], 1, 1, 2, 2, 0);

    /// <summary>Each preview pixel's grey level, in reading order — after checking it really is grey.</summary>
    private static byte[] GreyLevels(byte[] preview)
    {
        byte[] levels = new byte[preview.Length / CameraFrameConverter.BytesPerPixel];
        for (int pixel = 0; pixel < levels.Length; pixel++)
        {
            int offset = pixel * CameraFrameConverter.BytesPerPixel;
            Assert.Equal(preview[offset], preview[offset + 1]);
            Assert.Equal(preview[offset], preview[offset + 2]);
            Assert.Equal(0xFF, preview[offset + 3]);
            levels[pixel] = preview[offset];
        }

        return levels;
    }
}
