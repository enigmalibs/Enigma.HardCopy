namespace Enigma.HardCopy.Core;

/// <summary>
/// One rendered QR symbol: the PNG image plus the geometry the page layout needs to place it.
/// </summary>
/// <remarks>
/// <para>
/// This is a class and not a record, unlike the other option and result types of this library, precisely
/// because it carries <see cref="Png"/>: a record's generated equality and <c>ToString</c> would compare and
/// print an array by reference, which is a trap rather than a feature. Symbols are compared by their code
/// string, never by their pixels.
/// </para>
/// <para>
/// Every measurement here is reported rather than recomputed by the caller, because the page layout has to
/// reason about symbols of different sizes: the metadata code is short and the last data chunk is usually
/// partial, so a single backup normally mixes several QR versions on one sheet.
/// </para>
/// </remarks>
public sealed class QrSymbol
{
    /// <summary>
    /// Gets the symbol as PNG bytes — one bit per pixel, greyscale, with the quiet zone already drawn.
    /// </summary>
    /// <remarks>
    /// The quiet zone is part of the image rather than something the layout is trusted to add, because a
    /// symbol printed hard against a caption or a neighbouring code is a symbol no scanner will read.
    /// </remarks>
    public required byte[] Png { get; init; }

    /// <summary>Gets the QR symbol version, 1 to 40, that the code needed.</summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets the number of modules along one side of the image, <b>including</b> the quiet zone on both sides.
    /// </summary>
    /// <remarks>
    /// The quiet zone is counted in because this is the number that converts to a printed length: at a target
    /// module size in millimetres, this count times that size is the space the symbol occupies on the page.
    /// </remarks>
    public required int ModulesPerSide { get; init; }

    /// <summary>Gets the side of one module in PNG pixels, as rendered.</summary>
    public required int PixelsPerModule { get; init; }

    /// <summary>
    /// Gets the side of the PNG in pixels. The image is square, so this is both its width and its height.
    /// </summary>
    public int PixelSize => ModulesPerSide * PixelsPerModule;
}
