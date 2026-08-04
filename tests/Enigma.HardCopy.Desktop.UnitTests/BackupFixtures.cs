using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Builds the real backups the recovery ViewModel is fed with.
/// </summary>
/// <remarks>
/// The codes here come from Core's own encoder rather than from hand-written strings: a ViewModel test that
/// invented its own code format would keep passing after the format changed. The doctored metadata block in
/// <see cref="MetadataCodeWith"/> is the exception — it is the only way to reach the outcomes that a correct
/// encoder never produces, a wrong hash and a set encryption flag.
/// </remarks>
internal static class BackupFixtures
{
    /// <summary>The backup ID every fixture backup carries unless told otherwise.</summary>
    internal const string BackupId = "ABCD";

    /// <summary>The name every fixture backup records unless told otherwise.</summary>
    internal const string FileName = "secret.key";

    /// <summary>Encodes <paramref name="content"/> the way the application does.</summary>
    /// <param name="content">The bytes to back up.</param>
    /// <param name="fileName">The name to record.</param>
    /// <param name="backupId">The backup ID to stamp into every code.</param>
    /// <param name="preset">The chunk size to split with.</param>
    /// <returns>The encoded backup.</returns>
    internal static async Task<EncodedBackup> EncodeAsync(
        byte[] content,
        string fileName = FileName,
        string backupId = BackupId,
        ChunkSizePreset preset = ChunkSizePreset.Small)
    {
        BackupEncoder encoder = new(new FixedBackupIdGenerator(backupId), TimeProvider.System);
        using MemoryStream stream = new(content, writable: false);

        return await encoder.EncodeAsync(stream, fileName, EncodeOptions.FromPreset(preset));
    }

    /// <summary>
    /// Returns bytes that do not compress, so the encoded backup keeps its <c>c=0</c> flag and its chunks
    /// concatenate straight back to these bytes.
    /// </summary>
    /// <param name="length">How many bytes to produce.</param>
    /// <param name="seed">The seed, so the bytes are the same on every run.</param>
    /// <returns>The bytes.</returns>
    internal static byte[] IncompressibleBytes(int length, int seed = 1)
    {
        byte[] bytes = new byte[length];
        new Random(seed).NextBytes(bytes);

        return bytes;
    }

    /// <summary>
    /// Builds a valid, checksum-correct metadata code carrying <paramref name="metadata"/> instead of what
    /// <paramref name="backup"/> actually recorded.
    /// </summary>
    /// <param name="backup">The backup whose ID and chunk count the code must agree with.</param>
    /// <param name="metadata">The metadata to put in the code.</param>
    /// <returns>The code text.</returns>
    internal static string MetadataCodeWith(EncodedBackup backup, BackupMetadata metadata)
    {
        byte[] payload = Encoding.UTF8.GetBytes(BackupMetadataCodec.Format(metadata));
        CodeHeader header = new(
            backup.BackupId,
            HardCopyFormat.MetadataIndex,
            backup.Codes.Count - 1,
            Crc32.Compute(payload));

        return HeaderCodec.FormatCode(header, Base32.Encode(payload));
    }

    /// <summary>
    /// Damages the payload of <paramref name="code"/> the way a misread symbol does — one character, leaving the
    /// header and therefore the index intact.
    /// </summary>
    /// <param name="code">The code to damage.</param>
    /// <returns>The damaged code.</returns>
    /// <remarks>
    /// The <b>first</b> payload character is the one changed, deliberately. Base32 is unpadded here, so the last
    /// symbol of a payload can carry bits that decode to nothing — changing it might leave the bytes, and
    /// therefore the checksum, untouched. The first symbol always carries real bits.
    /// </remarks>
    internal static string DamagePayload(string code)
    {
        int payloadStart = code.LastIndexOf(HardCopyFormat.FieldSeparator) + 1;
        char original = code[payloadStart];
        char replacement = original == Base32.Alphabet[0] ? Base32.Alphabet[1] : Base32.Alphabet[0];

        return string.Concat(code.AsSpan(0, payloadStart), replacement.ToString(), code.AsSpan(payloadStart + 1));
    }

    /// <summary>Renders one code as the PNG a scan of it would look like.</summary>
    /// <param name="code">The code to render.</param>
    /// <returns>The PNG's bytes.</returns>
    internal static byte[] RenderPng(string code) => QrRenderer.Render(code).Png;
}
