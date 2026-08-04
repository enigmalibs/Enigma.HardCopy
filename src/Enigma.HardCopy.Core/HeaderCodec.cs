using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Formats and parses whole barcode strings —
/// <c>EHC1:&lt;BID&gt;:&lt;IDX&gt;/&lt;TOT&gt;:&lt;CRC&gt;:&lt;PAYLOAD&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the single gate every code passes through, in either direction. Formatting validates its
/// input and throws, because producing a malformed code would print unrecoverable paper; parsing never
/// throws, because a bad scan or a typo is an expected outcome of a recovery, not an error.
/// </para>
/// <para>
/// Parsing normalizes first — whitespace stripped, case raised — so a code typed by hand across several
/// lines, or in lower case, is accepted. Every field of the format is upper-case or numeric, and no field
/// contains whitespace, so neither step can lose information.
/// </para>
/// </remarks>
public static class HeaderCodec
{
    private const int FieldCount = 5;

    /// <summary>Formats <paramref name="header"/> as the leading fields of a code, without the payload.</summary>
    /// <param name="header">The header to format.</param>
    /// <returns>For example <c>EHC1:K7QA:3/17:1A2B3C4D</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="header"/> is not a valid header.</exception>
    public static string FormatHeader(CodeHeader header)
    {
        Validate(header);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{HardCopyFormat.Prefix}{HardCopyFormat.FieldSeparator}{header.BackupId}{HardCopyFormat.FieldSeparator}{header.Index}{HardCopyFormat.IndexSeparator}{header.Total}{HardCopyFormat.FieldSeparator}{header.Crc:X8}");
    }

    /// <summary>Formats a complete code from its header and Base32 payload.</summary>
    /// <param name="header">The header of the code.</param>
    /// <param name="payload">The <see cref="Base32"/> payload, which must not be empty.</param>
    /// <returns>The code string, entirely within <see cref="HardCopyFormat.AlphanumericCharset"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="header"/> is not a valid header, or <paramref name="payload"/> is empty or holds a
    /// character outside <see cref="Base32.Alphabet"/>.
    /// </exception>
    public static string FormatCode(CodeHeader header, string payload)
    {
        ArgumentException.ThrowIfNullOrEmpty(payload);
        if (!IsBase32(payload))
        {
            throw new ArgumentException("The payload is not Base32 text.", nameof(payload));
        }

        return FormatHeader(header) + HardCopyFormat.FieldSeparator + payload;
    }

    /// <summary>
    /// Attempts to parse a complete code, normalizing whitespace and case first. Returns
    /// <see langword="false"/> rather than throwing for anything malformed.
    /// </summary>
    /// <param name="code">The code text, as scanned or typed. May be <see langword="null"/>.</param>
    /// <param name="header">
    /// When this method returns <see langword="true"/>, the parsed header; otherwise the default value.
    /// </param>
    /// <param name="payload">
    /// When this method returns <see langword="true"/>, the normalized <see cref="Base32"/> payload
    /// (not yet decoded, and not yet checked against <see cref="CodeHeader.Crc"/>); otherwise
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a structurally valid code.</returns>
    public static bool TryParseCode(string? code, out CodeHeader header, [NotNullWhen(true)] out string? payload)
    {
        header = default;
        payload = null;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        string[] fields = Normalize(code).Split(HardCopyFormat.FieldSeparator);
        if (fields.Length != FieldCount)
        {
            return false;
        }

        if (!string.Equals(fields[0], HardCopyFormat.Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsBackupId(fields[1]))
        {
            return false;
        }

        string[] counters = fields[2].Split(HardCopyFormat.IndexSeparator);
        if (counters.Length != 2
            || !TryParseCounter(counters[0], out int index)
            || !TryParseCounter(counters[1], out int total)
            || index > total)
        {
            // Index 0 is the metadata block and 1..total the data chunks, so an index above the total can
            // only mean the code was damaged — there is no code it could legitimately be.
            return false;
        }

        if (fields[3].Length != HardCopyFormat.CrcHexLength
            || !uint.TryParse(fields[3], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint crc))
        {
            return false;
        }

        if (fields[4].Length == 0 || !IsBase32(fields[4]))
        {
            return false;
        }

        header = new CodeHeader(fields[1], index, total, crc);
        payload = fields[4];
        return true;
    }

    /// <summary>Strips all whitespace from <paramref name="code"/> and raises it to upper case.</summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    public static string Normalize(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        StringBuilder normalized = new(code.Length);
        foreach (char character in code)
        {
            if (!char.IsWhiteSpace(character))
            {
                normalized.Append(char.ToUpperInvariant(character));
            }
        }

        return normalized.ToString();
    }

    private static void Validate(CodeHeader header)
    {
        if (!IsBackupId(header.BackupId))
        {
            throw new ArgumentException(
                $"The backup ID must be {HardCopyFormat.BackupIdLength} Base32 characters.", nameof(header));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(header.Index, nameof(header));
        ArgumentOutOfRangeException.ThrowIfNegative(header.Total, nameof(header));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(header.Index, header.Total, nameof(header));
    }

    private static bool IsBackupId([NotNullWhen(true)] string? value)
        => value is { Length: HardCopyFormat.BackupIdLength } && IsBase32(value);

    private static bool IsBase32(string value)
    {
        foreach (char character in value)
        {
            if (!Base32.Alphabet.Contains(character, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseCounter(string value, out int result)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
}
