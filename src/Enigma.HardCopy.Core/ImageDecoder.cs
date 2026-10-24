using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Reads the QR codes out of an imported image — a scan or a photograph of a printed backup page.
/// </summary>
/// <remarks>
/// <para>
/// This type is the seam between a recovery and the barcode-reading library, as
/// <see cref="QrRenderer"/> is on the way out. Nothing of ZXing.Net or SkiaSharp crosses it: an image goes
/// in as a stream and code strings come out, so either library can be replaced without touching
/// <see cref="RecoverySession"/>. The image formats are whatever SkiaSharp decodes, which covers the PNG,
/// JPEG and BMP a scanner or a phone produces.
/// </para>
/// <para>
/// <b>One page, many codes.</b> A sheet carries a dozen symbols, so the reader is asked for all of them in
/// one pass — a single-barcode read would return one arbitrary symbol per page and quietly lose the rest.
/// </para>
/// <para>
/// A tightly cropped single symbol is read by a second pass, because it is a different problem: the
/// multi-symbol detector works outwards from the page it expects around each symbol, and an image that is
/// nothing but one code and its quiet zone gives it nothing to work from. Both shapes reach a recovery in
/// practice — a scanned page and a single exported symbol — so both are tried before an image is reported
/// as holding no codes.
/// </para>
/// <para>
/// <b>Two kinds of input.</b> An imported image arrives encoded — PNG, JPEG, BMP — through
/// <see cref="TryReadCodes"/>. A live camera frame arrives as raw luminance through
/// <see cref="ReadCodes(byte[], int, int, int)"/>:
/// a camera's Y plane is already exactly what the reader binarizes, so it is read as it comes, with no
/// colour conversion and no image codec in the way.
/// </para>
/// <para>
/// Decoding is synchronous and CPU-bound: like <see cref="QrRenderer"/> and <see cref="PdfComposer"/>, it
/// does not choose a thread for the caller. A UI caller imports images from a background task.
/// </para>
/// </remarks>
public static class ImageDecoder
{
    /// <summary>
    /// Attempts to read every QR code in <paramref name="image"/>. Returns <see langword="false"/> only when
    /// the bytes are not a decodable image — a readable image with no codes on it succeeds with an empty
    /// list, which is a different thing to tell the user.
    /// </summary>
    /// <param name="image">The image's bytes, read from the stream's current position to its end.</param>
    /// <param name="codes">
    /// When this method returns <see langword="true"/>, the text of every code found, in the order the
    /// reader found them; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="image"/> was decodable as an image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public static bool TryReadCodes(Stream image, [NotNullWhen(true)] out IReadOnlyList<string>? codes)
    {
        ArgumentNullException.ThrowIfNull(image);

        using SKBitmap? bitmap = SKBitmap.Decode(image);
        if (bitmap is null)
        {
            codes = null;
            return false;
        }

        codes = ReadCodes(bitmap);
        return true;
    }

    /// <summary>
    /// Reads every QR code in one frame of a live camera feed, given as 8-bit luminance — the Y plane of a
    /// YUV frame, used as the camera delivers it.
    /// </summary>
    /// <param name="luminance">
    /// The frame's pixels, one byte each, row after row, each row starting <paramref name="rowStride"/> bytes
    /// after the one before. The last row may stop at its last pixel rather than at the stride — a camera
    /// plane's buffer commonly does — so <c>rowStride × (height − 1) + width</c> bytes are enough.
    /// </param>
    /// <param name="width">The frame's width in pixels.</param>
    /// <param name="height">The frame's height in pixels.</param>
    /// <param name="rowStride">
    /// The distance in bytes from the start of one row to the start of the next; at least
    /// <paramref name="width"/>, larger when the camera pads its rows.
    /// </param>
    /// <returns>The text of every code found, in the order the reader found them; empty when there is none.</returns>
    /// <remarks>
    /// <para>
    /// <c>TryHarder</c> stays on, as for an imported image, even though a live frame is retried by the next
    /// one. Without it the multi-symbol detector samples only every few rows and misses symbols outright — a
    /// frame holding four codes reads as one — so a phone held over a page would collect its codes one at a
    /// time instead of all at once. A caller that cannot afford the cost drops frames while a read is running
    /// rather than reading each one less thoroughly.
    /// </para>
    /// <para>
    /// As with an image, the whole frame is searched for every symbol first, then for a single one: a phone
    /// held close to one code fills the frame with that code and little else, which the multi-symbol detector
    /// cannot locate.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="luminance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/> or <paramref name="height"/> is not positive, or <paramref name="rowStride"/> is
    /// less than <paramref name="width"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="luminance"/> is shorter than the frame its dimensions describe.
    /// </exception>
    public static IReadOnlyList<string> ReadCodes(byte[] luminance, int width, int height, int rowStride)
    {
        ArgumentNullException.ThrowIfNull(luminance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(rowStride, width);

        long requiredLength = ((long)rowStride * (height - 1)) + width;
        if (luminance.Length < requiredLength)
        {
            throw new ArgumentException(
                $"A {width}×{height} frame with a row stride of {rowStride} needs at least {requiredLength} bytes; "
                + $"the buffer holds {luminance.Length}.",
                nameof(luminance));
        }

        PlanarYUVLuminanceSource frame = new(
            luminance,
            dataWidth: rowStride,
            dataHeight: height,
            left: 0,
            top: 0,
            width,
            height,
            reverseHoriz: false);

        BarcodeReaderGeneric reader = CreateFrameReader();
        Result[]? results = reader.DecodeMultiple(frame);
        if (results is { Length: > 0 })
        {
            List<string> codes = new(results.Length);
            foreach (Result result in results)
            {
                codes.Add(result.Text);
            }

            return codes;
        }

        Result? single = reader.Decode(frame);

        return single is null ? [] : [single.Text];
    }

    private static IReadOnlyList<string> ReadCodes(SKBitmap bitmap)
    {
        Result[]? results = CreateReader(pureBarcode: false).DecodeMultiple(bitmap);
        if (results is { Length: > 0 })
        {
            List<string> codes = new(results.Length);
            foreach (Result result in results)
            {
                codes.Add(result.Text);
            }

            return codes;
        }

        // Nothing on the page — or the image is a single cropped symbol, which the multi-symbol detector
        // cannot locate. See the type's remarks.
        Result? single = CreateReader(pureBarcode: true).Decode(bitmap);

        return single is null ? [] : [single.Text];
    }

    /// <summary>
    /// Builds a reader restricted to QR codes. <c>TryHarder</c> is on because an imported image is a scan or
    /// a photograph — rotated, skewed, unevenly lit — and the extra passes it buys are worth far more than
    /// the milliseconds they cost when the alternative is telling the user to re-scan a page.
    /// </summary>
    private static ZXing.SkiaSharp.BarcodeReader CreateReader(bool pureBarcode) => new()
    {
        Options = new DecodingOptions
        {
            PossibleFormats = [BarcodeFormat.QR_CODE],
            PureBarcode = pureBarcode,
            TryHarder = true,
        },
    };

    /// <summary>
    /// Builds a reader for live camera frames: QR codes only, with <c>TryHarder</c> — see
    /// <see cref="ReadCodes(byte[], int, int, int)"/> for why. Never <c>PureBarcode</c>: a camera frame always
    /// has the page, the desk or the user's hand around the symbol.
    /// </summary>
    private static BarcodeReaderGeneric CreateFrameReader() => new()
    {
        Options = new DecodingOptions
        {
            PossibleFormats = [BarcodeFormat.QR_CODE],
            TryHarder = true,
        },
    };
}
