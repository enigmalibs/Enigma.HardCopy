using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Serializes and parses the metadata block's payload text — <c>|</c>-separated <c>key=value</c> pairs.
/// </summary>
/// <remarks>
/// <para>
/// The text is deliberately plain rather than JSON or binary: it is printed, in full, in the recovery-spec
/// box on page 1 of the PDF, so someone recovering the file without this application can read it.
/// </para>
/// <para>
/// <b>Escaping.</b> A file name may legally contain the <c>|</c> separator, so inside a <b>value</b>
/// <c>%</c> is written <c>%25</c> and <c>|</c> is written <c>%7C</c>; parsing accepts any <c>%XX</c> whose
/// byte is ASCII and rejects a truncated or non-hexadecimal escape. Keys never need escaping, and <c>=</c>
/// is never escaped because a pair splits on its <b>first</b> <c>=</c> only.
/// </para>
/// <para>
/// <b>Forward compatibility.</b> Unknown keys are ignored, so a later format version may add fields without
/// breaking this decoder. Every key version 1 defines must be present, and any duplicated key is rejected —
/// within one version, a missing or repeated field means damage, not evolution.
/// </para>
/// </remarks>
public static class BackupMetadataCodec
{
    private const char PairSeparator = '|';
    private const char KeyValueSeparator = '=';
    private const char EscapePrefix = '%';

    private const string VersionKey = "v";
    private const string FileNameKey = "n";
    private const string SizeKey = "s";
    private const string HashKey = "h";
    private const string CompressedKey = "c";
    private const string EncryptedKey = "e";
    private const string ChunkSizeKey = "z";
    private const string DateKey = "d";

    private const string DateFormat = "yyyy-MM-dd";
    private const int Sha256HexLength = 64;

    /// <summary>Serializes <paramref name="metadata"/> to the metadata block's payload text.</summary>
    /// <param name="metadata">The metadata to serialize.</param>
    /// <returns>
    /// For example
    /// <c>v=1|n=keys.kdbx|s=4096|h=9f86d0…|c=1|e=0|z=1024|d=2026-08-04</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
    public static string Format(BackupMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        StringBuilder text = new();
        Append(text, VersionKey, metadata.FormatVersion.ToString(CultureInfo.InvariantCulture));
        Append(text, FileNameKey, metadata.FileName);
        Append(text, SizeKey, metadata.OriginalSizeInBytes.ToString(CultureInfo.InvariantCulture));
        Append(text, HashKey, metadata.Sha256Hex);
        Append(text, CompressedKey, metadata.IsCompressed ? "1" : "0");
        Append(text, EncryptedKey, metadata.IsEncrypted ? "1" : "0");
        Append(text, ChunkSizeKey, metadata.ChunkSizeInBytes.ToString(CultureInfo.InvariantCulture));
        Append(text, DateKey, metadata.CreatedOn.ToString(DateFormat, CultureInfo.InvariantCulture));

        return text.ToString();
    }

    /// <summary>
    /// Attempts to parse the metadata block's payload text. Returns <see langword="false"/> rather than
    /// throwing for anything malformed — a damaged metadata block is an expected recovery outcome.
    /// </summary>
    /// <param name="text">The payload text. May be <see langword="null"/>.</param>
    /// <param name="metadata">
    /// When this method returns <see langword="true"/>, the parsed metadata; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="text"/> is a valid metadata block.</returns>
    public static bool TryParse(string? text, [NotNullWhen(true)] out BackupMetadata? metadata)
    {
        metadata = null;
        if (string.IsNullOrEmpty(text) || !TryReadPairs(text, out Dictionary<string, string>? pairs))
        {
            return false;
        }

        if (!TryGetInt32(pairs, VersionKey, out int version)
            || !TryGetString(pairs, FileNameKey, out string? fileName)
            || !TryGetInt64(pairs, SizeKey, out long size)
            || !TryGetHash(pairs, out string? hash)
            || !TryGetBoolean(pairs, CompressedKey, out bool isCompressed)
            || !TryGetBoolean(pairs, EncryptedKey, out bool isEncrypted)
            || !TryGetInt32(pairs, ChunkSizeKey, out int chunkSize)
            || chunkSize <= 0
            || !TryGetDate(pairs, out DateOnly createdOn))
        {
            return false;
        }

        metadata = new BackupMetadata
        {
            FormatVersion = version,
            FileName = fileName,
            OriginalSizeInBytes = size,
            Sha256Hex = hash,
            IsCompressed = isCompressed,
            IsEncrypted = isEncrypted,
            ChunkSizeInBytes = chunkSize,
            CreatedOn = createdOn,
        };
        return true;
    }

    private static void Append(StringBuilder text, string key, string value)
    {
        if (text.Length > 0)
        {
            text.Append(PairSeparator);
        }

        text.Append(key).Append(KeyValueSeparator).Append(Escape(value));
    }

    private static string Escape(string value)
    {
        StringBuilder escaped = new(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case EscapePrefix:
                    escaped.Append("%25");
                    break;
                case PairSeparator:
                    escaped.Append("%7C");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }

        return escaped.ToString();
    }

    private static bool TryUnescape(string value, [NotNullWhen(true)] out string? unescaped)
    {
        unescaped = null;
        StringBuilder result = new(value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != EscapePrefix)
            {
                result.Append(value[index]);
                continue;
            }

            if (index + 2 >= value.Length
                || !byte.TryParse(
                    value.AsSpan(index + 1, 2),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out byte escaped)
                || escaped >= 0x80)
            {
                // Only ASCII is ever escaped, so a byte above 0x7F would be a fragment of a UTF-8 sequence
                // this codec never produces — the value has been damaged.
                return false;
            }

            result.Append((char)escaped);
            index += 2;
        }

        unescaped = result.ToString();
        return true;
    }

    private static bool TryReadPairs(string text, [NotNullWhen(true)] out Dictionary<string, string>? pairs)
    {
        pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in text.Split(PairSeparator))
        {
            int separator = pair.IndexOf(KeyValueSeparator, StringComparison.Ordinal);
            if (separator <= 0 || !TryUnescape(pair[(separator + 1)..], out string? value))
            {
                pairs = null;
                return false;
            }

            if (!pairs.TryAdd(pair[..separator], value))
            {
                pairs = null;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetString(
        Dictionary<string, string> pairs,
        string key,
        [NotNullWhen(true)] out string? value)
        => pairs.TryGetValue(key, out value) && value.Length > 0;

    private static bool TryGetInt32(Dictionary<string, string> pairs, string key, out int value)
    {
        value = 0;
        return pairs.TryGetValue(key, out string? text)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetInt64(Dictionary<string, string> pairs, string key, out long value)
    {
        value = 0;
        return pairs.TryGetValue(key, out string? text)
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetBoolean(Dictionary<string, string> pairs, string key, out bool value)
    {
        value = false;
        if (!pairs.TryGetValue(key, out string? text))
        {
            return false;
        }

        switch (text)
        {
            case "0":
                return true;
            case "1":
                value = true;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetHash(Dictionary<string, string> pairs, [NotNullWhen(true)] out string? hash)
    {
        hash = null;
        if (!pairs.TryGetValue(HashKey, out string? text) || text.Length != Sha256HexLength)
        {
            return false;
        }

        foreach (char character in text)
        {
            if (!char.IsAsciiHexDigit(character))
            {
                return false;
            }
        }

        hash = text.ToLowerInvariant();
        return true;
    }

    private static bool TryGetDate(Dictionary<string, string> pairs, out DateOnly date)
    {
        date = default;
        return pairs.TryGetValue(DateKey, out string? text)
            && DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
