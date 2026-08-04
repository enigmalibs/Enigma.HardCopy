using System;
using System.Text;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class Base32Tests
{
    [Fact]
    public void Alphabet_IsTheRfc4648Alphabet()
        => Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", Base32.Alphabet);

    [Fact]
    public void Encode_Empty_ReturnsEmptyString() => Assert.Equal(string.Empty, Base32.Encode([]));

    // The RFC 4648 section 10 test vectors, with the padding the format deliberately omits stripped off.
    [Theory]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Encode_MatchesRfc4648Vectors(string text, string expected)
        => Assert.Equal(expected, Base32.Encode(Encoding.ASCII.GetBytes(text)));

    [Theory]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Decode_MatchesRfc4648Vectors(string expected, string encoded)
        => Assert.Equal(expected, Encoding.ASCII.GetString(Base32.Decode(encoded)));

    [Fact]
    public void Encode_ProducesOnlyAlphabetCharacters()
    {
        string encoded = Base32.Encode(RandomBytes(seed: 7, length: 1024));

        Assert.All(encoded, character => Assert.True(Base32.Alphabet.Contains(character, StringComparison.Ordinal)));
    }

    // Property-style: every buffer length in the region where the 5-byte/8-symbol groups do not line up,
    // plus a few realistic chunk sizes, must survive a round trip byte for byte.
    [Fact]
    public void RoundTrip_PreservesArbitraryBuffers()
    {
        foreach (int length in RoundTripLengths())
        {
            byte[] original = RandomBytes(seed: length, length);

            byte[] decoded = Base32.Decode(Base32.Encode(original));

            Assert.Equal(original, decoded);
        }
    }

    [Fact]
    public void RoundTrip_PreservesAllByteValues()
    {
        byte[] original = new byte[256];
        for (int value = 0; value < original.Length; value++)
        {
            original[value] = (byte)value;
        }

        Assert.Equal(original, Base32.Decode(Base32.Encode(original)));
    }

    [Fact]
    public void Decode_Empty_ReturnsEmptyArray() => Assert.Empty(Base32.Decode(string.Empty));

    [Theory]
    [InlineData("mzxw6ytboi")]
    [InlineData("MZXW6YTB OI")]
    [InlineData("MZXW6\r\nYTBOI")]
    [InlineData("  mzxw6ytb\toi  ")]
    public void Decode_NormalizesCaseAndWhitespace(string encoded)
        => Assert.Equal("foobar", Encoding.ASCII.GetString(Base32.Decode(encoded)));

    [Theory]
    [InlineData("MZXW6YTB!")]      // punctuation is not a symbol
    [InlineData("MZXW0YTB")]       // 0, 1, 8 and 9 are excluded from the alphabet
    [InlineData("MZXW1YTB")]
    [InlineData("MZXW8YTB")]
    [InlineData("MZXW6YTB=")]      // padding is never accepted: '=' is not a QR alphanumeric character
    public void TryDecode_UnknownCharacter_ReturnsFalse(string encoded)
        => Assert.False(Base32.TryDecode(encoded, out _));

    // 1, 3 and 6 symbols past a group boundary cannot be produced by any byte sequence, so a code that
    // ends on one has lost characters.
    [Theory]
    [InlineData("M")]
    [InlineData("MZX")]
    [InlineData("MZXW6Y")]
    [InlineData("MZXW6YTBM")]
    [InlineData("MZXW6YTBMZX")]
    public void TryDecode_ImpossibleSymbolCount_ReturnsFalse(string encoded)
        => Assert.False(Base32.TryDecode(encoded, out _));

    // "MY" and "MZ" both decode to 'f'; only "MY" leaves the trailing bits zero, so "MZ" is a corruption
    // of the final character that the codec catches without help from the CRC.
    [Fact]
    public void TryDecode_NonZeroTrailingBits_ReturnsFalse()
    {
        Assert.Equal("f", Encoding.ASCII.GetString(Base32.Decode("MY")));

        Assert.False(Base32.TryDecode("MZ", out _));
    }

    [Fact]
    public void TryDecode_Null_ReturnsFalse() => Assert.False(Base32.TryDecode(null, out _));

    [Fact]
    public void Decode_Null_Throws() => Assert.Throws<ArgumentNullException>(() => Base32.Decode(null!));

    [Fact]
    public void Decode_Invalid_ThrowsFormatException()
        => Assert.Throws<FormatException>(() => Base32.Decode("MZXW6YTB!"));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    [InlineData(4, 7)]
    [InlineData(5, 8)]
    [InlineData(6, 10)]
    [InlineData(512, 820)]
    [InlineData(1024, 1639)]
    [InlineData(1536, 2458)]
    public void GetSymbolCount_MatchesEncodedLength(int byteCount, int expected)
    {
        Assert.Equal(expected, Base32.GetSymbolCount(byteCount));
        Assert.Equal(expected, Base32.Encode(new byte[byteCount]).Length);
    }

    [Fact]
    public void GetSymbolCount_Negative_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Base32.GetSymbolCount(-1));

    private static byte[] RandomBytes(int seed, int length)
    {
        byte[] bytes = new byte[length];
        new Random(seed).NextBytes(bytes);

        return bytes;
    }

    private static int[] RoundTripLengths()
    {
        int[] lengths = new int[45];
        for (int index = 0; index < 41; index++)
        {
            lengths[index] = index;
        }

        lengths[41] = 511;
        lengths[42] = 512;
        lengths[43] = 1024;
        lengths[44] = 1536;

        return lengths;
    }
}
