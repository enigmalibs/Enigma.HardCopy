using System.Collections.Generic;
using System.Globalization;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The recovery instructions printed on page 1 of every backup — how to get the file back with ordinary
/// tools, if this application is not available.
/// </summary>
/// <remarks>
/// <para>
/// This text is the whole point of printing to paper rather than to a proprietary container: paper outlives
/// software, so the sheet has to explain itself. It describes the format completely enough that the file can
/// be reassembled with a Base32 decoder, a CRC-32 routine and <c>gunzip</c> — no part of it depends on this
/// codebase still existing.
/// </para>
/// <para>
/// It lives here, rather than inside <see cref="PdfComposer"/>, so that it can be asserted against the
/// format constants it describes and reused verbatim in the project's own documentation.
/// </para>
/// </remarks>
public static class RecoveryInstructions
{
    /// <summary>Gets the heading printed above the instructions.</summary>
    public static string Title => "Recovering this backup without Enigma.HardCopy";

    /// <summary>
    /// Gets the shape of a code, with each field named — for example
    /// <c>EHC1:&lt;BID&gt;:&lt;IDX&gt;/&lt;TOT&gt;:&lt;CRC&gt;:&lt;PAYLOAD&gt;</c>.
    /// </summary>
    public static string CodeShape => string.Create(
        CultureInfo.InvariantCulture,
        $"{HardCopyFormat.Prefix}{HardCopyFormat.FieldSeparator}<BID>{HardCopyFormat.FieldSeparator}<IDX>{HardCopyFormat.IndexSeparator}<TOT>{HardCopyFormat.FieldSeparator}<CRC>{HardCopyFormat.FieldSeparator}<PAYLOAD>");

    /// <summary>
    /// Gets the instruction paragraphs, in printing order. They are paragraphs rather than fixed lines
    /// because the renderer wraps them to the width of the printed box.
    /// </summary>
    public static IReadOnlyList<string> Paragraphs { get; } = BuildParagraphs();

    private static string[] BuildParagraphs()
    {
        string metadataIndex = HardCopyFormat.MetadataIndex.ToString(CultureInfo.InvariantCulture);

        return
        [
            string.Create(
                CultureInfo.InvariantCulture,
                $"Every code on these pages is one line of plain ASCII text, laid out as {CodeShape}. {HardCopyFormat.Prefix} is the format magic and version. BID is a backup ID of four characters shared by every code of this backup — ignore any code carrying a different one. IDX {metadataIndex} is the metadata code, captioned META; IDX 1 to TOT are the data chunks, and TOT counts data chunks only. CRC is the CRC-32/ISO-HDLC checksum of this code's decoded bytes — the same checksum gzip uses — as eight upper-case hexadecimal digits. PAYLOAD is unpadded RFC 4648 Base32, written with the letters A to Z and the digits 2 to 7."),

            "To recover the file, read every code, upper-case each PAYLOAD and strip its whitespace, Base32-decode it, check the decoded bytes against that code's CRC, then concatenate the data chunks in IDX order.",

            "The metadata code decodes to UTF-8 text holding key=value pairs separated by \"|\": v is the format version, n the file name, s the size of the original file in bytes, h its SHA-256 as lower-case hexadecimal, c is 1 when the joined chunks form a gzip stream (RFC 1952), e is the encryption flag and is always 0 in version 1, z the chunk size in bytes, and d the date the backup was made. Ignore any key you do not recognise.",

            "Inside a metadata value only, \"%\" is written %25 and \"|\" is written %7C. Undo both escapes after splitting the text on \"|\" and each pair on its first \"=\".",

            "Finally run gunzip if c is 1, then compare the SHA-256 of the result against h. Matching hashes mean the file came back intact. Keep every page: a missing chunk cannot be reconstructed from the others.",
        ];
    }
}
