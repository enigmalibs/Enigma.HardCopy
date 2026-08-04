namespace Enigma.HardCopy.Core;

/// <summary>
/// The outcome of offering one code to a <see cref="RecoverySession"/>, with whatever the session could
/// learn about the code before it decided.
/// </summary>
/// <remarks>
/// Instances come from the factory methods rather than a constructor, so that every result is one of the
/// shapes the session can actually produce — an <see cref="AddCodeOutcome.Accepted"/> result without an
/// index, for example, is not expressible.
/// </remarks>
public sealed record AddCodeResult
{
    private AddCodeResult()
    {
    }

    /// <summary>Gets what became of the code.</summary>
    public required AddCodeOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the barcode index the code claims — <see cref="HardCopyFormat.MetadataIndex"/> for the metadata
    /// block — or <see langword="null"/> when the text could not be parsed far enough to have one.
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// Gets the backup ID the code carries. Set for <see cref="AddCodeOutcome.WrongBackup"/>, where it is
    /// the foreign ID worth showing the user; <see langword="null"/> otherwise.
    /// </summary>
    public string? BackupId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the code was verified and stored — that is, whether the recovery
    /// moved forward because of it.
    /// </summary>
    public bool IsAccepted => Outcome is AddCodeOutcome.Accepted;

    /// <summary>
    /// Gets a value indicating whether the session now holds this code, whether or not this particular
    /// attempt is what put it there. True for <see cref="AddCodeOutcome.Accepted"/> and
    /// <see cref="AddCodeOutcome.Duplicate"/> — the two outcomes that leave the recovery no worse off.
    /// </summary>
    public bool IsHeld => Outcome is AddCodeOutcome.Accepted or AddCodeOutcome.Duplicate;

    /// <summary>Creates a <see cref="AddCodeOutcome.Malformed"/> result.</summary>
    /// <param name="index">The barcode index, when the code got far enough to have one.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult Malformed(int? index = null)
        => new() { Outcome = AddCodeOutcome.Malformed, Index = index };

    /// <summary>Creates a <see cref="AddCodeOutcome.WrongBackup"/> result.</summary>
    /// <param name="index">The barcode index the code claims.</param>
    /// <param name="backupId">The foreign backup ID the code carries.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult WrongBackup(int index, string backupId)
        => new() { Outcome = AddCodeOutcome.WrongBackup, Index = index, BackupId = backupId };

    /// <summary>Creates a <see cref="AddCodeOutcome.BadCrc"/> result.</summary>
    /// <param name="index">The barcode index the code claims.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult BadCrc(int index) => new() { Outcome = AddCodeOutcome.BadCrc, Index = index };

    /// <summary>Creates a <see cref="AddCodeOutcome.Conflict"/> result.</summary>
    /// <param name="index">The barcode index the code claims.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult Conflict(int index) => new() { Outcome = AddCodeOutcome.Conflict, Index = index };

    /// <summary>Creates a <see cref="AddCodeOutcome.Duplicate"/> result.</summary>
    /// <param name="index">The barcode index the code claims.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult Duplicate(int index) => new() { Outcome = AddCodeOutcome.Duplicate, Index = index };

    /// <summary>Creates an <see cref="AddCodeOutcome.Accepted"/> result.</summary>
    /// <param name="index">The barcode index the code claims.</param>
    /// <returns>The result.</returns>
    public static AddCodeResult Accepted(int index) => new() { Outcome = AddCodeOutcome.Accepted, Index = index };
}
