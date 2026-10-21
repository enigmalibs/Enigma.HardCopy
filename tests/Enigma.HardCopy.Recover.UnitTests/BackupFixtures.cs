using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.UnitTests.TestDoubles;

namespace Enigma.HardCopy.Recover.UnitTests;

/// <summary>
/// Builds the real backups the scan session is fed with.
/// </summary>
/// <remarks>
/// As in the desktop suite, the codes come from Core's own encoder rather than from hand-written strings, so a
/// format change cannot leave these tests passing against a format that no longer exists. The doctored metadata
/// code is the one exception — the only way to reach a hash mismatch, which a correct encoder never produces.
/// </remarks>
internal static class BackupFixtures
{
    /// <summary>The backup ID every fixture backup carries unless told otherwise.</summary>
    internal const string BackupId = "ABCD";

    /// <summary>A second backup's ID, for the codes that must be refused.</summary>
    internal const string OtherBackupId = "WXYZ";

    /// <summary>The name every fixture backup records unless told otherwise.</summary>
    internal const string FileName = "secret.key";

    /// <summary>Encodes <paramref name="content"/> the way the desktop app does, at the smallest density.</summary>
    /// <param name="content">The bytes to back up.</param>
    /// <param name="backupId">The backup ID to stamp into every code.</param>
    /// <param name="fileName">The name to record.</param>
    /// <returns>The encoded backup; its first code is the metadata code.</returns>
    internal static async Task<EncodedBackup> EncodeAsync(
        byte[] content,
        string backupId = BackupId,
        string fileName = FileName)
    {
        BackupEncoder encoder = new(new FixedBackupIdGenerator(backupId), TimeProvider.System);
        using MemoryStream stream = new(content, writable: false);

        return await encoder.EncodeAsync(stream, fileName, EncodeOptions.FromPreset(ChunkSizePreset.Small));
    }

    /// <summary>Bytes that do not compress, so a backup of them keeps one chunk per few hundred bytes.</summary>
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
    /// Damages the first payload character of <paramref name="code"/>, as a misprinted symbol would, leaving the
    /// header intact.
    /// </summary>
    /// <param name="code">The code to damage.</param>
    /// <returns>The damaged code.</returns>
    internal static string DamagePayload(string code)
    {
        int payloadStart = code.LastIndexOf(HardCopyFormat.FieldSeparator) + 1;
        char original = code[payloadStart];
        char replacement = original == Base32.Alphabet[0] ? Base32.Alphabet[1] : Base32.Alphabet[0];

        return string.Concat(code.AsSpan(0, payloadStart), replacement.ToString(), code.AsSpan(payloadStart + 1));
    }
}
