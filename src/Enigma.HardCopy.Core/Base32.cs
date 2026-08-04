using System;
using System.Diagnostics.CodeAnalysis;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Base32 codec (RFC 4648 alphabet, <b>unpadded</b>) used for every barcode payload.
/// </summary>
/// <remarks>
/// <para>
/// Base32 is what keeps a code inside the QR alphanumeric-mode charset (see
/// <see cref="HardCopyFormat.AlphanumericCharset"/>) — its 32 symbols <c>A–Z</c> and <c>2–7</c> are all
/// members of that set — at roughly 60% expansion but with codes that a human can still type by hand.
/// Padding is omitted deliberately: <c>=</c> is not a QR alphanumeric character.
/// </para>
/// <para>
/// Decoding is <i>tolerant of transcription noise but strict about bits</i>: input is upper-cased and all
/// whitespace is stripped (a hand-typed code may arrive lower-case or wrapped across lines), yet a symbol
/// count that no byte length can produce, and trailing bits that are not zero, are both rejected. Those
/// two checks catch a mistyped or truncated final character that the CRC would otherwise have to.
/// </para>
/// </remarks>
public static class Base32
{
    /// <summary>The RFC 4648 Base32 alphabet, indexed by 5-bit symbol value.</summary>
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private const int BitsPerSymbol = 5;
    private const int BitsPerByte = 8;

    /// <summary>Encodes <paramref name="bytes"/> as unpadded Base32.</summary>
    /// <param name="bytes">The bytes to encode. May be empty.</param>
    /// <returns>The Base32 text, or an empty string when <paramref name="bytes"/> is empty.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        char[] symbols = new char[GetSymbolCount(bytes.Length)];
        int bitBuffer = 0;
        int bitCount = 0;
        int written = 0;

        foreach (byte value in bytes)
        {
            bitBuffer = (bitBuffer << BitsPerByte) | value;
            bitCount += BitsPerByte;
            while (bitCount >= BitsPerSymbol)
            {
                bitCount -= BitsPerSymbol;
                symbols[written++] = Alphabet[(bitBuffer >> bitCount) & 0x1F];
            }
        }

        if (bitCount > 0)
        {
            // Left-align the leftover bits and pad the low end with zeros — the decoder requires them to
            // be zero, which is what makes a truncated final symbol detectable.
            symbols[written++] = Alphabet[(bitBuffer << (BitsPerSymbol - bitCount)) & 0x1F];
        }

        return new string(symbols, 0, written);
    }

    /// <summary>Decodes unpadded Base32 text, normalizing case and whitespace first.</summary>
    /// <param name="text">The Base32 text to decode.</param>
    /// <returns>The decoded bytes; an empty array when <paramref name="text"/> holds no symbols.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="text"/> contains a character outside <see cref="Alphabet"/>, has a symbol count no
    /// byte length can produce, or carries non-zero trailing bits.
    /// </exception>
    public static byte[] Decode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return TryDecode(text, out byte[]? bytes)
            ? bytes
            : throw new FormatException("The text is not valid unpadded Base32.");
    }

    /// <summary>
    /// Attempts to decode unpadded Base32 text, normalizing case and whitespace first. Never throws for
    /// malformed input — a bad scan or a mistyped code is an expected outcome, not an error.
    /// </summary>
    /// <param name="text">The Base32 text to decode. May be <see langword="null"/>.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true"/>, the decoded bytes (empty when
    /// <paramref name="text"/> held no symbols); otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="text"/> was valid unpadded Base32.</returns>
    public static bool TryDecode(string? text, [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;
        if (text is null)
        {
            return false;
        }

        int symbolCount = 0;
        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (GetSymbolValue(character) < 0)
            {
                return false;
            }

            symbolCount++;
        }

        // 1, 3 and 6 leftover symbols carry 5, 15 and 30 bits — none of which is a whole number of bytes
        // plus fewer than 5 padding bits, so no byte sequence can encode to them.
        int leftover = symbolCount % 8;
        if (leftover is 1 or 3 or 6)
        {
            return false;
        }

        byte[] decoded = new byte[(symbolCount * BitsPerSymbol) / BitsPerByte];
        int bitBuffer = 0;
        int bitCount = 0;
        int written = 0;

        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            bitBuffer = (bitBuffer << BitsPerSymbol) | GetSymbolValue(character);
            bitCount += BitsPerSymbol;
            if (bitCount >= BitsPerByte)
            {
                bitCount -= BitsPerByte;
                decoded[written++] = (byte)((bitBuffer >> bitCount) & 0xFF);
            }
        }

        // Whatever bits are left over are the encoder's zero padding; anything else means the text was
        // altered after it was produced.
        if ((bitBuffer & ((1 << bitCount) - 1)) != 0)
        {
            return false;
        }

        bytes = decoded;
        return true;
    }

    /// <summary>Returns the number of Base32 symbols <paramref name="byteCount"/> bytes encode to.</summary>
    /// <param name="byteCount">The number of bytes to be encoded.</param>
    /// <returns>The unpadded Base32 symbol count.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
    public static int GetSymbolCount(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

        return ((byteCount * BitsPerByte) + (BitsPerSymbol - 1)) / BitsPerSymbol;
    }

    /// <summary>Returns the 5-bit value of a Base32 symbol, or -1 when the character is not one.</summary>
    private static int GetSymbolValue(char character) => character switch
    {
        >= 'A' and <= 'Z' => character - 'A',
        >= 'a' and <= 'z' => character - 'a',
        >= '2' and <= '7' => (character - '2') + 26,
        _ => -1,
    };
}
