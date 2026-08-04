using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Enigma.HardCopy.Core;

/// <summary>
/// One recovery in progress: codes go in, in any order and from any source, and the original file comes out
/// once they are all there.
/// </summary>
/// <remarks>
/// <para>
/// A recovery is not a function of its input, it is a process — pages get scanned twice, one symbol refuses
/// to read and gets typed in by hand, a sheet from a different backup turns up in the pile. So this is a
/// stateful session rather than a method: it accumulates codes, tells the user what is still missing, and
/// rebuilds the file only when nothing is.
/// </para>
/// <para>
/// <b>Nothing here throws over a bad code.</b> Every outcome a scan or a typo can produce is reported as an
/// <see cref="AddCodeResult"/> — see <see cref="AddCodeOutcome"/> — because during a recovery those are
/// ordinary events, not exceptional ones. Exceptions are reserved for programming and I/O errors.
/// </para>
/// <para>
/// <b>The session's identity comes from the first code it accepts.</b> That code fixes the backup ID and the
/// chunk count; anything contradicting either is rejected from then on, so scans of two different backups
/// cannot be spliced into one file. A code is only allowed to fix them once it has passed its own checksum —
/// a damaged code never gets to define what the session is recovering.
/// </para>
/// <para>
/// <b>Integrity is checked twice, at two different scales.</b> Every code carries a CRC-32 of its own bytes,
/// caught here at the code that contains it, and the metadata block carries the SHA-256 of the whole
/// original file, checked once at the end by <see cref="TryAssemble"/>. The first tells the user which code
/// to re-scan; only the second can say the recovery is intact.
/// </para>
/// <para>
/// Each call is atomic, so a session may be handed to background work — a UI importing several images at
/// once — without further synchronisation. A <i>sequence</i> of calls is not atomic: reading
/// <see cref="Status"/> and then calling <see cref="TryAssemble"/> is two operations, and something may have
/// arrived in between.
/// </para>
/// </remarks>
public sealed class RecoverySession
{
    /// <summary>
    /// The buffer size used while decompressing. Large enough that a backup of a few hundred kilobytes is a
    /// handful of reads, small enough to be irrelevant to memory.
    /// </summary>
    private const int DecompressionWindowInBytes = 8192;

    /// <summary>
    /// Decodes the metadata block strictly: bytes that are not valid UTF-8 are damage the checksum could not
    /// catch, and the replacement characters a tolerant decoder would substitute would silently mangle the
    /// file name a recovery is supposed to restore.
    /// </summary>
    private static readonly UTF8Encoding _strictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly Lock _gate = new();
    private readonly Dictionary<int, byte[]> _chunks = [];

    private string? _backupId;
    private int? _totalChunks;
    private byte[]? _metadataBytes;
    private BackupMetadata? _metadata;

    /// <summary>
    /// Gets a snapshot of how far the recovery has got — which backup, what is known about the file, and
    /// which codes are still missing.
    /// </summary>
    /// <remarks>
    /// Each read builds a fresh snapshot, and walks the received chunks to do it. That is a few hundred
    /// comparisons for a backup of a few hundred kilobytes, and it keeps the returned value immune to
    /// whatever arrives next — which is what makes it safe to hand straight to a UI.
    /// </remarks>
    public RecoveryStatus Status
    {
        get
        {
            lock (_gate)
            {
                return new RecoveryStatus
                {
                    BackupId = _backupId,
                    Metadata = _metadata,
                    TotalChunks = _totalChunks,
                    ReceivedChunks = _chunks.Count,
                    MissingIndexes = GetMissingIndexes(),
                };
            }
        }
    }

