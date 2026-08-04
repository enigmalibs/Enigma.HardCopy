using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The default <see cref="IBackupEncoder"/>: hashes the file, compresses it when that helps, splits it into
/// chunks and formats each one — plus a metadata block — as a barcode string.
/// </summary>
/// <remarks>
/// <para>
/// The file is read into memory in one piece. This application backs up small critical files — keys,
/// password databases — and both the hash and the compression decision need the whole content anyway.
/// </para>
/// <para>
/// The work is CPU-bound once the stream has been read, and this type does not move it off the calling
/// thread: choosing a thread is the caller's business. A UI caller should invoke it from a background task.
/// </para>
/// </remarks>
public sealed class BackupEncoder : IBackupEncoder
{
    private readonly IBackupIdGenerator _backupIdGenerator;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="BackupEncoder"/> class.</summary>
    /// <param name="backupIdGenerator">Supplies the backup ID stamped into every code.</param>
    /// <param name="timeProvider">Supplies the date recorded in the metadata block.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public BackupEncoder(IBackupIdGenerator backupIdGenerator, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(backupIdGenerator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _backupIdGenerator = backupIdGenerator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<EncodedBackup> EncodeAsync(
        Stream file,
        string fileName,
        EncodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (!file.CanRead)
        {
            throw new ArgumentException("The stream is not readable.", nameof(file));
        }

        options ??= EncodeOptions.Default;

        byte[] original = await ReadToEndAsync(file, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        string sha256Hex = Convert.ToHexStringLower(SHA256.HashData(original));
        (byte[] payload, bool isCompressed) = Pack(original);
        IReadOnlyList<ReadOnlyMemory<byte>> chunks = Chunker.Split(payload, options.ChunkSizeInBytes);
        string backupId = _backupIdGenerator.NewBackupId();

        BackupMetadata metadata = new()
        {
            FileName = fileName,
            OriginalSizeInBytes = original.Length,
            Sha256Hex = sha256Hex,
            IsCompressed = isCompressed,
            ChunkSizeInBytes = options.ChunkSizeInBytes,
            CreatedOn = DateOnly.FromDateTime(_timeProvider.GetLocalNow().Date),
        };

        List<string> codes = new(chunks.Count + 1)
        {
            FormatCode(
                backupId,
                HardCopyFormat.MetadataIndex,
                chunks.Count,
                Encoding.UTF8.GetBytes(BackupMetadataCodec.Format(metadata))),
        };

        for (int index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            codes.Add(FormatCode(backupId, index + 1, chunks.Count, chunks[index].Span));
        }

        return new EncodedBackup { BackupId = backupId, Metadata = metadata, Codes = codes };
    }

    private static string FormatCode(string backupId, int index, int total, ReadOnlySpan<byte> bytes)
        => HeaderCodec.FormatCode(
            new CodeHeader(backupId, index, total, Crc32.Compute(bytes)),
            Base32.Encode(bytes));

    /// <summary>
    /// Compresses <paramref name="original"/> and keeps the result only when it is genuinely smaller —
    /// already-compressed inputs such as an archive or an encrypted container would otherwise grow.
    /// </summary>
    private static (byte[] Payload, bool IsCompressed) Pack(byte[] original)
    {
        byte[] compressed = Compress(original);
        return compressed.Length < original.Length ? (compressed, true) : (original, false);
    }

    private static byte[] Compress(ReadOnlySpan<byte> original)
    {
        using MemoryStream buffer = new();
        using (GZipStream gzip = new(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(original);
        }

        return buffer.ToArray();
    }

    private static async Task<byte[]> ReadToEndAsync(Stream file, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        await file.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return buffer.ToArray();
    }
}
