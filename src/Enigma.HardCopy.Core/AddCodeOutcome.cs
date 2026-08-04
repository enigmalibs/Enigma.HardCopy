namespace Enigma.HardCopy.Core;

/// <summary>
/// What became of a code offered to a <see cref="RecoverySession"/>.
/// </summary>
/// <remarks>
/// None of these is an error: a scan that misreads a character, a page scanned twice, or a sheet from last
/// year's backup finding its way into the pile are all ordinary events during a recovery, which is why they
/// are reported rather than thrown. The values are ordered as the session tests for them, and
/// <see cref="Malformed"/> is deliberately the zero value so that an uninitialised outcome can never read as
/// success.
/// </remarks>
public enum AddCodeOutcome
{
    /// <summary>
    /// The text is not a code of this format at all: a wrong number of fields, a bad magic or version, a
    /// non-numeric index — or, for the metadata block specifically, intact bytes that are not a metadata
    /// block. A truncated code lands here; a code whose <i>content</i> is damaged lands in
    /// <see cref="BadCrc"/> instead.
    /// </summary>
    Malformed = 0,

    /// <summary>
    /// The code is well formed but carries a different backup ID than the codes already accepted, so it
    /// belongs to another backup. Accepting it would splice two files together.
    /// </summary>
    WrongBackup,

    /// <summary>
    /// The code's payload does not survive its own checksum — either the Base32 text cannot decode to bytes
    /// at all, or the bytes disagree with the CRC-32 in the header. Re-scan or retype that one code.
    /// </summary>
    BadCrc,

    /// <summary>
    /// The code contradicts what the session already holds: the same index carrying different bytes, or a
    /// different total chunk count. One of the two is damaged in a way its checksum could not catch, because
    /// the CRC-32 covers a code's payload and not its header.
    /// </summary>
    Conflict,

    /// <summary>
    /// The code was already in the session, byte for byte — a page scanned twice, or a code that appears on
    /// two photographs. Nothing changed, and nothing is wrong.
    /// </summary>
    Duplicate,

    /// <summary>The code was verified and stored. It is new, and the recovery moved forward.</summary>
    Accepted,
}
