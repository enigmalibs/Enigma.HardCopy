using System.Globalization;

namespace Enigma.HardCopy.Core;

/// <summary>
/// Constants describing the Enigma.HardCopy barcode payload format.
/// </summary>
/// <remarks>
/// Every generated QR code carries a single ASCII string of the shape
/// <c>EHC1:&lt;BID&gt;:&lt;IDX&gt;/&lt;TOT&gt;:&lt;CRC&gt;:&lt;PAYLOAD&gt;</c>, whose characters all
/// belong to <see cref="AlphanumericCharset"/> so the code stays in QR alphanumeric mode and remains
/// typable by hand during an app-less recovery.
/// </remarks>
public static class HardCopyFormat
{
    /// <summary>The format magic, identifying a code as an Enigma.HardCopy payload.</summary>
    public const string Magic = "EHC";

    /// <summary>
    /// The payload format version. Bump this on any breaking change to the code layout, so a decoder
    /// can reject payloads it does not understand instead of misreading them.
    /// </summary>
    public const int Version = 1;

    /// <summary>The field separator between the header fields of a code.</summary>
    public const char FieldSeparator = ':';

    /// <summary>The separator between the chunk index and the total chunk count.</summary>
    public const char IndexSeparator = '/';

    /// <summary>The barcode index reserved for the metadata block; data chunks start at 1.</summary>
    public const int MetadataIndex = 0;

    /// <summary>
    /// The 45-character QR alphanumeric-mode charset. Every character of a generated code is drawn
    /// from this set, which is what keeps the encoding overhead near 10% instead of forcing byte mode.
    /// </summary>
    public const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>
    /// The leading token of every code — <see cref="Magic"/> followed by <see cref="Version"/>
    /// (for example <c>EHC1</c>).
    /// </summary>
    public static string Prefix => Magic + Version.ToString(CultureInfo.InvariantCulture);
}
