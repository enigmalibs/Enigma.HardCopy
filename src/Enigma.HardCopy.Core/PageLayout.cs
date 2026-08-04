using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The geometry of a printed backup: how large one code is on the sheet, how many fit a page, and therefore
/// how many pages the document has.
/// </summary>
/// <remarks>
/// <para>
/// No grid is hard-coded here. The only print constraint is
/// <see cref="PdfOptions.MinimumModuleSizeInMillimetres"/>; the grid is the densest one an A4 sheet allows
/// while honouring it, and each cell is then grown to fill whatever space that grid leaves over. A small
/// chunk size therefore yields more, smaller codes per page and a large one fewer, bigger codes, with no
/// per-preset tuning — and the printed module size always ends up at or above the floor, never below.
/// </para>
/// <para>
/// Computing the grid up front is also what makes the page count knowable <i>before</i> the document is
/// composed, which the UI's page estimate and the <c>Page n / m</c> footer both depend on. Page 1 carries
/// the recovery-spec box and so holds fewer rows than the pages after it.
/// </para>
/// </remarks>
public sealed class PageLayout
{
    /// <summary>The width of an A4 sheet, in millimetres.</summary>
    public const float PageWidthInMillimetres = 210f;

    /// <summary>The height of an A4 sheet, in millimetres.</summary>
    public const float PageHeightInMillimetres = 297f;

    /// <summary>
    /// The height reserved for the page header — the file name, hash, backup ID and summary line repeated
    /// on every page — in millimetres.
    /// </summary>
    public const float HeaderHeightInMillimetres = 18f;

    /// <summary>The height reserved for the page footer, in millimetres.</summary>
    public const float FooterHeightInMillimetres = 7f;

    /// <summary>
    /// The height reserved under each symbol for its index caption, in millimetres.
    /// </summary>
    public const float CaptionHeightInMillimetres = 4f;

    /// <summary>
    /// The height reserved on page 1 for the printed recovery instructions, in millimetres. The box is a
    /// fixed size so the page count stays predictable; it is sized with several lines of slack over what
    /// <see cref="RecoveryInstructions"/> actually needs.
    /// </summary>
    public const float InstructionsHeightInMillimetres = 60f;

    /// <summary>
    /// Shaved off both usable dimensions before the grid is computed, in millimetres, so that a grid can
    /// never come out filling the page to the very last hair. It only takes accumulated single-precision
    /// error in the millimetre-to-point conversion for the renderer to judge such a grid one whisker too
    /// large and refuse the whole document — and a grid one whisker too tall would silently become an extra
    /// page, making the printed <c>n / m</c> a lie. A quarter of a millimetre is thousands of times the
    /// error being guarded against and costs no density at any supported chunk size.
    /// </summary>
    private const float SafetyMarginInMillimetres = 0.25f;

    private PageLayout(
        int codeCount,
        int qrVersion,
        int modulesPerSide,
        float marginInMillimetres,
        float spacingInMillimetres,
        float cellSizeInMillimetres,
        int columns,
        int rowsPerPage,
        int rowsOnFirstPage,
        int pageCount)
    {
        CodeCount = codeCount;
        QrVersion = qrVersion;
        ModulesPerSide = modulesPerSide;
        MarginInMillimetres = marginInMillimetres;
        SpacingInMillimetres = spacingInMillimetres;
        CellSizeInMillimetres = cellSizeInMillimetres;
        Columns = columns;
        RowsPerPage = rowsPerPage;
        RowsOnFirstPage = rowsOnFirstPage;
        PageCount = pageCount;
    }

    /// <summary>Gets the number of codes laid out, the metadata code included.</summary>
    public int CodeCount { get; }

    /// <summary>
    /// Gets the QR symbol version the cell is sized for — the version of the <b>longest</b> code, so every
    /// code of the backup fits the same cell.
    /// </summary>
    public int QrVersion { get; }

    /// <summary>Gets the modules per side of that symbol, both quiet zones included.</summary>
    public int ModulesPerSide { get; }

    /// <summary>Gets the page margin, in millimetres.</summary>
    public float MarginInMillimetres { get; }

    /// <summary>Gets the gap between two neighbouring codes, in millimetres.</summary>
    public float SpacingInMillimetres { get; }

    /// <summary>Gets the printed side of one QR symbol, in millimetres (a symbol is square).</summary>
    public float CellSizeInMillimetres { get; }

    /// <summary>
    /// Gets the printed size of one QR module, in millimetres. Never below
    /// <see cref="PdfOptions.MinimumModuleSizeInMillimetres"/>.
    /// </summary>
    public float ModuleSizeInMillimetres => CellSizeInMillimetres / ModulesPerSide;

