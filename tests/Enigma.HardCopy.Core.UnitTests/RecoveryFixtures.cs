using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using SkiaSharp;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// The fixtures the recovery suites share: real backups from the encoder, fabricated backups whose metadata
/// block says something untrue, damaged codes, and images of printed pages.
/// </summary>
/// <remarks>
/// The fabricated backups are the interesting half. A recovery has to cope with codes it did not produce —
/// a metadata block recording the wrong hash, or claiming a gzip stream that is not one — and those cannot
/// be obtained from <see cref="BackupEncoder"/>, which by construction only ever tells the truth. Building
/// them here, from the same primitives the encoder uses, is what lets those paths be tested at all.
/// </remarks>
internal static class RecoveryFixtures
{
    internal const string FileName = "keys.kdbx";

    /// <summary>A backup ID that is not <see cref="FixedBackupIdGenerator.DefaultBackupId"/>.</summary>
    internal const string ForeignBackupId = "M4XZ";

    /// <summary>Encodes <paramref name="content"/> the way the application does.</summary>
    internal static async Task<EncodedBackup> EncodeAsync(
        byte[] content,
        string fileName = FileName,
        EncodeOptions? options = null,
        string backupId = FixedBackupIdGenerator.DefaultBackupId)
    {
        using MemoryStream stream = new(content);

        return await new BackupEncoder(new FixedBackupIdGenerator(backupId), new FixedTimeProvider())
            .EncodeAsync(stream, fileName, options, TestContext.Current.CancellationToken);
    }

    /// <summary>Formats one code the way <see cref="BackupEncoder"/> does, from bytes chosen by the caller.</summary>
    internal static string Code(string backupId, int index, int total, ReadOnlySpan<byte> bytes)
        => HeaderCodec.FormatCode(new CodeHeader(backupId, index, total, Crc32.Compute(bytes)), Base32.Encode(bytes));

    /// <summary>
    /// Builds a complete, internally consistent set of codes — every CRC correct — around a metadata block
    /// the caller composed. What the metadata block <i>says</i> is entirely up to the caller: that is the
    /// point of this helper.
    /// </summary>
    internal static IReadOnlyList<string> Fabricate(
        BackupMetadata metadata,
        byte[] payload,
        string backupId = FixedBackupIdGenerator.DefaultBackupId)
    {
        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(payload, metadata.ChunkSizeInBytes);
        List<string> codes =
        [
            Code(
                backupId,
                HardCopyFormat.MetadataIndex,
                chunks.Count,
                Encoding.UTF8.GetBytes(BackupMetadataCodec.Format(metadata))),
        ];

        for (int index = 0; index < chunks.Count; index++)
        {
            codes.Add(Code(backupId, index + 1, chunks.Count, chunks[index].Span));
        }

        return codes;
    }

    /// <summary>
    /// A metadata block describing <paramref name="original"/>. Every field a test wants to lie about is a
    /// parameter; the defaults tell the truth.
    /// </summary>
    internal static BackupMetadata Metadata(
        byte[] original,
        bool isCompressed = false,
        bool isEncrypted = false,
        string? sha256Hex = null,
        string fileName = FileName,
        int chunkSizeInBytes = (int)ChunkSizePreset.Medium,
        long? originalSizeInBytes = null)
        => new()
        {
            FileName = fileName,
            OriginalSizeInBytes = originalSizeInBytes ?? original.Length,
            Sha256Hex = sha256Hex ?? Sha256Hex(original),
            IsCompressed = isCompressed,
            IsEncrypted = isEncrypted,
            ChunkSizeInBytes = chunkSizeInBytes,
            CreatedOn = FixedTimeProvider.DefaultDate,
        };

