using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The default <see cref="IPdfComposer"/>: renders every code of a backup as a QR symbol and lays them out
/// on A4 pages, with the page furniture and the printed recovery instructions that make the sheets
/// self-describing.
/// </summary>
/// <remarks>
/// <para>
/// The grid is not written here — <see cref="PageLayout"/> computes it, and this type only draws what that
/// layout says. Keeping the arithmetic separate is what lets the page count be asserted, and shown in the
/// UI, without composing a document.
/// </para>
/// <para>
/// The work is CPU-bound: rendering the symbols dominates, and the token is honoured between them. A UI
/// caller runs this on a background task.
/// </para>
/// </remarks>
public sealed class PdfComposer : IPdfComposer
{
    private const float TitleFontSize = 8.5f;
    private const float BodyFontSize = 7.5f;
    private const float CaptionFontSize = 7f;
    private const float InstructionsTitleFontSize = 7.5f;
    private const float InstructionsFontSize = 6.5f;

    private const float RuleThicknessInPoints = 0.5f;
    private const float InstructionsBorderInPoints = 0.5f;
    private const float InstructionsPaddingInMillimetres = 3f;
    private const float ParagraphSpacingInMillimetres = 1.2f;
    private const float HeaderRulePaddingInMillimetres = 1f;
    private const float HeaderBottomPaddingInMillimetres = 2f;
    private const float FooterTopPaddingInMillimetres = 1.5f;

    /// <summary>
    /// The longest file name printed in the page header. The name is furniture for a human reading the
    /// sheet; the authoritative copy is inside the metadata code, so truncating a very long one costs
    /// nothing and keeps the header's height predictable.
    /// </summary>
    private const int MaxPrintedFileNameLength = 60;

    private const string EllipsisSuffix = "…";

    static PdfComposer()
    {
        // QuestPDF refuses to generate anything until a license type is declared. Doing it here rather than
        // at application startup means it cannot be forgotten — a static constructor is guaranteed to have
        // run before Compose can be called, including from a test host or a future CLI.
        QuestPDF.Settings.License = LicenseType.Community;

        // A file name may hold characters the bundled font has no glyph for — CJK, emoji. Left at its
        // default, QuestPDF would throw and the backup would fail over page furniture. A fallback box in
        // the header is the better trade: the name that matters is the one inside the metadata code.
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    /// <inheritdoc/>
    public byte[] Compose(
        EncodedBackup backup,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);

        options ??= PdfOptions.Default;

        PageLayout layout = PageLayout.ForBackup(backup, options);
        IReadOnlyList<QrSymbol> symbols = RenderSymbols(backup.Codes, options.Qr, cancellationToken);

        return Document
            .Create(document => document.Page(page => ComposePage(page, backup, symbols, layout)))
            .WithMetadata(BuildMetadata(backup))
            .GeneratePdf();
    }

    private static IReadOnlyList<QrSymbol> RenderSymbols(
        IReadOnlyList<string> codes,
        QrRenderOptions options,
        CancellationToken cancellationToken)
    {
        List<QrSymbol> symbols = new(codes.Count);
        foreach (string code in codes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            symbols.Add(QrRenderer.Render(code, options));
        }

        return symbols;
    }

    private static DocumentMetadata BuildMetadata(EncodedBackup backup) => new()
    {
        Title = string.Create(
            CultureInfo.InvariantCulture,
            $"Enigma.HardCopy paper backup of {backup.Metadata.FileName} ({backup.BackupId})"),
        Subject = "Paper backup — QR codes recoverable without this application",
        Author = "Enigma.HardCopy",
        Creator = "Enigma.HardCopy",

        // Taken from the backup rather than from the clock, so composing the same backup twice produces the
        // same document.
        CreationDate = backup.Metadata.CreatedOn.ToDateTime(TimeOnly.MinValue),
    };

    private static void ComposePage(
        PageDescriptor page,
        EncodedBackup backup,
        IReadOnlyList<QrSymbol> symbols,
        PageLayout layout)
    {
        page.Size(PageSizes.A4);
        page.Margin(layout.MarginInMillimetres, Unit.Millimetre);
        page.DefaultTextStyle(style => style.FontSize(BodyFontSize));

        ComposeHeader(page.Header().Height(PageLayout.HeaderHeightInMillimetres, Unit.Millimetre), backup);
        ComposeContent(page.Content(), backup, symbols, layout);
        ComposeFooter(page.Footer().Height(PageLayout.FooterHeightInMillimetres, Unit.Millimetre));
    }