    /// <summary>Gets the full height of one grid cell — symbol plus caption — in millimetres.</summary>
    public float CellHeightInMillimetres => CellSizeInMillimetres + CaptionHeightInMillimetres;

    /// <summary>Gets the number of codes per grid row.</summary>
    public int Columns { get; }

    /// <summary>Gets the number of grid rows on a page that carries no recovery-spec box.</summary>
    public int RowsPerPage { get; }

    /// <summary>
    /// Gets the number of grid rows on page 1, which shares its height with the recovery-spec box. May be
    /// zero, in which case page 1 carries the instructions alone.
    /// </summary>
    public int RowsOnFirstPage { get; }

    /// <summary>Gets the number of codes on a page that carries no recovery-spec box.</summary>
    public int CodesPerPage => Columns * RowsPerPage;

    /// <summary>Gets the number of codes on page 1.</summary>
    public int CodesOnFirstPage => Columns * RowsOnFirstPage;

    /// <summary>Gets the number of pages the document has.</summary>
    public int PageCount { get; }

    /// <summary>Computes the layout of <paramref name="backup"/>.</summary>
    /// <param name="backup">The encoded backup to be printed.</param>
    /// <param name="options">The composition options, or <see langword="null"/> for <see cref="PdfOptions.Default"/>.</param>
    /// <returns>The layout of the document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The backup holds no codes, or a symbol large enough for its longest code does not fit an A4 page
    /// under <paramref name="options"/>.
    /// </exception>
    public static PageLayout ForBackup(EncodedBackup backup, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(backup);

        return ForCodes(backup.Codes, options);
    }

    /// <summary>Computes the layout of a list of code strings.</summary>
    /// <param name="codes">The codes to be printed, in printing order.</param>
    /// <param name="options">The composition options, or <see langword="null"/> for <see cref="PdfOptions.Default"/>.</param>
    /// <returns>The layout of the document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="codes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="codes"/> is empty or holds an empty entry, or a symbol large enough for the longest
    /// code does not fit an A4 page under <paramref name="options"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// No QR symbol at all can hold the longest code at the error-correction level of
    /// <paramref name="options"/>. Raising the level above <see cref="QrErrorCorrectionLevel.Medium"/>
    /// shrinks the capacity that <see cref="EncodeOptions.MaxChunkSizeInBytes"/> was chosen against, so a
    /// large chunk and a high level can be individually valid yet impossible together.
    /// </exception>
    public static PageLayout ForCodes(IReadOnlyList<string> codes, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Count == 0)
        {
            throw new ArgumentException("A backup always has at least the metadata code.", nameof(codes));
        }

        options ??= PdfOptions.Default;

