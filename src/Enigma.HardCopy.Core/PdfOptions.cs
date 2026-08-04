using System;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The tunable inputs of <see cref="IPdfComposer.Compose"/>.
/// </summary>
/// <remarks>
/// <para>
/// Page size is deliberately not among them — A4 only is a validated decision, so
/// <see cref="PageLayout"/> treats the sheet as a constant.
/// </para>
/// <para>
/// What <i>is</i> adjustable is how generously a symbol is printed.
/// <see cref="MinimumModuleSizeInMillimetres"/> is a floor, not a size: the layout packs the densest grid
/// an A4 page allows while still honouring it, then grows each cell into whatever space that grid leaves
/// over. Raising the floor therefore trades codes-per-page for scanning robustness.
/// </para>
/// </remarks>
public sealed record PdfOptions
{
    /// <summary>
    /// The smallest permitted value of <see cref="MinimumModuleSizeInMillimetres"/>. Below roughly this
    /// size a module stops surviving an ordinary office printer and a phone camera.
    /// </summary>
    public const float MinModuleSizeInMillimetres = 0.20f;

    /// <summary>
    /// The largest permitted value of <see cref="MinimumModuleSizeInMillimetres"/>. This is only the top of
    /// the accepted range, not a promise: whether a floor this high can be honoured depends on how many
    /// modules the backup's longest code needs, and <see cref="PageLayout"/> rejects the combinations that
    /// cannot fit an A4 page.
    /// </summary>
    public const float MaxModuleSizeInMillimetres = 2f;

    /// <summary>The smallest permitted page margin, in millimetres — most printers cannot print closer.</summary>
    public const float MinPageMarginInMillimetres = 5f;

    /// <summary>The largest permitted page margin, in millimetres.</summary>
    public const float MaxPageMarginInMillimetres = 40f;

    /// <summary>The largest permitted gap between two printed codes, in millimetres.</summary>
    public const float MaxCodeSpacingInMillimetres = 20f;

    /// <summary>Gets the default options, which is what the application always uses.</summary>
    public static PdfOptions Default { get; } = new();

    /// <summary>Gets the options each individual QR symbol is rendered with.</summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public QrRenderOptions Qr
    {
        get => field;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = QrRenderOptions.Default;

    /// <summary>
    /// Gets the smallest printed size of one QR module, in millimetres, between
    /// <see cref="MinModuleSizeInMillimetres"/> and <see cref="MaxModuleSizeInMillimetres"/>. Defaults to
    /// 0.35 mm, which is about eight dots of a 600 dpi printer.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the permitted range.</exception>
    public float MinimumModuleSizeInMillimetres
    {
        get => field;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinModuleSizeInMillimetres);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxModuleSizeInMillimetres);
            field = value;
        }
    } = 0.35f;

    /// <summary>
    /// Gets the page margin, in millimetres, between <see cref="MinPageMarginInMillimetres"/> and
    /// <see cref="MaxPageMarginInMillimetres"/>. Defaults to 10 mm.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the permitted range.</exception>
    public float PageMarginInMillimetres
    {
        get => field;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinPageMarginInMillimetres);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxPageMarginInMillimetres);
            field = value;
        }
    } = 10f;

    /// <summary>
    /// Gets the gap left between two neighbouring codes, in millimetres, from zero to
    /// <see cref="MaxCodeSpacingInMillimetres"/>. Defaults to 3 mm — enough white paper that a scanner
    /// locating one symbol does not wander into the next.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the permitted range.</exception>
    public float CodeSpacingInMillimetres
    {
        get => field;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxCodeSpacingInMillimetres);
            field = value;
        }
    } = 3f;
}
