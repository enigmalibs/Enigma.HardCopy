using System;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class OutcomeMessagesTests
{
    private static readonly BackupMetadata _metadata = new()
    {
        FileName = "secret.key",
        OriginalSizeInBytes = 1024,
        Sha256Hex = new string('a', 64),
        IsCompressed = false,
        ChunkSizeInBytes = 512,
        CreatedOn = new DateOnly(2026, 8, 4),
    };

    [Fact]
    public void AcceptedMetadataCode_IsNamedAsSuch_NotAsCodeZero()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Accepted(HardCopyFormat.MetadataIndex));

        Assert.Equal(Strings.CodeAcceptedMetadata, message.Text);
        Assert.Equal(MessageSeverity.Success, message.Severity);
    }

    [Fact]
    public void AcceptedDataCode_NamesItsIndex()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Accepted(7));

        Assert.Contains("7", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Success, message.Severity);
    }

    [Fact]
    public void Duplicate_IsInformation_BecauseNothingIsWrong()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Duplicate(3));

        Assert.Contains("3", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Information, message.Severity);
    }

    [Fact]
    public void BadCrc_IsAnError_NamingTheCodeToRescan()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.BadCrc(4));

        Assert.Contains("4", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void Conflict_IsAnError_NamingTheIndex()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Conflict(5));

        Assert.Contains("5", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void WrongBackup_IsAWarning_NamingTheForeignBackup()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.WrongBackup(2, "WXYZ"));

        Assert.Contains("2", message.Text, StringComparison.Ordinal);
        Assert.Contains("WXYZ", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Warning, message.Severity);
    }

    [Fact]
    public void MalformedWithoutAnIndex_SaysSoWithoutInventingOne()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Malformed());

        Assert.Equal(Strings.CodeMalformed, message.Text);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void MalformedWithAnIndex_NamesIt()
    {
        StatusMessage message = OutcomeMessages.Describe(AddCodeResult.Malformed(9));

        Assert.Contains("9", message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void Verified_IsTheOnlySuccessfulAssembly()
    {
        StatusMessage message = OutcomeMessages.Describe(
            AssemblyResult.Verified([1, 2, 3], _metadata.Sha256Hex, _metadata));

        Assert.Equal(Strings.AssemblyVerified, message.Text);
        Assert.Equal(MessageSeverity.Success, message.Severity);
    }

    [Fact]
    public void HashMismatch_IsAWarning_ShowingBothHashes()
    {
        string actual = new('b', 64);
        StatusMessage message = OutcomeMessages.Describe(AssemblyResult.HashMismatch([1], actual, _metadata));

        Assert.Contains(actual, message.Text, StringComparison.Ordinal);
        Assert.Contains(_metadata.Sha256Hex, message.Text, StringComparison.Ordinal);
        Assert.Equal(MessageSeverity.Warning, message.Severity);
    }

    [Fact]
    public void EncryptionUnsupported_IsAnError()
    {
        StatusMessage message = OutcomeMessages.Describe(AssemblyResult.EncryptionUnsupported(_metadata));

        Assert.Equal(Strings.AssemblyEncryptionUnsupported, message.Text);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void DecompressionFailed_IsAnError()
    {
        StatusMessage message = OutcomeMessages.Describe(AssemblyResult.DecompressionFailed(_metadata));

        Assert.Equal(Strings.AssemblyDecompressionFailed, message.Text);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void Incomplete_IsAnError()
    {
        StatusMessage message = OutcomeMessages.Describe(AssemblyResult.Incomplete());

        Assert.Equal(Strings.AssemblyIncomplete, message.Text);
        Assert.Equal(MessageSeverity.Error, message.Severity);
    }

    [Fact]
    public void NullResults_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => OutcomeMessages.Describe((AddCodeResult)null!));
        Assert.Throws<ArgumentNullException>(() => OutcomeMessages.Describe((AssemblyResult)null!));
    }
}