        // The cell is sized for the longest code, not for each code's own version: a uniform cell is what
        // makes the grid a grid. A shorter code — the metadata block especially — simply prints with
        // larger modules inside the same square, which only helps it scan.
        string longest = codes[0];
        foreach (string code in codes)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("A code is empty.", nameof(codes));
            }

            if (code.Length > longest.Length)
            {
                longest = code;
            }
        }

        return Create(codes.Count, QrRenderer.GetVersion(longest, options.Qr.ErrorCorrection), options);
    }

    /// <summary>
    /// Estimates the layout a file of <paramref name="fileSizeInBytes"/> bytes would produce, without
    /// encoding it — what the UI needs to warn about a large input before any work is done.
    /// </summary>
    /// <param name="fileSizeInBytes">The size of the file to be backed up, in bytes.</param>
    /// <param name="encodeOptions">The encoding options, or <see langword="null"/> for <see cref="EncodeOptions.Default"/>.</param>
    /// <param name="pdfOptions">The composition options, or <see langword="null"/> for <see cref="PdfOptions.Default"/>.</param>
    /// <returns>The estimated layout.</returns>
    /// <remarks>
    /// The estimate is deliberately the <b>worst case</b>, and so never promises fewer pages than the real
    /// document needs. It assumes the file does not compress — a compressible one produces fewer chunks —
    /// and it sizes the cell for a full chunk even when the file is smaller than one.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fileSizeInBytes"/> is negative, or no QR symbol can hold a full chunk at the
    /// error-correction level of <paramref name="pdfOptions"/> — see <see cref="ForCodes"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A symbol large enough for a full chunk does not fit an A4 page under <paramref name="pdfOptions"/>.
    /// </exception>
    public static PageLayout Estimate(
        long fileSizeInBytes,
        EncodeOptions? encodeOptions = null,
        PdfOptions? pdfOptions = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileSizeInBytes);

        encodeOptions ??= EncodeOptions.Default;
        pdfOptions ??= PdfOptions.Default;

        int chunkCount = Chunker.CountChunks(fileSizeInBytes, encodeOptions.ChunkSizeInBytes);
        int longestCodeLength = GetChunkCodeLength(Math.Max(chunkCount, 1), encodeOptions.ChunkSizeInBytes);

        return Create(
            chunkCount + 1,
            QrRenderer.GetVersionForLength(longestCodeLength, pdfOptions.Qr.ErrorCorrection),
            pdfOptions);
    }

    /// <summary>Returns the number of codes printed on page <paramref name="pageNumber"/>.</summary>
    /// <param name="pageNumber">The page number, from 1 to <see cref="PageCount"/>.</param>
    /// <returns>
    /// The number of codes on that page. Zero only when page 1 is filled by the recovery-spec box alone.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="pageNumber"/> is below 1 or above <see cref="PageCount"/>.
    /// </exception>
    public int GetCodeCountOnPage(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageNumber, PageCount);

        int capacity = pageNumber == 1 ? CodesOnFirstPage : CodesPerPage;
        int printedEarlier = pageNumber == 1 ? 0 : CodesOnFirstPage + ((pageNumber - 2) * CodesPerPage);

        return Math.Clamp(CodeCount - printedEarlier, 0, capacity);
    }

    /// <summary>
    /// Returns the length of the code carrying chunk <paramref name="chunkCount"/> of
    /// <paramref name="chunkCount"/> — the longest a code of such a backup can be, since the index and
    /// total fields are at their widest and the payload is a full chunk.
    /// </summary>
    private static int GetChunkCodeLength(int chunkCount, int chunkSizeInBytes)
    {
        // Measured through the real codec rather than recomputed, so the estimate cannot drift from the
        // format. The extra character is the separator between the header and the payload.
        string header = HeaderCodec.FormatHeader(new CodeHeader("AAAA", chunkCount, chunkCount, uint.MaxValue));

        return header.Length + 1 + Base32.GetSymbolCount(chunkSizeInBytes);
    }

    private static PageLayout Create(int codeCount, int qrVersion, PdfOptions options)
    {
        int modulesPerSide = QrRenderer.GetModulesPerSide(qrVersion);
        float margin = options.PageMarginInMillimetres;
        float spacing = options.CodeSpacingInMillimetres;

        float usableWidth = PageWidthInMillimetres - (2f * margin) - SafetyMarginInMillimetres;
        float contentHeight = PageHeightInMillimetres
            - (2f * margin)
            - HeaderHeightInMillimetres
            - FooterHeightInMillimetres
            - SafetyMarginInMillimetres;
        float firstPageContentHeight = contentHeight - InstructionsHeightInMillimetres - spacing;

        float smallestCellSize = modulesPerSide * options.MinimumModuleSizeInMillimetres;
        int columns = CountThatFit(usableWidth, smallestCellSize, spacing);
        int rowsPerPage = CountThatFit(contentHeight, smallestCellSize + CaptionHeightInMillimetres, spacing);
        if (columns == 0 || rowsPerPage == 0)
        {
            throw new ArgumentException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A version-{qrVersion} symbol needs at least {smallestCellSize:0.#} mm at {options.MinimumModuleSizeInMillimetres} mm per module, which does not fit an A4 page with a {margin} mm margin. Lower the minimum module size, the margin, or the chunk size."),
                nameof(options));
        }

        // Both fits are, by construction, at least the smallest permitted cell — so taking the smaller of
        // the two keeps the module size above its floor while wasting neither dimension.
        float widthFit = (usableWidth - ((columns - 1) * spacing)) / columns;
        float heightFit = ((contentHeight - ((rowsPerPage - 1) * spacing)) / rowsPerPage) - CaptionHeightInMillimetres;
        float cellSize = MathF.Min(widthFit, heightFit);

        int rowsOnFirstPage = CountThatFit(firstPageContentHeight, cellSize + CaptionHeightInMillimetres, spacing);
        int codesOnFirstPage = columns * rowsOnFirstPage;
        int pageCount = codeCount <= codesOnFirstPage
            ? 1
            : 1 + DivideRoundingUp(codeCount - codesOnFirstPage, columns * rowsPerPage);

        return new PageLayout(
            codeCount,
            qrVersion,
            modulesPerSide,
            margin,
            spacing,
            cellSize,
            columns,
            rowsPerPage,
            rowsOnFirstPage,
            pageCount);
    }

    /// <summary>
    /// Returns how many items of <paramref name="itemSize"/>, separated by <paramref name="spacing"/>, fit
    /// in <paramref name="available"/>. Zero when not even one does, including when
    /// <paramref name="available"/> is negative.
    /// </summary>
    private static int CountThatFit(float available, float itemSize, float spacing)
        => available < itemSize ? 0 : (int)MathF.Floor((available + spacing) / (itemSize + spacing));

    private static int DivideRoundingUp(int dividend, int divisor) => (dividend + divisor - 1) / divisor;
}