    /// <summary>
    /// Offers one code to the recovery — as scanned, or as typed by hand. Whitespace and case are normalized
    /// first, so a code split across several lines or entered in lower case is accepted.
    /// </summary>
    /// <param name="code">
    /// The code text. May be <see langword="null"/>, empty or nonsense, which is reported as
    /// <see cref="AddCodeOutcome.Malformed"/> rather than thrown.
    /// </param>
    /// <returns>What became of the code.</returns>
    public AddCodeResult AddCode(string? code)
    {
        if (!HeaderCodec.TryParseCode(code, out CodeHeader header, out string? payload))
        {
            return AddCodeResult.Malformed();
        }

        lock (_gate)
        {
            if (_backupId is not null && !string.Equals(header.BackupId, _backupId, StringComparison.Ordinal))
            {
                return AddCodeResult.WrongBackup(header.Index, header.BackupId);
            }

            // The CRC-32 covers the payload only, so this is the last check that can be trusted absolutely:
            // everything after it reasons about a header that no checksum protects.
            if (!Base32.TryDecode(payload, out byte[]? bytes) || Crc32.Compute(bytes) != header.Crc)
            {
                return AddCodeResult.BadCrc(header.Index);
            }

            if (_totalChunks is int total && header.Total != total)
            {
                return AddCodeResult.Conflict(header.Index);
            }

            return header.IsMetadata ? StoreMetadata(header, bytes) : StoreChunk(header, bytes);
        }
    }

    /// <summary>
    /// Reads every code in an imported image and offers each one to the recovery. A scanned page carries a
    /// dozen codes, and each is reported separately, so a single smudged symbol does not fail the import.
    /// </summary>
    /// <param name="image">
    /// The image's bytes — PNG, JPEG or BMP — read from the stream's current position to its end.
    /// </param>
    /// <returns>
    /// What was found in the image and what became of each code, or
    /// <see cref="ImageScanResult.Unreadable"/> when the bytes are not a decodable image.
    /// </returns>
    /// <remarks>
    /// Decoding is CPU-bound and synchronous, and this method does not choose a thread for the caller: a UI
    /// caller imports images from a background task.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
    public ImageScanResult AddImage(Stream image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (!ImageDecoder.TryReadCodes(image, out IReadOnlyList<string>? codes))
        {
            return ImageScanResult.Unreadable;
        }

        List<AddCodeResult> results = new(codes.Count);
        foreach (string code in codes)
        {
            results.Add(AddCode(code));
        }

        return ImageScanResult.Scanned(results);
    }

    /// <summary>
    /// Rebuilds the original file, if everything needed is present: the chunks are concatenated in index
    /// order, decompressed when the metadata block says they form a gzip stream, and the result is checked
    /// against the SHA-256 recorded when the backup was made.
    /// </summary>
    /// <returns>
    /// The rebuilt file and whether its hash matched — or <see cref="AssemblyOutcome.Incomplete"/> when the
    /// metadata block or a chunk is still missing. Never throws over missing or contradictory codes.
    /// </returns>
    /// <remarks>
    /// A mismatched hash still returns the bytes, so a user who understands what they are looking at can
    /// keep them; nothing else in this application may present them as a successful recovery.
    /// </remarks>
    public AssemblyResult TryAssemble()
    {
        lock (_gate)
        {
            if (_metadata is null || _totalChunks is not int total || _chunks.Count != total)
            {
                return AssemblyResult.Incomplete(_metadata);
            }

            if (_metadata.IsEncrypted)
            {
                return AssemblyResult.EncryptionUnsupported(_metadata);
            }

            byte[] payload = Concatenate(total);
            byte[] content;
            if (_metadata.IsCompressed)
            {
                if (!TryDecompress(payload, _metadata.OriginalSizeInBytes, out byte[]? decompressed))
                {
                    return AssemblyResult.DecompressionFailed(_metadata);
                }

                content = decompressed;
            }
            else
            {
                content = payload;
            }

            string sha256Hex = Convert.ToHexStringLower(SHA256.HashData(content));

            // The metadata codec normalizes the recorded hash to lower case, but a caller-built
            // BackupMetadata need not have, and the comparison is too important to depend on that.
            return string.Equals(sha256Hex, _metadata.Sha256Hex, StringComparison.OrdinalIgnoreCase)
                ? AssemblyResult.Verified(content, sha256Hex, _metadata)
                : AssemblyResult.HashMismatch(content, sha256Hex, _metadata);
        }
    }

