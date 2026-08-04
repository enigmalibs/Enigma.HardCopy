using System;
using QRCoder;
using QRCoder.Exceptions;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Renders a barcode string as a QR symbol, and answers the sizing questions the page layout asks before any
/// symbol exists.
/// </summary>
/// <remarks>
/// <para>
/// This type is the seam between the format and the barcode library. Nothing of QRCoder crosses it: the
/// caller supplies one of our <see cref="QrErrorCorrectionLevel"/> values and receives a
/// <see cref="QrSymbol"/>, so the library can be replaced without touching the PDF composer or the app.
/// </para>
/// <para>
/// <b>Alphanumeric mode is the load-bearing property of this seam.</b> Every code produced by
/// <see cref="BackupEncoder"/> is restricted to <see cref="HardCopyFormat.AlphanumericCharset"/>, which lets
/// the encoder pack two characters into 11 bits. Byte mode would spend 8 bits per character — about 45% more —
/// and the capacity budget the page layout is built on would collapse. Mode selection is left to the library,
/// which detects it from the text; this renderer never forces UTF-8 and never requests an ECI mode, because
/// either would drop the symbol to byte mode.
/// </para>
/// <para>
/// Rendering is CPU-bound and synchronous. Like <see cref="BackupEncoder"/>, it does not choose a thread for
/// the caller: a UI caller renders a whole document from a background task.
/// </para>
/// </remarks>
public static class QrRenderer
{
    /// <summary>
    /// The quiet zone, in modules, on <b>each</b> side of the symbol — the four modules the QR standard
    /// requires. Without it a scanner cannot find the symbol's edges.
    /// </summary>
    public const int QuietZoneModules = 4;

    /// <summary>The lowest QR symbol version.</summary>
    private const int MinVersion = 1;

    /// <summary>The highest QR symbol version; there is no symbol beyond it.</summary>
    private const int MaxVersion = 40;

    /// <summary>The modules per side of a version-1 symbol, quiet zone excluded.</summary>
    private const int Version1Modules = 21;

    /// <summary>Each version step adds four modules per side.</summary>
    private const int ModulesPerVersionStep = 4;

    /// <summary>
    /// The character a synthetic probe string is built from in <see cref="GetVersionForLength"/>. Any member
    /// of the QR alphanumeric charset would do; <c>A</c> is simply the least surprising one to see in a dump.
    /// </summary>
    private const char AlphanumericProbeCharacter = 'A';

    /// <summary>
    /// The alphanumeric capacity of a version-40 symbol at <see cref="QrErrorCorrectionLevel.Low"/> — 4296
    /// characters — which is the largest number of characters <i>any</i> QR symbol can hold at <i>any</i>
    /// error-correction level. <see cref="GetVersionForLength"/> rejects anything longer before it builds a
    /// probe string, so an absurd length cannot make it allocate an absurd string. A length that merely
    /// exceeds the capacity of the <i>requested</i> level is caught by the render attempt itself.
    /// </summary>
    private const int MaxAlphanumericLength = 4296;

    /// <summary>Renders one barcode string as a QR symbol.</summary>
    /// <param name="code">The barcode string, as produced by <see cref="BackupEncoder"/>.</param>
    /// <param name="options">
    /// The render options, or <see langword="null"/> for <see cref="QrRenderOptions.Default"/>.
    /// </param>
    /// <returns>The rendered symbol, quiet zone included.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="code"/> is <see langword="null"/>, empty or whitespace, or is longer than any QR symbol
    /// can hold at the requested error-correction level.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="options"/> carries an error-correction level that is not a defined
    /// <see cref="QrErrorCorrectionLevel"/>.
    /// </exception>
    public static QrSymbol Render(string code, QrRenderOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        options ??= QrRenderOptions.Default;

        using QRCodeData data = GenerateForCode(code, options.ErrorCorrection);
        int version = data.Version;

        // PngByteQRCode is the cross-platform renderer of the pair QRCoder offers: it writes the PNG itself
        // and so pulls in no System.Drawing, which on Linux would need a native GDI+ stand-in.
        // Its Dispose only forwards to the QRCodeData it was handed — the same instance the outer `using`
        // owns — so both dispose the one object. QRCodeData.Dispose is idempotent, and disposing both keeps
        // this method free of an "except this one" exception to the rule.
        using PngByteQRCode renderer = new(data);
        byte[] png = renderer.GetGraphic(options.PixelsPerModule, drawQuietZones: true);

        return new QrSymbol
        {
            Png = png,
            Version = version,
            ModulesPerSide = GetModulesPerSide(version),
            PixelsPerModule = options.PixelsPerModule,
        };
    }

    /// <summary>Returns the QR symbol version <paramref name="code"/> needs, without rendering it.</summary>
    /// <param name="code">The barcode string, as produced by <see cref="BackupEncoder"/>.</param>
    /// <param name="errorCorrection">The error-correction level to size for.</param>
    /// <returns>The symbol version, between 1 and 40.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="code"/> is <see langword="null"/>, empty or whitespace, or is longer than any QR symbol
    /// can hold at <paramref name="errorCorrection"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="errorCorrection"/> is not a defined <see cref="QrErrorCorrectionLevel"/>.
    /// </exception>
    public static int GetVersion(string code, QrErrorCorrectionLevel errorCorrection = QrErrorCorrectionLevel.Medium)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        using QRCodeData data = GenerateForCode(code, errorCorrection);

