using System;
using System.Globalization;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// Turns the Core recovery outcomes into the sentences the user reads.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of the translation layer between Core's result objects and the UI, kept in one place so
/// it can be tested exhaustively — every outcome maps to a string, and none of them falls through to a blank
/// label or a raw enum name.
/// </para>
/// <para>
/// The severities are a judgement, not a mechanical mapping. A duplicate code is
/// <see cref="MessageSeverity.Information"/> because nothing is wrong with it; a code from another backup is a
/// <see cref="MessageSeverity.Warning"/> because it was silently ignored and the user should know why their
/// page did not help; a rebuilt file whose hash does not match is a warning rather than an error because the
/// bytes exist and the decision about them is the user's.
/// </para>
/// </remarks>
public static class OutcomeMessages
{
    /// <summary>Describes what became of one code offered to a recovery.</summary>
    /// <param name="result">The outcome to describe.</param>
    /// <returns>The message to show.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static StatusMessage Describe(AddCodeResult result)
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
            AddCodeOutcome.WrongBackup
                => StatusMessage.Warning(Format(Strings.CodeWrongBackupFormat, result.Index, result.BackupId)),

            // A malformed code may or may not have got far enough to have an index — the metadata block whose
            // bytes are intact but are not a metadata block is the case that has one.
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

        // The mismatch is the one outcome worth spelling out in full: both hashes, side by side, so the user
        // can see for themselves that the recovery is not the file that was backed up.
        if (result is { Outcome: AssemblyOutcome.HashMismatch, Sha256Hex: string actual, Metadata.Sha256Hex: string expected })
        {
            return StatusMessage.Warning(Format(Strings.RecoverMismatchWarningFormat, actual, expected));
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

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
