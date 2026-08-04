using System;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The tunable inputs of <see cref="QrRenderer.Render"/>.
/// </summary>
/// <remarks>
/// <para>
/// Both settings are deliberately coarse. Everything else about a symbol — its version, its module count, its
/// quiet zone — follows from the code being rendered and from the QR standard, so exposing it as an option
/// would only offer the caller a way to produce paper that cannot be scanned.
/// </para>
/// <para>
/// <see cref="Default"/> is the pair the page layout is tuned for; a caller that has no opinion should pass
/// <see langword="null"/> and get it.
/// </para>
/// </remarks>
public sealed record QrRenderOptions
{
    /// <summary>
    /// The smallest permitted pixels-per-module value: one pixel per module, the bare module matrix. Legal,
    /// because a PDF or a viewer can scale it, but every scaling step then shows nearest-neighbour edges.
    /// </summary>
    public const int MinPixelsPerModule = 1;

    /// <summary>
    /// The largest permitted pixels-per-module value. A version-40 symbol at this scale is a 5920-pixel
    /// square PNG — about as large as is defensible before the images dominate the size of the PDF.
    /// </summary>
    public const int MaxPixelsPerModule = 32;

    /// <summary>
    /// Gets the default options — <see cref="QrErrorCorrectionLevel.Medium"/> at 8 pixels per module.
    /// </summary>
    public static QrRenderOptions Default { get; } = new();

    /// <summary>
    /// Gets the error-correction level of the symbol. Defaults to <see cref="QrErrorCorrectionLevel.Medium"/>,
    /// the level the whole capacity budget of the format is computed against.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrection { get; init; } = QrErrorCorrectionLevel.Medium;

    /// <summary>
    /// Gets the side of one QR module in PNG pixels, between <see cref="MinPixelsPerModule"/> and
    /// <see cref="MaxPixelsPerModule"/>. Defaults to 8.
    /// </summary>
    /// <remarks>
    /// The rendered PNG is a raster that the PDF scales to a fixed physical size, so this value sets the
    /// print resolution rather than the printed size. At the plan's target of 0.35 mm per module, 8 pixels
    /// per module works out to roughly 580 dpi: above what a laser printer resolves, and therefore free of
    /// the module-edge rounding that a lower value would print.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the permitted range.</exception>
    public int PixelsPerModule
    {
        get => field;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinPixelsPerModule);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxPixelsPerModule);
            field = value;
        }
    } = 8;
}
