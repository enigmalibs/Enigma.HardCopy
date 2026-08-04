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
}
