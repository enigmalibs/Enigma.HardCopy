using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.HardCopy.Core.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

/// <summary>
/// The recovery state machine: what a session does with each code it is offered, what it reports while codes
/// are still missing, and what it will and will not rebuild.
/// </summary>
public sealed class RecoverySessionTests
{
    /// <summary>
    /// The fixture size. At the default chunk size this is six data chunks plus the metadata block — enough
    /// for out-of-order, partial and conflicting cases to be distinguishable, small enough to stay quick.
    /// </summary>
    private const int FixtureSizeInBytes = 6_000;

    // ---------------------------------------------------------------------------------------------------
    // Status: what the user is told between scans.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public void Status_BeforeAnyCode_KnowsNothingAndClaimsNothingIsMissing()
    {
        RecoveryStatus status = new RecoverySession().Status;

        Assert.Null(status.BackupId);
        Assert.Null(status.Metadata);
        Assert.Null(status.TotalChunks);
        Assert.False(status.HasMetadata);
        Assert.False(status.IsComplete);
        Assert.Equal(0, status.ReceivedChunks);
        Assert.Empty(status.MissingIndexes);
    }

    [Fact]
    public async Task AddCode_TheMetadataBlock_IsAcceptedAndReadInFull()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();

        AddCodeResult result = session.AddCode(backup.Codes[0]);

        Assert.Equal(AddCodeResult.Accepted(HardCopyFormat.MetadataIndex), result);
        RecoveryStatus status = session.Status;
        Assert.True(status.HasMetadata);