        return data.Version;
    }

    /// <summary>
    /// Returns the QR symbol version an alphanumeric-mode string of <paramref name="alphanumericLength"/>
    /// characters needs.
    /// </summary>
    /// <remarks>
    /// This exists for the page-layout estimator, which has to size a grid — and report a page count to the
    /// user — before a single code has been encoded. It is exact rather than an approximation because QR
    /// alphanumeric mode packs characters in pairs at 11 bits per pair: the encoded bit length, and therefore
    /// the symbol version, depends only on <i>how many</i> characters there are and never on <i>which</i>
    /// characters they are. A synthetic run of <see cref="AlphanumericProbeCharacter"/> consequently sizes
    /// identically to any real code of the same length, which is the whole trick of this method.
    /// </remarks>
    /// <param name="alphanumericLength">The character count to size for; must be positive.</param>
    /// <param name="errorCorrection">The error-correction level to size for.</param>
    /// <returns>The symbol version, between 1 and 40.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="alphanumericLength"/> is not positive, or is longer than any QR symbol can hold at
    /// <paramref name="errorCorrection"/>; or <paramref name="errorCorrection"/> is not a defined
    /// <see cref="QrErrorCorrectionLevel"/>.
    /// </exception>
    public static int GetVersionForLength(
        int alphanumericLength,
        QrErrorCorrectionLevel errorCorrection = QrErrorCorrectionLevel.Medium)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alphanumericLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alphanumericLength, MaxAlphanumericLength);

        string probe = new(AlphanumericProbeCharacter, alphanumericLength);
        try
        {
            using QRCodeData data = Generate(probe, errorCorrection);

            return data.Version;
        }
        catch (DataTooLongException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alphanumericLength),
                alphanumericLength,
                $"No QR symbol can hold {alphanumericLength} alphanumeric characters at error-correction level {errorCorrection}.");
        }
    }

    /// <summary>
    /// Returns the modules per side of a symbol of <paramref name="version"/>, <b>including</b> the quiet zone
    /// on both sides.
    /// </summary>
    /// <param name="version">The symbol version, between 1 and 40.</param>
    /// <returns>
    /// <c>21 + 4 * (version - 1) + 2 * <see cref="QuietZoneModules"/></c> — the QR standard's module count for
    /// the version, widened by the quiet zone that <see cref="Render"/> draws into the image.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is outside 1 to 40.</exception>
    public static int GetModulesPerSide(int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, MinVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version, MaxVersion);

        return Version1Modules + (ModulesPerVersionStep * (version - 1)) + (2 * QuietZoneModules);
    }

    /// <summary>
    /// Generates the symbol data for a caller-supplied code, translating "no symbol is big enough" into an
    /// <see cref="ArgumentException"/> about the <c>code</c> parameter — which is what it is, from the point of
    /// view of every public entry point that takes one.
    /// </summary>
    private static QRCodeData GenerateForCode(string code, QrErrorCorrectionLevel errorCorrection)
    {
        try
        {
            return Generate(code, errorCorrection);
        }
        catch (DataTooLongException exception)
        {
            throw new ArgumentException(
                $"The code is {code.Length} characters, which no QR symbol can hold at error-correction level {errorCorrection}.",
                nameof(code),
                exception);
        }
    }

    /// <summary>
    /// The single call into QRCoder. The three flags are spelled out rather than left to their defaults
    /// because they are the ones that would silently cost us alphanumeric mode: <c>forceUtf8</c> and
    /// <c>utf8BOM</c> encode the text as bytes, and any <c>eciMode</c> other than the default prepends a
    /// character-set designator that only byte mode can carry. The symbol version is left unrequested so the
    /// library picks the smallest one that fits.
    /// </summary>
    private static QRCodeData Generate(string text, QrErrorCorrectionLevel errorCorrection)
        => QRCodeGenerator.GenerateQrCode(
            text,
            ToEccLevel(errorCorrection),
            forceUtf8: false,
            utf8BOM: false,
            eciMode: QRCodeGenerator.EciMode.Default);

    /// <summary>
    /// Maps our error-correction level onto QRCoder's. This mapping is the "build-tunable constant" the plan
    /// calls for: it is the only place the two vocabularies meet.
    /// </summary>
    private static QRCodeGenerator.ECCLevel ToEccLevel(QrErrorCorrectionLevel errorCorrection)
        => errorCorrection switch
        {
            QrErrorCorrectionLevel.Low => QRCodeGenerator.ECCLevel.L,
            QrErrorCorrectionLevel.Medium => QRCodeGenerator.ECCLevel.M,
            QrErrorCorrectionLevel.Quartile => QRCodeGenerator.ECCLevel.Q,
            QrErrorCorrectionLevel.High => QRCodeGenerator.ECCLevel.H,
            _ => throw new ArgumentOutOfRangeException(
                nameof(errorCorrection),
                errorCorrection,
                "The error-correction level is not a defined QrErrorCorrectionLevel."),
        };
}
