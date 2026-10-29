using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Pins the application icon to the branding set: the ten-frame <c>app.ico</c> the executable and the window
/// carry, and the 256-pixel PNG the About dialog shows.
/// </summary>
/// <remarks>
/// Headers only, read straight from the bytes: decoding an image would need Avalonia's platform, and the frame
/// list and the PNG's size are what a wrong file would get wrong.
/// </remarks>
public sealed class AppIconTests
{
    private static readonly int[] ExpectedFrameSizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void AppIco_HoldsTheTenBrandingFrames_EachAPng()
    {
        byte[] ico = File.ReadAllBytes(RepositoryRoot.Combine("src/Enigma.HardCopy.Desktop/Assets/app.ico"));

        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(0)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)));
        int count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));

        int[] sizes = new int[count];
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> entry = ico.AsSpan(6 + (16 * i), 16);

            // A width byte of 0 means 256: the field is one byte wide.
            sizes[i] = entry[0] == 0 ? 256 : entry[0];
            Assert.Equal(entry[0], entry[1]);

            int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            Assert.True(ico.AsSpan(offset).StartsWith(PngSignature), $"The {sizes[i]} px frame is not a PNG.");
        }

        Assert.Equal(ExpectedFrameSizes, sizes.Order());
    }

    [Fact]
    public void AppPng_IsTheIconsLargestFrame_At256Pixels()
    {
        byte[] png = File.ReadAllBytes(RepositoryRoot.Combine("src/Enigma.HardCopy.Desktop/Assets/app-256.png"));

        Assert.True(png.AsSpan().StartsWith(PngSignature));

        // IHDR is always the first chunk: width and height follow its 4-byte length and 4-byte type.
        Assert.Equal(256u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(256u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20)));
    }
}