    private static void ComposeHeader(IContainer container, EncodedBackup backup)
    {
        container.PaddingBottom(HeaderBottomPaddingInMillimetres, Unit.Millimetre).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Enigma.HardCopy — paper backup").FontSize(TitleFontSize).SemiBold();
                row.AutoItem()
                    .Text(string.Create(CultureInfo.InvariantCulture, $"Backup ID {backup.BackupId}"))
                    .FontSize(TitleFontSize)
                    .SemiBold();
            });

            column.Item().Row(row =>
            {
                row.RelativeItem()
                    .Text(string.Create(CultureInfo.InvariantCulture, $"File  {Shorten(backup.Metadata.FileName)}"))
                    .ClampLines(1);
                row.AutoItem().Text(FormatSummary(backup));
            });

            column.Item()
                .Text(string.Create(CultureInfo.InvariantCulture, $"SHA-256  {backup.Metadata.Sha256Hex}"));

            column.Item()
                .PaddingTop(HeaderRulePaddingInMillimetres, Unit.Millimetre)
                .LineHorizontal(RuleThicknessInPoints, Unit.Point);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(FooterTopPaddingInMillimetres, Unit.Millimetre).Row(row =>
        {
            row.RelativeItem()
                .Text(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{HardCopyFormat.Prefix} format · keep every page — a missing code cannot be reconstructed from the others"))
                .FontSize(CaptionFontSize);

            row.AutoItem().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(CaptionFontSize));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void ComposeContent(
        IContainer container,
        EncodedBackup backup,
        IReadOnlyList<QrSymbol> symbols,
        PageLayout layout)
    {
        // No spacing on this column: it also surrounds the page breaks, and a gap landing at the top of a
        // fresh page would shift that page's grid down out of the height PageLayout budgeted for it.
        container.Column(column =>
        {
            int printed = 0;
            for (int pageNumber = 1; pageNumber <= layout.PageCount; pageNumber++)
            {
                if (pageNumber == 1)
                {
                    ComposeInstructions(column.Item()
                        .Height(PageLayout.InstructionsHeightInMillimetres + layout.SpacingInMillimetres, Unit.Millimetre)
                        .PaddingBottom(layout.SpacingInMillimetres, Unit.Millimetre));
                }
                else
                {
                    column.Item().PageBreak();
                }

                int count = layout.GetCodeCountOnPage(pageNumber);
                if (count > 0)
                {
                    int offset = printed;
                    ComposeGrid(column.Item(), backup, symbols, offset, count, layout);
                    printed += count;
                }
            }
        });
    }

    private static void ComposeInstructions(IContainer container)
    {
        container
            .Border(InstructionsBorderInPoints, Unit.Point)
            .Padding(InstructionsPaddingInMillimetres, Unit.Millimetre)
            .Column(column =>
            {
                column.Spacing(ParagraphSpacingInMillimetres, Unit.Millimetre);
                column.Item().Text(RecoveryInstructions.Title).FontSize(InstructionsTitleFontSize).SemiBold();
                foreach (string paragraph in RecoveryInstructions.Paragraphs)
                {
                    column.Item().Text(paragraph).FontSize(InstructionsFontSize);
                }
            });
    }

    private static void ComposeGrid(
        IContainer container,
        EncodedBackup backup,
        IReadOnlyList<QrSymbol> symbols,
        int offset,
        int count,
        PageLayout layout)
    {
        container.Column(rows =>
        {
            rows.Spacing(layout.SpacingInMillimetres, Unit.Millimetre);
            for (int placed = 0; placed < count; placed += layout.Columns)
            {
                int rowStart = offset + placed;
                int rowLength = Math.Min(layout.Columns, count - placed);
                rows.Item().Height(layout.CellHeightInMillimetres, Unit.Millimetre).Row(row =>
                {
                    row.Spacing(layout.SpacingInMillimetres, Unit.Millimetre);
                    for (int slot = 0; slot < rowLength; slot++)
                    {
                        int index = rowStart + slot;
                        ComposeCell(
                            row.ConstantItem(layout.CellSizeInMillimetres, Unit.Millimetre),
                            symbols[index],
                            GetCaption(index, backup.Codes.Count),
                            layout);
                    }
                });
            }
        });
    }

    private static void ComposeCell(IContainer container, QrSymbol symbol, string caption, PageLayout layout)
    {
        container.Column(cell =>
        {
            // UseOriginalImage is not optional: without it QuestPDF re-encodes the bitmap as a lossy JPEG,
            // and a QR symbol whose module edges have been smeared by DCT ringing is a symbol that may not
            // scan. The PNG goes into the document exactly as QRCoder produced it.
            cell.Item()
                .Height(layout.CellSizeInMillimetres, Unit.Millimetre)
                .Image(symbol.Png)
                .FitArea()
                .UseOriginalImage(true);

            cell.Item()
                .Height(PageLayout.CaptionHeightInMillimetres, Unit.Millimetre)
                .AlignCenter()
                .AlignMiddle()
                .Text(caption)
                .FontSize(CaptionFontSize);
        });
    }

    /// <summary>
    /// The caption printed under a code: <c>META</c> for the metadata block, otherwise the chunk's position.
    /// </summary>
    private static string GetCaption(int index, int codeCount)
        => index == HardCopyFormat.MetadataIndex
            ? "META"
            : string.Create(CultureInfo.InvariantCulture, $"{index} / {codeCount - 1}");

    private static string FormatSummary(EncodedBackup backup)
    {
        BackupMetadata metadata = backup.Metadata;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{metadata.OriginalSizeInBytes:N0} bytes · {metadata.ChunkSizeInBytes} B chunks · {backup.Codes.Count - 1} data codes · {(metadata.IsCompressed ? "gzip" : "stored")} · {metadata.CreatedOn:yyyy-MM-dd}");
    }

    private static string Shorten(string fileName)
        => fileName.Length <= MaxPrintedFileNameLength
            ? fileName
            : string.Concat(fileName.AsSpan(0, MaxPrintedFileNameLength), EllipsisSuffix);
}
