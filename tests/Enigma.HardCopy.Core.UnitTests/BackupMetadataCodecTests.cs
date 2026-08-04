using System;
using System.Linq;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class BackupMetadataCodecTests
{
    private const string Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private static readonly string _validText =
        $"v=1|n=keys.kdbx|s=4096|h={Hash}|c=1|e=0|z=1024|d=2026-08-04";

    private static BackupMetadata Sample => new()
    {
        FileName = "keys.kdbx",
        OriginalSizeInBytes = 4096,
        Sha256Hex = Hash,
        IsCompressed = true,
        ChunkSizeInBytes = 1024,
        CreatedOn = new DateOnly(2026, 8, 4),
    };

    // Golden string: this text is printed verbatim in the PDF's recovery-spec box, so its layout is pinned.
    [Fact]
    public void Format_ProducesTheSpecifiedLayout() => Assert.Equal(_validText, BackupMetadataCodec.Format(Sample));

    [Fact]
    public void Format_WritesTheReservedEncryptionFlagAsZero()
        => Assert.Contains("|e=0|", BackupMetadataCodec.Format(Sample), StringComparison.Ordinal);

    [Fact]
    public void Format_WritesTheCompressionFlagAsZeroWhenUncompressed()
        => Assert.Contains("|c=0|", BackupMetadataCodec.Format(Sample with { IsCompressed = false }), StringComparison.Ordinal);

    [Fact]
    public void Format_EscapesTheSeparatorAndTheEscapeCharacter()
        => Assert.Contains(
            "|n=a%7Cb%25c.key|",
            BackupMetadataCodec.Format(Sample with { FileName = "a|b%c.key" }),
            StringComparison.Ordinal);

    [Fact]
    public void Format_Null_Throws() => Assert.Throws<ArgumentNullException>(() => BackupMetadataCodec.Format(null!));

    [Fact]
    public void TryParse_ReadsEveryFieldOfTheGoldenText()
    {
        BackupMetadata metadata = Parse(_validText);

        Assert.Equal(1, metadata.FormatVersion);
        Assert.Equal("keys.kdbx", metadata.FileName);
        Assert.Equal(4096, metadata.OriginalSizeInBytes);
        Assert.Equal(Hash, metadata.Sha256Hex);
        Assert.True(metadata.IsCompressed);
        Assert.False(metadata.IsEncrypted);
        Assert.Equal(1024, metadata.ChunkSizeInBytes);
        Assert.Equal(new DateOnly(2026, 8, 4), metadata.CreatedOn);
    }

    [Fact]
    public void TryParse_ReadsBackWhatFormatWrote()
        => Assert.Equal(Sample, Parse(BackupMetadataCodec.Format(Sample)));

    // A file name is user data: it may contain the pair separator, the escape character, non-ASCII text or
    // characters outside the basic multilingual plane, and must come back byte for byte.
    [Theory]
    [InlineData("keys.kdbx")]
    [InlineData("my|weird|name.key")]
    [InlineData("100%|pure.txt")]
    [InlineData("%%%|||%%%")]
    [InlineData("a=b=c.txt")]
    [InlineData("clé privée — sauvegarde.pem")]
    [InlineData("файл-ключа.txt")]
    [InlineData("emoji 🔐 backup.key")]
    [InlineData("  spaces  everywhere  .txt")]
    [InlineData("no-extension")]
    [InlineData(".hidden")]
    public void TryParse_RoundTripsExoticFileNames(string fileName)
    {
        BackupMetadata original = Sample with { FileName = fileName };

        BackupMetadata parsed = Parse(BackupMetadataCodec.Format(original));

        Assert.Equal(original, parsed);
        Assert.Equal(fileName, parsed.FileName);
    }

    [Fact]
    public void TryParse_IgnoresUnknownKeys()
        => Assert.Equal(Sample, Parse($"{_validText}|x=1|future=whatever"));

    [Fact]
    public void TryParse_AcceptsAFutureFormatVersion() => Assert.Equal(2, Parse(With("v", "2")).FormatVersion);

    [Fact]
    public void TryParse_ReadsTheReservedEncryptionFlag() => Assert.True(Parse(With("e", "1")).IsEncrypted);

    [Theory]
    [InlineData("v")]
    [InlineData("n")]
    [InlineData("s")]
    [InlineData("h")]
    [InlineData("c")]
    [InlineData("e")]
    [InlineData("z")]
    [InlineData("d")]
    public void TryParse_MissingRequiredKey_ReturnsFalse(string key)
        => Assert.False(BackupMetadataCodec.TryParse(Without(key), out _));

    [Fact]
    public void TryParse_DuplicateKey_ReturnsFalse()
        => Assert.False(BackupMetadataCodec.TryParse($"{_validText}|n=other.key", out _));

    [Theory]
    [InlineData("v", "x")]
    [InlineData("v", "")]
    [InlineData("v", "1.0")]
    [InlineData("n", "")]
    [InlineData("s", "-1")]
    [InlineData("s", "4096.5")]
    [InlineData("s", "lots")]
    [InlineData("h", "abc")]
    [InlineData("h", "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("c", "2")]
    [InlineData("c", "true")]
    [InlineData("e", "x")]
    [InlineData("z", "0")]
    [InlineData("z", "-1024")]
    [InlineData("d", "2026-8-4")]
    [InlineData("d", "04-08-2026")]
    [InlineData("d", "2026-13-01")]
    public void TryParse_InvalidValue_ReturnsFalse(string key, string value)
        => Assert.False(BackupMetadataCodec.TryParse(With(key, value), out _));

    [Theory]
    [InlineData("a%7")]     // escape truncated
    [InlineData("a%")]      // escape at the very end
    [InlineData("a%ZZ")]    // not hexadecimal
    [InlineData("a%C3%A9")] // a UTF-8 sequence: this codec only ever escapes ASCII
    public void TryParse_MalformedEscape_ReturnsFalse(string fileName)
        => Assert.False(BackupMetadataCodec.TryParse(With("n", fileName), out _));

    [Fact]
    public void TryParse_UpperCaseHash_IsNormalizedToLowerCase()
        => Assert.Equal(Hash, Parse(With("h", Hash.ToUpperInvariant())).Sha256Hex);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a metadata block")]
    [InlineData("v=1|n")]                 // a pair with no separator
    [InlineData("=1|n=x")]                // a pair with no key
    public void TryParse_Malformed_ReturnsFalse(string? text)
        => Assert.False(BackupMetadataCodec.TryParse(text, out _));

    [Fact]
    public void TryParse_TrailingSeparator_ReturnsFalse()
        => Assert.False(BackupMetadataCodec.TryParse($"{_validText}|", out _));

    /// <summary>Parses text the test expects to be valid, asserting success so the result is non-null.</summary>
    private static BackupMetadata Parse(string text)
    {
        Assert.True(BackupMetadataCodec.TryParse(text, out BackupMetadata? metadata), $"'{text}' did not parse.");

        return metadata!;
    }

    private static string Without(string key) => string.Join(
        '|',
        _validText.Split('|').Where(pair => !pair.StartsWith($"{key}=", StringComparison.Ordinal)));

    private static string With(string key, string value) => string.Join(
        '|',
        _validText.Split('|').Select(pair => pair.StartsWith($"{key}=", StringComparison.Ordinal) ? $"{key}={value}" : pair));
}
