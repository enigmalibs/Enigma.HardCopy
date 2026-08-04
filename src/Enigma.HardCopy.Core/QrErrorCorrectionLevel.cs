namespace Enigma.HardCopy.Core;

/// <summary>
/// The four QR error-correction levels, in increasing order of redundancy.
/// </summary>
/// <remarks>
/// <para>
/// This enum exists so that the barcode library's own error-correction type never reaches the public
/// surface. Which library draws the symbols is an implementation detail of <see cref="QrRenderer"/>, and
/// replacing it must not be a breaking change for the desktop app or a future CLI.
/// </para>
/// <para>
/// Redundancy is not free: it is paid for in symbol size, and a larger symbol means fewer codes on a printed
/// page. One default-sized code (a 1024-byte chunk, about 1663 characters) needs QR version 24 at
/// <see cref="Low"/>, 28 at <see cref="Medium"/>, 33 at <see cref="Quartile"/> and 39 at <see cref="High"/> —
/// so the choice moves the symbol from 105 to 165 modules across the page.
/// </para>
/// <para>
/// <see cref="Medium"/> is the validated default for this application, and the level the page-layout capacity
/// budget is computed against: its ~15% recovery capacity comfortably covers the damage a sheet of paper
/// realistically suffers — a fold, a coffee ring, a scanner smudge — without spending two thirds of the
/// symbol on redundancy.
/// </para>
/// </remarks>
public enum QrErrorCorrectionLevel
{
    /// <summary>
    /// Level L — about 7% of the codewords recoverable. The smallest symbol for a given payload, and the
    /// least tolerance for a damaged or poorly scanned print.
    /// </summary>
    Low,

    /// <summary>
    /// Level M — about 15% of the codewords recoverable. The validated default for this application; see the
    /// remarks on <see cref="QrErrorCorrectionLevel"/> for why.
    /// </summary>
    Medium,

    /// <summary>
    /// Level Q — about 25% of the codewords recoverable. Worth the extra pages only for a print that will be
    /// handled a great deal.
    /// </summary>
    Quartile,

    /// <summary>
    /// Level H — about 30% of the codewords recoverable, and the largest symbol. At this level the largest
    /// permitted chunk no longer fits a single symbol, so it is not a level the encoder can assume.
    /// </summary>
    High,
}
