namespace Enigma.HardCopy.Core;

/// <summary>
/// The header of a single barcode: everything that precedes the payload in
/// <c>EHC1:&lt;BID&gt;:&lt;IDX&gt;/&lt;TOT&gt;:&lt;CRC&gt;:&lt;PAYLOAD&gt;</c>.
/// </summary>
/// <param name="BackupId">
/// The backup ID shared by every code of one backup — <see cref="HardCopyFormat.BackupIdLength"/>
/// <see cref="Base32"/> characters. A recovery rejects codes carrying a different ID, so scans from two
/// backups cannot be silently mixed into one file.
/// </param>
/// <param name="Index">
/// The code's position: <see cref="HardCopyFormat.MetadataIndex"/> for the metadata block, then
/// <c>1</c>..<paramref name="Total"/> for the data chunks.
/// </param>
/// <param name="Total">The number of <b>data</b> chunks in the backup — the metadata block is not counted.</param>
/// <param name="Crc">The CRC-32 of this code's decoded bytes (see <see cref="Crc32"/>).</param>
public readonly record struct CodeHeader(string BackupId, int Index, int Total, uint Crc)
{
    /// <summary>
    /// Gets a value indicating whether this is the metadata block rather than a data chunk.
    /// </summary>
    public bool IsMetadata => Index == HardCopyFormat.MetadataIndex;
}