    internal static string Sha256Hex(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    internal static byte[] Gzip(byte[] content)
    {
        using MemoryStream buffer = new();
        using (GZipStream gzip = new(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(content);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Alters one character in the middle of a code's payload, as a misread scan or a typo would. The
    /// position matters: mid-payload the Base32 still decodes, so the damage is caught by the CRC-32 rather
    /// than by the codec's symbol-count and padding rules.
    /// </summary>
    internal static string FlipPayloadCharacter(string code) => FlipCharacter(code, PayloadMiddle(code));

    /// <summary>Alters the very last character of a code, where the codec's padding rules also apply.</summary>
    internal static string FlipLastPayloadCharacter(string code) => FlipCharacter(code, code.Length - 1);

    /// <summary>Cuts a code short, keeping <paramref name="length"/> characters.</summary>
    internal static string Truncate(string code, int length) => code[..length];

    /// <summary>Cuts a code's payload in half, leaving the header and the field count intact.</summary>
    internal static string TruncatePayload(string code) => code[..PayloadMiddle(code)];

    /// <summary>Renders one code as the PNG a symbol on paper would scan to.</summary>
    internal static byte[] RenderSymbol(string code, int pixelsPerModule = 4)
        => QrRenderer.Render(code, new QrRenderOptions { PixelsPerModule = pixelsPerModule }).Png;

    /// <summary>
    /// Draws several symbols on one white sheet, as a printed page holds them — the shape an imported scan
    /// really has, and the one that makes the reader find many codes in a single image.
    /// </summary>
    internal static byte[] RenderPage(
        IReadOnlyList<string> codes,
        int columns = 3,
        int pixelsPerModule = 4,
        SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        const int margin = 24;
        const int gap = 16;

        List<SKImage> symbols = [];
        try
        {
            foreach (string code in codes)
            {
                symbols.Add(SKImage.FromEncodedData(RenderSymbol(code, pixelsPerModule)));
            }

            int cell = symbols.Max(symbol => symbol.Width);
            int rows = ((symbols.Count - 1) / columns) + 1;
            int width = (2 * margin) + (columns * cell) + ((columns - 1) * gap);
            int height = (2 * margin) + (rows * cell) + ((rows - 1) * gap);

            using SKSurface surface = SKSurface.Create(new SKImageInfo(width, height));
            surface.Canvas.Clear(SKColors.White);
            for (int index = 0; index < symbols.Count; index++)
            {
                surface.Canvas.DrawImage(
                    symbols[index],
                    margin + ((index % columns) * (cell + gap)),
                    margin + ((index / columns) * (cell + gap)));
            }

            return Encode(surface, format);
        }
        finally
        {
            foreach (SKImage symbol in symbols)
            {
                symbol.Dispose();
            }
        }
    }

    /// <summary>A readable image with nothing on it — an accidentally blank scan.</summary>
    internal static byte[] BlankImage(int size = 400)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
        surface.Canvas.Clear(SKColors.White);

        return Encode(surface, SKEncodedImageFormat.Png);
    }

    /// <summary>
    /// Re-encodes a PNG as an uncompressed 24-bit BMP. Skia decodes BMP but does not write it, and the
    /// application claims to accept BMP imports, so the format needs a file to prove it against.
    /// </summary>
    internal static byte[] ToBmp(byte[] png)
    {
        const int fileHeaderSize = 14;
        const int infoHeaderSize = 40;

        using SKBitmap? bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);

        int stride = ((bitmap.Width * 3) + 3) / 4 * 4;
        int pixelDataSize = stride * bitmap.Height;
        byte[] bmp = new byte[fileHeaderSize + infoHeaderSize + pixelDataSize];

        using (MemoryStream stream = new(bmp))
        using (BinaryWriter writer = new(stream, Encoding.ASCII))
        {
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(bmp.Length);
            writer.Write(0);
            writer.Write(fileHeaderSize + infoHeaderSize);

            writer.Write(infoHeaderSize);
            writer.Write(bitmap.Width);
            writer.Write(bitmap.Height);
            writer.Write((short)1);
            writer.Write((short)24);
            writer.Write(0);
            writer.Write(pixelDataSize);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // BMP rows run bottom to top, and each pixel is written blue, green, red.
            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                long rowStart = stream.Position;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);
                    writer.Write(pixel.Blue);
                    writer.Write(pixel.Green);
                    writer.Write(pixel.Red);
                }

                stream.Position = rowStart + stride;
            }
        }

        return bmp;
    }

    /// <summary>Bytes that are not an image at all.</summary>
    internal static byte[] NotAnImage() => Encoding.UTF8.GetBytes("This file is text, whatever its extension claims.");

    /// <summary>Random bytes: gzip cannot shrink them, so the stored-as-is path is exercised.</summary>
    internal static byte[] Incompressible(int length)
    {
        byte[] content = new byte[length];
        new Random(length).NextBytes(content);

        return content;
    }

    /// <summary>Highly repetitive text: gzip shrinks it by orders of magnitude.</summary>
    internal static byte[] Compressible(int repetitions = 500)
        => Encoding.UTF8.GetBytes(
            string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", repetitions)));

    /// <summary>
    /// Shuffles the codes deterministically, so "any order" is tested with a specific, reproducible disorder
    /// rather than one that differs every run.
    /// </summary>
    internal static IReadOnlyList<string> Shuffled(IReadOnlyList<string> codes, int seed = 79)
    {
        List<string> shuffled = [.. codes];
        Random random = new(seed);
        for (int index = shuffled.Count - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        return shuffled;
    }

    /// <summary>Offers every code to the session, in the order given.</summary>
    internal static IReadOnlyList<AddCodeResult> AddAll(RecoverySession session, IEnumerable<string> codes)
    {
        List<AddCodeResult> results = [];
        foreach (string code in codes)
        {
            results.Add(session.AddCode(code));
        }

        return results;
    }

    /// <summary>Imports one image into the session from bytes.</summary>
    internal static ImageScanResult AddImage(RecoverySession session, byte[] image)
    {
        using MemoryStream stream = new(image);

        return session.AddImage(stream);
    }

    private static byte[] Encode(SKSurface surface, SKEncodedImageFormat format)
    {
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(format, quality: 100);
        Assert.NotNull(data);

        return data.ToArray();
    }

    private static int PayloadMiddle(string code)
    {
        int payloadStart = 0;
        for (int field = 0; field < 4; field++)
        {
            payloadStart = code.IndexOf(HardCopyFormat.FieldSeparator, payloadStart) + 1;
            Assert.True(payloadStart > 0, $"'{code}' does not have five fields.");
        }

        return payloadStart + ((code.Length - payloadStart) / 2);
    }

    private static string FlipCharacter(string code, int position)
    {
        char[] characters = code.ToCharArray();
        characters[position] = characters[position] == 'A' ? 'B' : 'A';

        return new string(characters);
    }
}
