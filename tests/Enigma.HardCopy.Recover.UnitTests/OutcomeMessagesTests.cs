using System;
using System.Globalization;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.Resources;
using Enigma.HardCopy.Recover.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Recover.UnitTests;

/// <summary>
/// Every outcome Core can report becomes a sentence at the right severity — none falls through to a blank line or
/// an enum name.
/// </summary>
public sealed class OutcomeMessagesTests
{
    [Fact]
    public void Describe_AnAcceptedMetadataCode_IsASuccess()
        => Assert.Equal(
            StatusMessage.Success(Strings.CodeAcceptedMetadata),
            OutcomeMessages.Describe(AddCodeResult.Accepted(HardCopyFormat.MetadataIndex), "ABCD"));

    [Fact]
    public void Describe_AnAcceptedDataCode_IsASuccessNamingItsIndex()
        => Assert.Equal(
            StatusMessage.Success(Format(Strings.CodeAcceptedFormat, 4)),
            OutcomeMessages.Describe(AddCodeResult.Accepted(4), "ABCD"));

    [Fact]
    public void Describe_ADuplicate_IsInformationOnly()
        => Assert.Equal(MessageSeverity.Information, OutcomeMessages.Describe(AddCodeResult.Duplicate(4), "ABCD").Severity);

    [Fact]
    public void Describe_ACodeFromAnotherBackup_IsAWarningNamingBothBackups()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.WrongBackup(4, "WXYZ"), "ABCD");

        Assert.Equal(StatusMessage.Warning(Format(Strings.CodeWrongBackupFormat, 4, "WXYZ", "ABCD")), message);
    }

    [Fact]
    public void Describe_ACodeFromAnotherBackup_WithNoSessionBackupKnown_StillReads()
        => Assert.Contains(
            Strings.UnknownValue,
            OutcomeMessages.Describe(AddCodeResult.WrongBackup(4, "WXYZ"), null).Text,
            StringComparison.Ordinal);

    [Theory]
    [InlineData(AddCodeOutcome.BadCrc)]
    [InlineData(AddCodeOutcome.Conflict)]
    public void Describe_ADamagedCode_IsAnError(AddCodeOutcome outcome)
    {
        AddCodeResult result = outcome is AddCodeOutcome.BadCrc ? AddCodeResult.BadCrc(2) : AddCodeResult.Conflict(2);

        Assert.True(OutcomeMessages.Describe(result, "ABCD").IsError);
    }

    [Fact]
    public void Describe_AForeignQrCode_SaysItIsNotOneOfOurs()
        => Assert.Equal(StatusMessage.Error(Strings.CodeMalformed), OutcomeMessages.Describe(AddCodeResult.Malformed(), null));

    [Fact]
    public void Describe_AMalformedCodeWithAnIndex_NamesTheIndex()
        => Assert.Equal(
            StatusMessage.Error(Format(Strings.CodeMalformedFormat, 0)),
            OutcomeMessages.Describe(AddCodeResult.Malformed(0), "ABCD"));

    [Fact]
    public void Describe_EveryAssemblyOutcome_HasASentence()
    {
        foreach (AssemblyOutcome outcome in Enum.GetValues<AssemblyOutcome>())
        {
            StatusMessage message = OutcomeMessages.Describe(ResultFor(outcome));

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.DoesNotContain(outcome.ToString(), message.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Describe_AVerifiedFile_IsTheOnlySuccess()
    {
        foreach (AssemblyOutcome outcome in Enum.GetValues<AssemblyOutcome>())
        {
            Assert.Equal(outcome is AssemblyOutcome.Verified, OutcomeMessages.Describe(ResultFor(outcome)).IsSuccess);
        }
    }

    [Fact]
    public void Describe_AHashMismatch_IsAWarningShowingBothHashes()
    {
        StatusMessage message = OutcomeMessages.Describe(
            AssemblyResult.HashMismatch([1, 2, 3], "aaaa", Metadata("bbbb")));

        Assert.True(message.IsWarning);
        Assert.Contains("aaaa", message.Text, StringComparison.Ordinal);
        Assert.Contains("bbbb", message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeCheck_EveryAssemblyOutcome_HasASentence()
    {
        foreach (AssemblyOutcome outcome in Enum.GetValues<AssemblyOutcome>())
        {
            StatusMessage message = OutcomeMessages.DescribeCheck(ResultFor(outcome));

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.DoesNotContain(outcome.ToString(), message.Text, StringComparison.Ordinal);
        }
    }

    // A check either passes or fails: there are no bytes on offer to soften a failure into a warning.
    [Fact]
    public void DescribeCheck_AVerifiedFile_IsTheOnlyPass_AndEveryOtherOutcomeAFailure()
    {
        foreach (AssemblyOutcome outcome in Enum.GetValues<AssemblyOutcome>())
        {
            StatusMessage message = OutcomeMessages.DescribeCheck(ResultFor(outcome));

            Assert.Equal(outcome is AssemblyOutcome.Verified ? MessageSeverity.Success : MessageSeverity.Error, message.Severity);
        }
    }

    [Fact]
    public void DescribeCheck_AHashMismatch_IsAFailureShowingBothHashes()
    {
        StatusMessage message = OutcomeMessages.DescribeCheck(
            AssemblyResult.HashMismatch([1, 2, 3], "aaaa", Metadata("bbbb")));

        Assert.Equal(StatusMessage.Error(Format(Strings.CheckHashMismatchFormat, "aaaa", "bbbb")), message);
    }

    [Fact]
    public void Describe_NullResults_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => OutcomeMessages.Describe((AddCodeResult)null!, null));
        Assert.Throws<ArgumentNullException>(() => OutcomeMessages.Describe((AssemblyResult)null!));
        Assert.Throws<ArgumentNullException>(() => OutcomeMessages.DescribeCheck(null!));
    }

    private static AssemblyResult ResultFor(AssemblyOutcome outcome) => outcome switch
    {
        AssemblyOutcome.Verified => AssemblyResult.Verified([1], "aaaa", Metadata("aaaa")),
        AssemblyOutcome.HashMismatch => AssemblyResult.HashMismatch([1], "aaaa", Metadata("bbbb")),
        AssemblyOutcome.EncryptionUnsupported => AssemblyResult.EncryptionUnsupported(Metadata("aaaa")),
        AssemblyOutcome.DecompressionFailed => AssemblyResult.DecompressionFailed(Metadata("aaaa")),
        _ => AssemblyResult.Incomplete(),
    };

    private static BackupMetadata Metadata(string sha256Hex) => new()
    {
        FileName = "secret.key",
        OriginalSizeInBytes = 1,
        Sha256Hex = sha256Hex,
        IsCompressed = false,
        ChunkSizeInBytes = 256,
        CreatedOn = new DateOnly(2026, 10, 19),
    };

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