    private AddCodeResult StoreMetadata(CodeHeader header, byte[] bytes)
    {
        if (_metadataBytes is not null)
        {
            return _metadataBytes.AsSpan().SequenceEqual(bytes)
                ? AddCodeResult.Duplicate(header.Index)
                : AddCodeResult.Conflict(header.Index);
        }

        // Intact bytes that are not a metadata block mean this code was produced by something else — the
        // checksum vouches for the bytes, so there is nothing to re-scan.
        if (!TryReadMetadata(bytes, out BackupMetadata? metadata))
        {
            return AddCodeResult.Malformed(header.Index);
        }

        Adopt(header);
        _metadataBytes = bytes;
        _metadata = metadata;

        return AddCodeResult.Accepted(header.Index);
    }

    private AddCodeResult StoreChunk(CodeHeader header, byte[] bytes)
    {
        if (_chunks.TryGetValue(header.Index, out byte[]? held))
        {
            return held.AsSpan().SequenceEqual(bytes)
                ? AddCodeResult.Duplicate(header.Index)
                : AddCodeResult.Conflict(header.Index);
        }

        Adopt(header);
        _chunks.Add(header.Index, bytes);

        return AddCodeResult.Accepted(header.Index);
    }

    /// <summary>
    /// Takes the backup ID and chunk count from the first code the session accepts. Both are already known
    /// to agree with whatever was adopted before, so this only ever fills in what is still unset.
    /// </summary>
    private void Adopt(CodeHeader header)
    {
        _backupId ??= header.BackupId;
        _totalChunks ??= header.Total;
    }

    private IReadOnlyList<int> GetMissingIndexes()
    {
        if (_totalChunks is not int total)
        {
            return [];
        }

        List<int> missing = new(total - _chunks.Count);
        for (int index = 1; index <= total; index++)
        {
            if (!_chunks.ContainsKey(index))
            {
                missing.Add(index);
            }
        }

        return missing;
    }

    /// <summary>
    /// Joins the data chunks in index order. Every index from 1 to <paramref name="total"/> is present by
    /// construction — indexes above the total are rejected when a code is parsed, index 0 is the metadata
    /// block, and the count has been checked — so a missing key here would be a bug, and throwing is right.
    /// </summary>
    private byte[] Concatenate(int total)
    {
        int length = 0;
        for (int index = 1; index <= total; index++)
        {
            length += _chunks[index].Length;
        }

        byte[] payload = new byte[length];
        int offset = 0;
        for (int index = 1; index <= total; index++)
        {
            byte[] chunk = _chunks[index];
            chunk.CopyTo(payload, offset);
            offset += chunk.Length;
        }

        return payload;
    }

    private static bool TryReadMetadata(byte[] bytes, [NotNullWhen(true)] out BackupMetadata? metadata)
    {
        string text;
        try
        {
            text = _strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            metadata = null;
            return false;
        }

        return BackupMetadataCodec.TryParse(text, out metadata);
    }

    private static bool TryDecompress(byte[] payload, long maxLength, [NotNullWhen(true)] out byte[]? content)
    {
        content = null;
        try
        {
            using MemoryStream compressed = new(payload);
            using GZipStream gzip = new(compressed, CompressionMode.Decompress);
            using MemoryStream buffer = new();

            byte[] window = new byte[DecompressionWindowInBytes];
            int read;
            while ((read = gzip.Read(window, 0, window.Length)) > 0)
            {
                if (buffer.Length + read > maxLength)
                {
                    // A stream that expands past the size the metadata block records cannot be the file that
                    // block describes, and stopping here is also what keeps a damaged — or deliberately
                    // crafted — backup from being decompressed into memory without limit.
                    return false;
                }

                buffer.Write(window, 0, read);
            }

            content = buffer.ToArray();
            return true;
        }
        catch (InvalidDataException)
        {
            // Not a gzip stream, or its own trailer disagrees with its contents.
            return false;
        }
    }
}