        // Field for field — BackupMetadata is a record, so this compares the name, size, hash, both flags,
        // the chunk size and the date at once. Anything the codec dropped would show up here.
        Assert.Equal(backup.Metadata, status.Metadata);
        Assert.Equal(backup.BackupId, status.BackupId);
        Assert.Equal(backup.Codes.Count - 1, status.TotalChunks);
        Assert.False(status.IsComplete);
    }

    // The chunk count lives in every header, so it is known from the first code accepted — the UI can show
    // "1 of 7" before it knows what the file is even called.
    [Fact]
    public async Task AddCode_ADataChunk_FixesTheBackupIdAndTotalBeforeTheMetadataBlockArrives()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();

        AddCodeResult result = session.AddCode(backup.Codes[3]);

        Assert.Equal(AddCodeResult.Accepted(3), result);
        RecoveryStatus status = session.Status;
        Assert.Equal(backup.BackupId, status.BackupId);
        Assert.Equal(backup.Codes.Count - 1, status.TotalChunks);
        Assert.Null(status.Metadata);
        Assert.False(status.HasMetadata);
        Assert.False(status.IsComplete);
        Assert.Equal(1, status.ReceivedChunks);
        Assert.DoesNotContain(3, status.MissingIndexes);
    }

    [Fact]
    public async Task Status_MissingIndexes_AreTheOnesNotYetInAscendingOrder()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        int total = backup.Codes.Count - 1;

        session.AddCode(backup.Codes[0]);
        session.AddCode(backup.Codes[4]);
        session.AddCode(backup.Codes[1]);

        RecoveryStatus status = session.Status;
        Assert.Equal(2, status.ReceivedChunks);
        Assert.Equal(Enumerable.Range(2, total - 1).Where(index => index != 4), status.MissingIndexes);
        Assert.False(status.IsComplete);
    }

    [Fact]
    public async Task AddCode_EveryCodeShuffled_LeavesTheSessionComplete()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();

        IReadOnlyList<AddCodeResult> results = RecoveryFixtures.AddAll(
            session,
            RecoveryFixtures.Shuffled(backup.Codes));

        Assert.All(results, result => Assert.True(result.IsAccepted, $"code {result.Index} was {result.Outcome}"));
        RecoveryStatus status = session.Status;
        Assert.True(status.IsComplete);
        Assert.Empty(status.MissingIndexes);
        Assert.Equal(backup.Codes.Count - 1, status.ReceivedChunks);
    }

    // ---------------------------------------------------------------------------------------------------
    // Codes the session already holds.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AddCode_TheSameChunkTwice_IsADuplicateAndChangesNothing()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        session.AddCode(backup.Codes[2]);

        AddCodeResult result = session.AddCode(backup.Codes[2]);

        Assert.Equal(AddCodeResult.Duplicate(2), result);
        Assert.True(result.IsHeld);
        Assert.False(result.IsAccepted);
        Assert.Equal(1, session.Status.ReceivedChunks);
    }

    [Fact]
    public async Task AddCode_TheMetadataBlockTwice_IsADuplicate()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        session.AddCode(backup.Codes[0]);

        AddCodeResult result = session.AddCode(backup.Codes[0]);

        Assert.Equal(AddCodeResult.Duplicate(HardCopyFormat.MetadataIndex), result);
        Assert.Equal(backup.Metadata, session.Status.Metadata);
    }

    // A conflict is the one case where two codes agree on which index they are and disagree on what it
    // holds. The code already in the session wins: it has been through the same checks the new one just
    // failed to contradict, and overwriting it would lose a chunk that may well be the good one.
    [Fact]
    public async Task AddCode_TheSameIndexWithDifferentBytes_IsAConflictAndTheHeldChunkSurvives()
    {
        EncodedBackup backup = await BackupAsync();
        int total = backup.Codes.Count - 1;
        RecoverySession session = new();
        session.AddCode(backup.Codes[1]);

        AddCodeResult result = session.AddCode(
            RecoveryFixtures.Code(backup.BackupId, 1, total, [9, 9, 9]));

        Assert.Equal(AddCodeResult.Conflict(1), result);
        Assert.Equal(1, session.Status.ReceivedChunks);

        // The proof that the impostor was not stored: the rest of the backup still assembles and verifies.
        RecoveryFixtures.AddAll(session, backup.Codes);
        Assert.True(session.TryAssemble().HashVerified);
    }

    [Fact]
    public async Task AddCode_ASecondDifferentMetadataBlock_IsAConflict()
    {
        EncodedBackup backup = await BackupAsync();
        int total = backup.Codes.Count - 1;
        RecoverySession session = new();
        session.AddCode(backup.Codes[0]);

        string rival = RecoveryFixtures.Code(
            backup.BackupId,
            HardCopyFormat.MetadataIndex,
            total,
            Encoding.UTF8.GetBytes(
                BackupMetadataCodec.Format(backup.Metadata with { FileName = "something-else.bin" })));
        AddCodeResult result = session.AddCode(rival);

        Assert.Equal(AddCodeResult.Conflict(HardCopyFormat.MetadataIndex), result);
        Assert.Equal(RecoveryFixtures.FileName, session.Status.Metadata?.FileName);
    }

    // The CRC-32 covers a code's payload, not its header, so a mistyped total is damage no checksum catches.
    // Accepting it would leave the session waiting for chunks that do not exist, or believing it is complete
    // when it is not.
    [Fact]
    public async Task AddCode_ADifferentChunkTotal_IsAConflict()
    {
        EncodedBackup backup = await BackupAsync();
        int total = backup.Codes.Count - 1;
        RecoverySession session = new();
        session.AddCode(backup.Codes[1]);

        AddCodeResult result = session.AddCode(
            RecoveryFixtures.Code(backup.BackupId, 2, total + 1, [1, 2, 3]));

        Assert.Equal(AddCodeResult.Conflict(2), result);
        Assert.Equal(total, session.Status.TotalChunks);
        Assert.Equal(1, session.Status.ReceivedChunks);
    }

    // ---------------------------------------------------------------------------------------------------
    // Codes from somewhere else.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public async Task AddCode_AForeignBackupId_IsRejectedAndTheForeignIdIsReported()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        session.AddCode(backup.Codes[0]);

        AddCodeResult result = session.AddCode(
            RecoveryFixtures.Code(RecoveryFixtures.ForeignBackupId, 1, backup.Codes.Count - 1, [1, 2, 3]));

        Assert.Equal(AddCodeOutcome.WrongBackup, result.Outcome);
        Assert.Equal(1, result.Index);
        Assert.Equal(RecoveryFixtures.ForeignBackupId, result.BackupId);
        Assert.False(result.IsHeld);
        Assert.Equal(0, session.Status.ReceivedChunks);
        Assert.Equal(backup.BackupId, session.Status.BackupId);
    }

    // "Wrong backup" is relative to the session, not absolute: an empty session is recovering whichever
    // backup the first sound code belongs to.
    [Fact]
    public void AddCode_AnyBackupIdOnTheFirstCode_IsAccepted()
    {
        RecoverySession session = new();

        AddCodeResult result = session.AddCode(
            RecoveryFixtures.Code(RecoveryFixtures.ForeignBackupId, 1, 1, [1, 2, 3]));

        Assert.Equal(AddCodeResult.Accepted(1), result);
        Assert.Equal(RecoveryFixtures.ForeignBackupId, session.Status.BackupId);
    }

    // ---------------------------------------------------------------------------------------------------
    // Text that is not a code of this format.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello")]
    [InlineData("EHC2:K7QA:1/2:00000000:AAAA")]
    [InlineData("XYZ1:K7QA:1/2:00000000:AAAA")]
    [InlineData("EHC1:K7QA:1/2:00000000")]
    [InlineData("EHC1:K7QA:1/2:00000000:")]
    [InlineData("EHC1:K7QA:X/2:00000000:AAAA")]
    [InlineData("EHC1:K7QA:3/2:00000000:AAAA")]
    [InlineData("EHC1:K7Q:1/2:00000000:AAAA")]
    [InlineData("EHC1:K7QA:1/2:1A2B3C:AAAA")]
    [InlineData("EHC1:K7QA:1/2:00000000:AA1A")]
    public void AddCode_NotACodeOfThisFormat_IsMalformedWithNoIndexToReport(string? code)
    {
        AddCodeResult result = new RecoverySession().AddCode(code);

        Assert.Equal(AddCodeResult.Malformed(), result);
        Assert.Null(result.Index);
        Assert.False(result.IsHeld);
    }

    [Fact]
    public async Task AddCode_ACodeCutShortOfItsPayload_IsMalformed()
    {
        EncodedBackup backup = await BackupAsync();

        Assert.Equal(
            AddCodeOutcome.Malformed,
            new RecoverySession().AddCode(RecoveryFixtures.Truncate(backup.Codes[1], 10)).Outcome);
    }

    // ---------------------------------------------------------------------------------------------------
    // Codes that are damaged rather than foreign. All of these report the index, because "re-scan code 4"
    // is the only useful thing to tell the user.
    // ---------------------------------------------------------------------------------------------------
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task AddCode_ACharacterAlteredMidPayload_IsBadCrcAtThatIndex(int index)
    {
        EncodedBackup backup = await BackupAsync();

        AddCodeResult result = new RecoverySession().AddCode(
            RecoveryFixtures.FlipPayloadCharacter(backup.Codes[index]));

        Assert.Equal(AddCodeResult.BadCrc(index), result);
    }

    // The final character is the one the Base32 codec's padding rules cover, so the damage may be caught by
    // the codec rather than by the checksum. Either way it is the same thing to report: that code did not
    // survive, re-scan it.
    [Fact]
    public async Task AddCode_TheFinalCharacterAltered_IsBadCrc()
    {
        EncodedBackup backup = await BackupAsync();

        AddCodeResult result = new RecoverySession().AddCode(
            RecoveryFixtures.FlipLastPayloadCharacter(backup.Codes[2]));

        Assert.Equal(AddCodeResult.BadCrc(2), result);
    }

    [Fact]
    public async Task AddCode_APayloadCutInHalf_IsBadCrc()
    {
        EncodedBackup backup = await BackupAsync();

        AddCodeResult result = new RecoverySession().AddCode(
            RecoveryFixtures.TruncatePayload(backup.Codes[2]));

        Assert.Equal(AddCodeResult.BadCrc(2), result);
    }

    // A damaged code must not get to say what the session is recovering: its header is unverified, so a
    // misread backup ID would lock the session onto a backup that does not exist and reject every good code
    // that followed.
    [Fact]
    public async Task AddCode_ADamagedCode_DoesNotFixTheSessionIdentity()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();

        session.AddCode(RecoveryFixtures.FlipPayloadCharacter(backup.Codes[1]));

        RecoveryStatus status = session.Status;
        Assert.Null(status.BackupId);
        Assert.Null(status.TotalChunks);
        Assert.Equal(0, status.ReceivedChunks);

        // Still open to any backup, which is what proves nothing was locked in.
        Assert.True(session.AddCode(RecoveryFixtures.Code(RecoveryFixtures.ForeignBackupId, 1, 1, [7])).IsAccepted);
    }

    [Fact]
    public async Task AddCode_TypedByHandInLowerCaseAcrossSeveralLines_IsAccepted()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();

        IReadOnlyList<AddCodeResult> results = RecoveryFixtures.AddAll(
            session,
            backup.Codes.Select(AsTypedByHand));

        Assert.All(results, result => Assert.True(result.IsAccepted));
        Assert.True(session.TryAssemble().HashVerified);
    }

    // ---------------------------------------------------------------------------------------------------
    // A metadata block whose bytes are intact but are not a metadata block. The checksum vouches for the
    // bytes, so there is nothing to re-scan — the code was produced by something else.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public void AddCode_IntactBytesThatAreNotAMetadataBlock_IsMalformed()
    {
        string code = RecoveryFixtures.Code(
            FixedBackupIdGenerator.DefaultBackupId,
            HardCopyFormat.MetadataIndex,
            2,
            Encoding.UTF8.GetBytes("this is not a metadata block"));
        RecoverySession session = new();

        AddCodeResult result = session.AddCode(code);

        Assert.Equal(AddCodeResult.Malformed(HardCopyFormat.MetadataIndex), result);
        Assert.Null(session.Status.Metadata);
        Assert.Null(session.Status.BackupId);
    }

    [Fact]
    public void AddCode_MetadataBytesThatAreNotValidUtf8_IsMalformed()
    {
        string code = RecoveryFixtures.Code(
            FixedBackupIdGenerator.DefaultBackupId,
            HardCopyFormat.MetadataIndex,
            2,
            [0xFF, 0xFE, 0x41]);

        AddCodeResult result = new RecoverySession().AddCode(code);

        Assert.Equal(AddCodeResult.Malformed(HardCopyFormat.MetadataIndex), result);
    }

    // ---------------------------------------------------------------------------------------------------
    // Assembly.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public void TryAssemble_WithNoCodesAtAll_IsIncomplete()
    {
        AssemblyResult result = new RecoverySession().TryAssemble();

        Assert.Equal(AssemblyOutcome.Incomplete, result.Outcome);
        Assert.False(result.HashVerified);
        Assert.Null(result.Content);
        Assert.Null(result.Metadata);
        Assert.Null(result.Sha256Hex);
    }

    [Fact]
    public async Task TryAssemble_WithOneChunkMissing_IsIncompleteButKnowsTheFile()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, backup.Codes.Where((_, index) => index != 3));

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.Incomplete, result.Outcome);
        Assert.Null(result.Content);
        Assert.Equal(backup.Metadata, result.Metadata);
        Assert.Equal(new[] { 3 }, session.Status.MissingIndexes);
    }

    // Every chunk present and still not recoverable: without the metadata block there is no name to save
    // under, no way to know whether the bytes need decompressing, and nothing to verify them against.
    [Fact]
    public async Task TryAssemble_WithEveryChunkButNoMetadataBlock_IsIncomplete()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, backup.Codes.Skip(1));

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.Incomplete, result.Outcome);
        Assert.Null(result.Metadata);
        Assert.Empty(session.Status.MissingIndexes);
        Assert.False(session.Status.IsComplete);
    }

    [Fact]
    public async Task TryAssemble_Complete_ReturnsTheFileAndVerifiesItsHash()
    {
        byte[] content = RecoveryFixtures.Incompressible(FixtureSizeInBytes);
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content);
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, backup.Codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.Verified, result.Outcome);
        Assert.True(result.HashVerified);
        Assert.Equal(content, result.Content);
        Assert.Equal(RecoveryFixtures.Sha256Hex(content), result.Sha256Hex);
        Assert.Equal(backup.Metadata, result.Metadata);
    }

    [Fact]
    public async Task TryAssemble_ACompressedBackup_IsDecompressedFirst()
    {
        byte[] content = RecoveryFixtures.Compressible();
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync(content);
        Assert.True(backup.Metadata.IsCompressed, "the fixture must take the compressed path");
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, backup.Codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.Verified, result.Outcome);
        Assert.Equal(content, result.Content);
    }

    [Fact]
    public async Task TryAssemble_ABackupOfAnEmptyFile_IsCompleteFromTheMetadataBlockAlone()
    {
        EncodedBackup backup = await RecoveryFixtures.EncodeAsync([]);
        RecoverySession session = new();

        Assert.True(session.AddCode(backup.Codes[0]).IsAccepted);

        RecoveryStatus status = session.Status;
        Assert.Equal(0, status.TotalChunks);
        Assert.True(status.IsComplete);
        Assert.Empty(status.MissingIndexes);
        AssemblyResult result = session.TryAssemble();
        Assert.Equal(AssemblyOutcome.Verified, result.Outcome);
        Assert.Empty(result.Content!);
    }

    [Fact]
    public async Task TryAssemble_Twice_GivesTheSameAnswer()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, backup.Codes);

        AssemblyResult first = session.TryAssemble();
        AssemblyResult second = session.TryAssemble();

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.Content, second.Content);
        Assert.Equal(first.Sha256Hex, second.Sha256Hex);
    }

    // The end-to-end check, and the only one that can catch a chunk that passed its own CRC yet is not the
    // chunk that belongs at that index. The bytes are still returned: PHASE05's "save anyway" needs them.
    [Fact]
    public void TryAssemble_WithAWrongRecordedHash_IsAMismatchAndStillHandsBackTheBytes()
    {
        byte[] content = RecoveryFixtures.Incompressible(3_000);
        IReadOnlyList<string> codes = RecoveryFixtures.Fabricate(
            RecoveryFixtures.Metadata(content, sha256Hex: new string('0', 64)),
            content);
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.HashMismatch, result.Outcome);
        Assert.False(result.HashVerified);
        Assert.Equal(content, result.Content);
        Assert.Equal(RecoveryFixtures.Sha256Hex(content), result.Sha256Hex);
    }

    [Fact]
    public void TryAssemble_WhenTheCompressedFlagLies_IsADecompressionFailure()
    {
        byte[] content = RecoveryFixtures.Incompressible(3_000);
        IReadOnlyList<string> codes = RecoveryFixtures.Fabricate(
            RecoveryFixtures.Metadata(content, isCompressed: true),
            content);
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.DecompressionFailed, result.Outcome);
        Assert.Null(result.Content);
        Assert.NotNull(result.Metadata);
    }

    // A gzip stream that expands past the size the metadata block records is refused before it is
    // materialized, so a damaged or deliberately crafted backup cannot be decompressed into memory without
    // limit.
    [Fact]
    public void TryAssemble_WhenTheStreamExpandsPastTheRecordedSize_IsADecompressionFailure()
    {
        byte[] content = RecoveryFixtures.Incompressible(4_000);
        IReadOnlyList<string> codes = RecoveryFixtures.Fabricate(
            RecoveryFixtures.Metadata(content, isCompressed: true, originalSizeInBytes: 100),
            RecoveryFixtures.Gzip(content));
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.DecompressionFailed, result.Outcome);
        Assert.Null(result.Content);
    }

    // The opposite direction is deliberately not a decompression failure: a stream shorter than the recorded
    // size is materialized — it costs nothing — and reported as the hash mismatch it is, which is the more
    // precise thing to say about it.
    [Fact]
    public void TryAssemble_WhenTheStreamIsShorterThanRecorded_IsAHashMismatch()
    {
        byte[] content = RecoveryFixtures.Incompressible(3_000);
        IReadOnlyList<string> codes = RecoveryFixtures.Fabricate(
            RecoveryFixtures.Metadata(content, isCompressed: true),
            RecoveryFixtures.Gzip([1, 2, 3]));
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, codes);

        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.HashMismatch, result.Outcome);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
    }

    // Format version 1 never sets the encryption flag, so a backup that has it set was written by something
    // that can do more than this build can undo. Handing back what is presumably ciphertext as a recovered
    // file would be worse than refusing.
    [Fact]
    public void TryAssemble_WhenTheEncryptionFlagIsSet_RefusesToRecover()
    {
        byte[] content = RecoveryFixtures.Incompressible(2_000);
        IReadOnlyList<string> codes = RecoveryFixtures.Fabricate(
            RecoveryFixtures.Metadata(content, isEncrypted: true),
            content);
        RecoverySession session = new();
        RecoveryFixtures.AddAll(session, codes);

        Assert.True(session.Status.IsComplete, "every code is present — the refusal is about the flag, not the codes");
        AssemblyResult result = session.TryAssemble();

        Assert.Equal(AssemblyOutcome.EncryptionUnsupported, result.Outcome);
        Assert.False(result.HashVerified);
        Assert.Null(result.Content);
        Assert.True(result.Metadata?.IsEncrypted);
    }

    // ---------------------------------------------------------------------------------------------------
    // Images and threads.
    // ---------------------------------------------------------------------------------------------------
    [Fact]
    public void AddImage_NullStream_Throws()
        => Assert.Throws<ArgumentNullException>(() => new RecoverySession().AddImage(null!));

    [Fact]
    public void AddImage_BytesThatAreNotAnImage_ReportsTheFileRatherThanTheCodes()
    {
        ImageScanResult result = RecoveryFixtures.AddImage(new RecoverySession(), RecoveryFixtures.NotAnImage());

        Assert.False(result.IsImageReadable);
        Assert.Empty(result.Results);
        Assert.Equal(0, result.CodesFound);
        Assert.Equal(0, result.AcceptedCount);
    }

    // The session is documented as safe to hand to background work, which is exactly what PHASE05's
    // multi-image import will do. Every code goes in four times from several threads at once: each must end
    // up held exactly once, and the file must still verify.
    [Fact]
    public async Task AddCode_FromSeveralThreadsAtOnce_KeepsTheSessionConsistent()
    {
        EncodedBackup backup = await BackupAsync();
        RecoverySession session = new();
        string[] offered =
            [.. Enumerable.Range(0, 4).SelectMany(seed => RecoveryFixtures.Shuffled(backup.Codes, seed))];

        AddCodeResult[] results = new AddCodeResult[offered.Length];
        Parallel.For(0, offered.Length, index => results[index] = session.AddCode(offered[index]));

        Assert.All(results, result => Assert.True(result.IsHeld, $"code {result.Index} was {result.Outcome}"));
        Assert.Equal(backup.Codes.Count, results.Count(result => result.IsAccepted));
        RecoveryStatus status = session.Status;
        Assert.True(status.IsComplete);
        Assert.Equal(backup.Codes.Count - 1, status.ReceivedChunks);
        Assert.True(session.TryAssemble().HashVerified);
    }

    private static Task<EncodedBackup> BackupAsync()
        => RecoveryFixtures.EncodeAsync(RecoveryFixtures.Incompressible(FixtureSizeInBytes));

    /// <summary>
    /// The same code as someone would type it off the page: lower case, broken across lines, with stray
    /// spaces. Every field of the format is upper-case or numeric and none contains whitespace, so none of
    /// this can lose information.
    /// </summary>
    private static string AsTypedByHand(string code)
    {
        StringBuilder typed = new("  ");
        for (int index = 0; index < code.Length; index += 40)
        {
            typed.Append(code.AsSpan(index, Math.Min(40, code.Length - index))).Append('\n');
        }

        return typed.ToString().ToLowerInvariant();
    }
}
