namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// One camera-style luminance frame, as <see cref="ImageDecoder.ReadCodes(byte[], int, int, int)"/> takes it.
/// </summary>
/// <param name="Pixels">One byte per pixel, rows <paramref name="RowStride"/> bytes apart.</param>
/// <param name="Width">The frame's width in pixels.</param>
/// <param name="Height">The frame's height in pixels.</param>
/// <param name="RowStride">The distance in bytes between the starts of two consecutive rows.</param>
internal sealed record LuminanceFrame(byte[] Pixels, int Width, int Height, int RowStride);
