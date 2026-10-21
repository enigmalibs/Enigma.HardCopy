using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// The camera side of the decoding seam: a live frame's luminance goes in, the codes in view come out.
/// </summary>
public sealed class ImageDecoderFrameTests
{
    [Fact]
    public async Task ReadCodes_ASymbolInsideALargerPaddedFrame_ReadsIt()
    {
        EncodedBackup backup = await SmallBackupAsync();

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.ToLuminanceFrame(
            RecoveryFixtures.RenderSymbol(backup.Codes[1]),
            border: 120,
            rowPadding: 64));

        Assert.Equal([backup.Codes[1]], codes);
    }

    // A camera plane's buffer commonly ends at the last pixel of the last row rather than at its stride; a
    // reader that insisted on a full final stride would reject every such frame.
    [Fact]
    public async Task ReadCodes_AFrameWhoseLastRowIsUnpadded_ReadsIt()
    {
        EncodedBackup backup = await SmallBackupAsync();

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.ToLuminanceFrame(
            RecoveryFixtures.RenderSymbol(backup.Codes[0]),
            trimLastRow: true));

        Assert.Equal([backup.Codes[0]], codes);
    }

    [Fact]
    public async Task ReadCodes_SeveralSymbolsInView_ReadsEveryOne()
    {
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(
            RecoveryFixtures.Incompressible(1_500),
            options: EncodeOptions.FromPreset(ChunkSizePreset.Small));
        Assert.True(backup.Codes.Count >= 4);

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.ToLuminanceFrame(
            RecoveryFixtures.RenderPage(backup.Codes.Take(4).ToList(), columns: 2)));

        Assert.Equal(4, codes.Count);
        Assert.All(backup.Codes.Take(4), code => Assert.Contains(code, codes));
    }

    // The default density is what a phone is pointed at most of the time, and its symbols are dense — well
    // past version 20 — at three pixels a module, roughly what a phone held over one code resolves.
    [Fact]
    public async Task ReadCodes_ADefaultDensitySymbol_ReadsIt()
    {
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(RecoveryFixtures.Incompressible(3_000));
        Assert.True(QrRenderer.GetVersion(backup.Codes[1]) > 20, "the fixture is not a dense symbol");

        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.ToLuminanceFrame(
            RecoveryFixtures.RenderSymbol(backup.Codes[1], pixelsPerModule: 3)));

        Assert.Equal([backup.Codes[1]], codes);
    }

    [Fact]
    public void ReadCodes_ABlankFrame_FindsNothing()
    {
        IReadOnlyList<string> codes = ReadCodes(RecoveryFixtures.ToLuminanceFrame(RecoveryFixtures.BlankImage()));

        Assert.Empty(codes);
    }

    [Fact]
    public async Task ReadCodes_FramesFedToASession_RecoverTheFile()
    {
        byte[] original = RecoveryFixtures.Incompressible(700);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(
            original,
            options: EncodeOptions.FromPreset(ChunkSizePreset.Small));
        RecoverySession session = new();

        foreach (string printed in RecoveryFixtures.Shuffled(backup.Codes))
        {
            foreach (string read in ReadCodes(RecoveryFixtures.ToLuminanceFrame(RecoveryFixtures.RenderSymbol(printed))))
            {
                Assert.True(session.AddCode(read).IsAccepted);
            }
        }

        AssemblyResult result = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, result.Outcome);
        Assert.Equal(original, result.Content);
    }

    [Fact]
    public void ReadCodes_NullBuffer_Throws()
        => Assert.Throws<ArgumentNullException>(() => ImageDecoder.ReadCodes(null!, 10, 10, 10));

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(-1, 10, 10)]
    [InlineData(10, 0, 10)]
    [InlineData(10, -1, 10)]
    [InlineData(10, 10, 9)]
    public void ReadCodes_ImpossibleDimensions_Throw(int width, int height, int rowStride)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => ImageDecoder.ReadCodes(new byte[1_000], width, height, rowStride));

    [Theory]
    [InlineData(10, 10, 10, 99)]
    [InlineData(10, 10, 16, 153)]
    public void ReadCodes_ABufferShorterThanTheFrame_Throws(int width, int height, int rowStride, int length)
    {
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => ImageDecoder.ReadCodes(new byte[length], width, height, rowStride));

        Assert.Equal("luminance", thrown.ParamName);
    }

    [Fact]
    public void ReadCodes_ABufferOfExactlyTheMinimumLength_IsAccepted()
        => Assert.Empty(ImageDecoder.ReadCodes(FilledWhite((16 * 9) + 10), 10, 10, 16));

    /// <summary>A backup of two codes — the metadata block and one small data chunk.</summary>
    private static Task<EncodedBackup> SmallBackupAsync()
        => RecoveryFixtures.EncodeAsync(
            RecoveryFixtures.Incompressible(300),
            options: EncodeOptions.FromPreset(ChunkSizePreset.Small));

    private static IReadOnlyList<string> ReadCodes(LuminanceFrame frame)
        => ImageDecoder.ReadCodes(frame.Pixels, frame.Width, frame.Height, frame.RowStride);

    private static byte[] FilledWhite(int length)
    {
        byte[] pixels = new byte[length];
        Array.Fill(pixels, (byte)0xFF);

        return pixels;
    }
}
