using System.Collections.Generic;

namespace Enigma.HardCopy.Core;

/// <summary>
/// The result of encoding one file: the code strings to print, and what they describe.
/// </summary>
public sealed class EncodedBackup
{
    /// <summary>
    /// Gets the backup ID carried by every code — see <see cref="CodeHeader.BackupId"/>.
    /// </summary>
    public required string BackupId { get; init; }

    /// <summary>Gets the metadata describing the encoded file.</summary>
    public required BackupMetadata Metadata { get; init; }

    /// <summary>
    /// Gets every code of the backup, in printing order. <c>Codes[0]</c> is always the metadata block
    /// (barcode index <see cref="HardCopyFormat.MetadataIndex"/>); the remaining entries are the data
    /// chunks, in order, and there is one per <see cref="CodeHeader.Total"/>.
    /// </summary>
    public required IReadOnlyList<string> Codes { get; init; }
}
