using System;
using System.Globalization;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.Resources;

namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// Turns Core's recovery outcomes into the sentences the user reads.
/// </summary>
/// <remarks>
/// The whole translation layer between Core's result objects and this UI, kept in one place so that it can be
/// tested exhaustively. The severities follow the desktop app's judgement: a code from another backup is a
/// <see cref="MessageSeverity.Warning"/> (it was silently ignored and the user should know why their page did not
/// help), a damaged code is an <see cref="MessageSeverity.Error"/>, a hash mismatch a warning (the bytes exist and
/// the decision about them is the user's) — except in a check's verdict, where there is nothing to decide; see
/// <see cref="DescribeCheck"/>.
/// </remarks>
public static class OutcomeMessages
{
    /// <summary>Describes what became of one code offered to the session.</summary>
    /// <param name="result">The outcome to describe.</param>
    /// <param name="sessionBackupId">
    /// The backup the session is recovering, named when a code from another one is refused — so the user can tell
    /// which of the two piles is the stray one.
    /// </param>
    /// <returns>The message to show.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static StatusMessage Describe(AddCodeResult result, string? sessionBackupId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            AddCodeOutcome.Accepted when result.Index == HardCopyFormat.MetadataIndex
                => StatusMessage.Success(Strings.CodeAcceptedMetadata),
            AddCodeOutcome.Accepted => StatusMessage.Success(Format(Strings.CodeAcceptedFormat, result.Index)),
            AddCodeOutcome.Duplicate => StatusMessage.Information(Format(Strings.CodeDuplicateFormat, result.Index)),
            AddCodeOutcome.BadCrc => StatusMessage.Error(Format(Strings.CodeBadCrcFormat, result.Index)),
            AddCodeOutcome.Conflict => StatusMessage.Error(Format(Strings.CodeConflictFormat, result.Index)),
            AddCodeOutcome.WrongBackup => StatusMessage.Warning(Format(
                Strings.CodeWrongBackupFormat,
                result.Index,
                result.BackupId,
                sessionBackupId ?? Strings.UnknownValue)),

            // A malformed code may or may not have got far enough to have an index: a metadata block whose bytes are
            // intact but are not a metadata block has one; a stray QR code that is a URL has none.
            _ when result.Index is int index => StatusMessage.Error(Format(Strings.CodeMalformedFormat, index)),
            _ => StatusMessage.Error(Strings.CodeMalformed),
        };
    }

    /// <summary>Describes what came of rebuilding the file.</summary>
    /// <param name="result">The outcome to describe.</param>
    /// <returns>The message to show.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static StatusMessage Describe(AssemblyResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // The mismatch is the one outcome worth spelling out in full: both hashes, so the user can see for
        // themselves that the recovery is not the file that was backed up.
        if (result is { Outcome: AssemblyOutcome.HashMismatch, Sha256Hex: string actual, Metadata.Sha256Hex: string expected })
        {
            return StatusMessage.Warning(Format(Strings.AssemblyHashMismatchFormat, actual, expected));
        }

        return result.Outcome switch
        {
            AssemblyOutcome.Verified => StatusMessage.Success(Strings.AssemblyVerified),
            AssemblyOutcome.HashMismatch => StatusMessage.Warning(Strings.AssemblyHashMismatch),
            AssemblyOutcome.EncryptionUnsupported => StatusMessage.Error(Strings.AssemblyEncryptionUnsupported),
            AssemblyOutcome.DecompressionFailed => StatusMessage.Error(Strings.AssemblyDecompressionFailed),
            _ => StatusMessage.Error(Strings.AssemblyIncomplete),
        };
    }

    /// <summary>Describes what a check of a printout found once every code was in.</summary>
    /// <param name="result">The outcome of rebuilding the file in memory.</param>
    /// <returns>The verdict to show.</returns>
    /// <remarks>
    /// Not the recovery's sentences: a check offers nothing to save, so a hash mismatch is not a warning about bytes
    /// the user may still want but a failed check — every outcome short of <see cref="AssemblyOutcome.Verified"/>
    /// means the printout cannot be relied on, and is an <see cref="MessageSeverity.Error"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static StatusMessage DescribeCheck(AssemblyResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result is { Outcome: AssemblyOutcome.HashMismatch, Sha256Hex: string actual, Metadata.Sha256Hex: string expected })
        {
            return StatusMessage.Error(Format(Strings.CheckHashMismatchFormat, actual, expected));
        }

        return result.Outcome switch
        {
            AssemblyOutcome.Verified => StatusMessage.Success(Strings.CheckPassed),
            AssemblyOutcome.HashMismatch => StatusMessage.Error(Strings.CheckHashMismatch),
            AssemblyOutcome.EncryptionUnsupported => StatusMessage.Error(Strings.CheckEncryptionUnsupported),
            AssemblyOutcome.DecompressionFailed => StatusMessage.Error(Strings.CheckDecompressionFailed),
            _ => StatusMessage.Error(Strings.CheckIncomplete),
        };
    }

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
