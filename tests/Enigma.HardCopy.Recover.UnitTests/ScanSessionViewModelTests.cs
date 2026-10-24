using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.Resources;
using Enigma.HardCopy.Recover.Services;
using Enigma.HardCopy.Recover.UnitTests.TestDoubles;
using Enigma.HardCopy.Recover.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.HardCopy.Recover.UnitTests;

/// <summary>
/// The scan session, driven the way a phone drives it: a camera that starts or does not, frames full of codes in
/// any order and repeated many times, a picker the user may back out of, and two questions they may refuse.
/// </summary>
public sealed class ScanSessionViewModelTests
{
    /// <summary>A hash no fixture's content has, recorded by the doctored metadata code.</summary>
    private const string WrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

    private static readonly byte[] MismatchedContent = BackupFixtures.IncompressibleBytes(700, seed: 3);

    private readonly FakeCameraScanner _scanner = new();
    private readonly RecordingHapticFeedback _haptics = new();
    private readonly FakeRecoveredFileSaver _saver = new();
    private readonly FakeNotificationService _notifications = new();
    private readonly FakeConfirmationService _confirmations = new();

    [Fact]
    public void Initially_NothingIsRead_AndTheCameraIsOff()
    {
        ScanSessionViewModel session = CreateSession();

        Assert.Equal(ScanState.Ready, session.State);
        Assert.False(session.HasAnyCode);
        Assert.True(session.ShowIntroduction);
        Assert.Equal(Strings.StartScanning, session.StartLabel);
        Assert.Equal(Strings.CodesProgressUnknown, session.CodesText);
        Assert.Equal(0d, session.ProgressPercent);
        Assert.False(session.HasMissing);
        Assert.True(session.StartScanningCommand.CanExecute(null));
        Assert.False(session.SaveCommand.CanExecute(null));
        Assert.Equal(0, _scanner.StartCount);
        Assert.Equal(SessionPurpose.Recover, session.Purpose);
        Assert.Equal(Strings.ProgressHeader, session.ProgressTitle);
        Assert.True(session.CanStartCheck);
        Assert.True(session.StartCheckCommand.CanExecute(null));
        Assert.False(session.HasCheckVerdict);
    }

    [Fact]
    public async Task StartScanning_WhenTheCameraStarts_Scans()
    {
        ScanSessionViewModel session = CreateSession();

        await session.StartScanningCommand.ExecuteAsync(null);

        Assert.Equal(ScanState.Scanning, session.State);
        Assert.True(_scanner.IsRunning);
        Assert.False(session.HasCameraProblem);
        Assert.True(session.StopScanningCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(CameraStartResult.PermissionDenied)]
    [InlineData(CameraStartResult.Unavailable)]
    public async Task StartScanning_WhenTheCameraDoesNotStart_ExplainsWhy_AndCanBeRetried(CameraStartResult result)
    {
        _scanner.NextStartResult = result;
        ScanSessionViewModel session = CreateSession();

        await session.StartScanningCommand.ExecuteAsync(null);

        Assert.Equal(ScanState.Ready, session.State);
        Assert.Equal(
            result is CameraStartResult.PermissionDenied ? Strings.CameraPermissionDenied : Strings.CameraUnavailable,
            session.CameraProblem);
        Assert.True(session.StartScanningCommand.CanExecute(null));

        _scanner.NextStartResult = CameraStartResult.Started;
        await session.StartScanningCommand.ExecuteAsync(null);

        Assert.Equal(ScanState.Scanning, session.State);
        Assert.False(session.HasCameraProblem);
    }

    [Fact]
    public async Task CodesInAnyOrder_CompleteTheSession_AndTheCameraStopsByItself()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(1_200));
        ScanSessionViewModel session = await ScanningSessionAsync();

        foreach (string code in Shuffled(backup.Codes))
        {
            _scanner.Show(code);
        }

        Assert.Equal(ScanState.Complete, session.State);
        Assert.False(_scanner.IsRunning);
        Assert.Equal(1, _scanner.StopCount);
        Assert.Equal(backup.Codes.Count, _haptics.AcceptedCount);
        Assert.Equal(0, _haptics.RejectedCount);
        Assert.Equal(100d, session.ProgressPercent);
        Assert.False(session.HasMissing);
        Assert.True(session.CanSave);
        Assert.True(session.SaveCommand.CanExecute(null));
        Assert.False(session.VerifyCommand.CanExecute(null));
        Assert.False(session.HasCheckVerdict);
        Assert.Equal(StatusMessage.Success(Strings.SessionComplete), Assert.Single(_notifications.Published));
    }

    [Fact]
    public async Task Progress_CountsTheMetadataCodeAsOneMoreCode_AndListsWhatIsMissing()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(3_000));
        int total = backup.Codes.Count - 1;
        Assert.True(total >= 5, "the fixture needs a handful of data codes");
        ScanSessionViewModel session = await ScanningSessionAsync();

        _scanner.Show(backup.Codes[2]);

        Assert.True(session.HasAnyCode);
        Assert.False(session.ShowIntroduction);
        Assert.Equal(BackupFixtures.BackupId, session.BackupId);
        Assert.Equal(Format(Strings.CodesProgressFormat, 1, total), session.CodesText);
        Assert.Equal(Strings.MetadataMissing, session.MetadataText);
        Assert.Null(session.FileName);
        Assert.Equal(100d / (total + 1), session.ProgressPercent, precision: 6);
        Assert.True(session.HasMissing);
        Assert.Equal($"1, 3–{total}", session.MissingText);

        _scanner.Show(backup.Codes[0]);

        Assert.Equal(Strings.MetadataReceived, session.MetadataText);
        Assert.Equal(BackupFixtures.FileName, session.FileName);
        Assert.Equal(backup.Metadata.Sha256Hex, session.ExpectedSha256);
        Assert.Equal(Format(Strings.FileSizeFormat, backup.Metadata.OriginalSizeInBytes), session.FileSizeText);
        Assert.Equal(200d / (total + 1), session.ProgressPercent, precision: 6);
    }

    // The camera reports a code on every frame it is in view. Without the set of texts already handled, the haptics
    // would buzz continuously and the last-code line would never settle.
    [Fact]
    public async Task ACodeHeldInView_IsHandledOnce()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync();

        _scanner.Show(backup.Codes[1]);
        StatusMessage? first = session.LastScan;
        _scanner.Show(backup.Codes[1]);
        _scanner.Show(backup.Codes[1], backup.Codes[1]);

        Assert.Equal(1, _haptics.AcceptedCount);
        Assert.Same(first, session.LastScan);
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    public async Task ACodeFromAnotherBackup_IsRefusedOnce_WithAWarningNamingBothBackups()
    {
        EncodedBackup mine = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        EncodedBackup other = await BackupFixtures.EncodeAsync(
            BackupFixtures.IncompressibleBytes(600, seed: 2),
            backupId: BackupFixtures.OtherBackupId);
        ScanSessionViewModel session = await ScanningSessionAsync();
        _scanner.Show(mine.Codes[1]);
        string progressBefore = session.CodesText;

        _scanner.Show(other.Codes[2]);
        _scanner.Show(other.Codes[2]);

        StatusMessage warning = Assert.Single(_notifications.Published);
        Assert.True(warning.IsWarning);
        Assert.Contains(BackupFixtures.OtherBackupId, warning.Text, StringComparison.Ordinal);
        Assert.Contains(BackupFixtures.BackupId, warning.Text, StringComparison.Ordinal);
        Assert.Equal(warning, session.LastScan);
        Assert.Equal(1, _haptics.RejectedCount);
        Assert.Equal(progressBefore, session.CodesText);
        Assert.Equal(BackupFixtures.BackupId, session.BackupId);
    }

    [Fact]
    public async Task ADamagedCode_IsReportedAsAnError_AndChangesNothing()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync();

        _scanner.Show(BackupFixtures.DamagePayload(backup.Codes[1]));

        StatusMessage error = Assert.Single(_notifications.Published);
        Assert.True(error.IsError);
        Assert.Equal(Format(Strings.CodeBadCrcFormat, 1), error.Text);
        Assert.Equal(1, _haptics.RejectedCount);
        Assert.False(session.HasAnyCode);
    }

    [Fact]
    public async Task AQrCodeThatIsNotOneOfOurs_IsReportedOnce()
    {
        ScanSessionViewModel session = await ScanningSessionAsync();

        _scanner.Show("https://example.invalid/menu");
        _scanner.Show("https://example.invalid/menu");

        Assert.Equal(StatusMessage.Error(Strings.CodeMalformed), Assert.Single(_notifications.Published));
        Assert.Equal(1, _haptics.RejectedCount);
        Assert.False(session.HasAnyCode);
    }

    [Fact]
    public async Task CodesArrivingWhileTheCameraIsOff_AreIgnored()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = CreateSession();

        _scanner.Show(backup.Codes[1]);
        Assert.False(session.HasAnyCode);

        await session.StartScanningCommand.ExecuteAsync(null);
        session.StopScanningCommand.Execute(null);
        _scanner.Show(backup.Codes[1]);

        Assert.False(session.HasAnyCode);
        Assert.Equal(0, _haptics.AcceptedCount);
    }

    [Fact]
    public async Task StopScanning_PausesTheCamera_AndKeepsTheCodes()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync();
        _scanner.Show(backup.Codes[1]);

        session.StopScanningCommand.Execute(null);

        Assert.Equal(ScanState.Ready, session.State);
        Assert.False(_scanner.IsRunning);
        Assert.True(session.HasAnyCode);
        Assert.Equal(Strings.ResumeScanning, session.StartLabel);
        Assert.False(session.ShowIntroduction);
    }

    [Fact]
    public async Task Save_WhenTheFileVerifies_WritesTheExactBytes_UnderTheRecordedName()
    {
        byte[] original = BackupFixtures.IncompressibleBytes(1_000);
        ScanSessionViewModel session = await CompleteSessionAsync(original);

        await session.SaveCommand.ExecuteAsync(null);

        Assert.Equal(original, _saver.Destination!.Written);
        Assert.Equal([BackupFixtures.FileName], _saver.SuggestedNames);
        StatusMessage saved = _notifications.Published[^1];
        Assert.True(saved.IsSuccess);
        Assert.Equal(Format(Strings.SavedFormat, _saver.Destination.Name), saved.Text);
        Assert.False(session.CanSaveAnyway);
    }

    [Fact]
    public async Task Save_WhenThePickerIsDismissed_WritesNothing_AndSaysNothing()
    {
        ScanSessionViewModel session = await CompleteSessionAsync(BackupFixtures.IncompressibleBytes(500));
        int published = _notifications.Published.Count;
        _saver.Destination = null;

        await session.SaveCommand.ExecuteAsync(null);

        Assert.Equal(published, _notifications.Published.Count);
        Assert.True(session.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_WhenTheWriteFails_ReportsTheError()
    {
        ScanSessionViewModel session = await CompleteSessionAsync(BackupFixtures.IncompressibleBytes(500));
        _saver.Destination = new FakeSaveTarget(failure: new IOException("Drive is full."));

        await session.SaveCommand.ExecuteAsync(null);

        StatusMessage failed = _notifications.Published[^1];
        Assert.True(failed.IsError);
        Assert.Equal(Format(Strings.SaveFailedFormat, "Drive is full."), failed.Text);
        Assert.False(session.IsBusy);
        Assert.True(session.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_OnAHashMismatch_WritesNothing_AndOffersSaveAnyway()
    {
        (ScanSessionViewModel session, EncodedBackup backup) = await MismatchedSessionAsync();

        await session.SaveCommand.ExecuteAsync(null);

        Assert.Null(_saver.Destination!.Written);
        Assert.Empty(_saver.SuggestedNames);
        StatusMessage warning = _notifications.Published[^1];
        Assert.True(warning.IsWarning);
        Assert.Contains(backup.Metadata.Sha256Hex, warning.Text, StringComparison.Ordinal);
        Assert.Contains(WrongHash, warning.Text, StringComparison.Ordinal);
        Assert.True(session.CanSaveAnyway);
        Assert.True(session.SaveAnywayCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAnyway_WhenDeclined_WritesNothing_AndKeepsTheOffer()
    {
        (ScanSessionViewModel session, _) = await MismatchedSessionAsync();
        await session.SaveCommand.ExecuteAsync(null);
        _confirmations.UnverifiedSaveAnswer = false;

        await session.SaveAnywayCommand.ExecuteAsync(null);

        Assert.Equal(1, _confirmations.UnverifiedSaveAsked);
        Assert.Null(_saver.Destination!.Written);
        Assert.Empty(_saver.SuggestedNames);
        Assert.True(session.CanSaveAnyway);
    }

    [Fact]
    public async Task SaveAnyway_WhenConfirmed_WritesTheBytes_NamesBothHashes_AndWithdrawsTheOffer()
    {
        (ScanSessionViewModel session, EncodedBackup backup) = await MismatchedSessionAsync();
        await session.SaveCommand.ExecuteAsync(null);
        _confirmations.UnverifiedSaveAnswer = true;

        await session.SaveAnywayCommand.ExecuteAsync(null);

        Assert.Equal(WrongHash, _confirmations.LastExpectedSha256);
        Assert.Equal(backup.Metadata.Sha256Hex, _confirmations.LastActualSha256);
        Assert.Equal(MismatchedContent, _saver.Destination!.Written);
        Assert.True(_notifications.Published[^1].IsWarning);
        Assert.False(session.CanSaveAnyway);
    }

    [Fact]
    public async Task NewSession_WithNothingRead_DoesNotAsk_AndStartsTheCamera()
    {
        ScanSessionViewModel session = CreateSession();

        await session.NewSessionCommand.ExecuteAsync(null);

        Assert.Equal(0, _confirmations.NewSessionAsked);
        Assert.Equal(ScanState.Scanning, session.State);
    }

    [Fact]
    public async Task NewSession_WhenDeclined_KeepsEverything()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync();
        _scanner.Show(backup.Codes[1]);
        _confirmations.NewSessionAnswer = false;

        await session.NewSessionCommand.ExecuteAsync(null);

        Assert.Equal(1, _confirmations.NewSessionAsked);
        Assert.True(session.HasAnyCode);
        Assert.Equal(ScanState.Scanning, session.State);
    }

    [Fact]
    public async Task NewSession_WhenConfirmed_ForgetsEverything_AndScansAgain()
    {
        EncodedBackup first = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        EncodedBackup second = await BackupFixtures.EncodeAsync(
            BackupFixtures.IncompressibleBytes(600, seed: 2),
            backupId: BackupFixtures.OtherBackupId);
        ScanSessionViewModel session = await ScanningSessionAsync();
        _scanner.Show(first.Codes[1]);
        _confirmations.NewSessionAnswer = true;

        await session.NewSessionCommand.ExecuteAsync(null);

        Assert.False(session.HasAnyCode);
        Assert.Null(session.LastScan);
        Assert.Equal(ScanState.Scanning, session.State);

        // The new session belongs to whichever backup is scanned first — and the old one's codes are new again.
        _scanner.Show(second.Codes[1]);
        Assert.Equal(BackupFixtures.OtherBackupId, session.BackupId);
        _scanner.Show(first.Codes[1]);
        Assert.True(_notifications.Published[^1].IsWarning);
    }

    // A backup of an empty file is the metadata code alone: there are no data codes to wait for.
    [Fact]
    public async Task AnEmptyFilesBackup_CompletesWithTheMetadataCodeAlone()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync([]);
        Assert.Single(backup.Codes);
        ScanSessionViewModel session = await ScanningSessionAsync();

        _scanner.Show(backup.Codes[0]);
        await session.SaveCommand.ExecuteAsync(null);

        Assert.Equal(ScanState.Complete, session.State);
        Assert.Equal(100d, session.ProgressPercent);
        Assert.Equal(Format(Strings.CodesProgressFormat, 0, 0), session.CodesText);
        Assert.Equal(Array.Empty<byte>(), _saver.Destination!.Written);
    }

    [Fact]
    public async Task StartCheck_TurnsTheCameraOn_ForACheck()
    {
        ScanSessionViewModel session = CreateSession();

        await session.StartCheckCommand.ExecuteAsync(null);

        Assert.Equal(ScanState.Scanning, session.State);
        Assert.True(_scanner.IsRunning);
        Assert.Equal(SessionPurpose.Check, session.Purpose);
        Assert.True(session.IsChecking);
        Assert.Equal(Strings.CheckProgressHeader, session.ProgressTitle);
        Assert.False(session.CanStartCheck);
    }

    // The whole point of a check: the same scan, then a verdict on the hash — and never a file.
    [Fact]
    public async Task ACompleteCheck_VerifiesTheHash_AndSavesNothing()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(1_200));
        ScanSessionViewModel session = await ScanningSessionAsync(SessionPurpose.Check);

        _scanner.Show([.. Shuffled(backup.Codes)]);
        await VerificationAsync(session);

        Assert.Equal(ScanState.Complete, session.State);
        Assert.False(_scanner.IsRunning);
        Assert.Equal(StatusMessage.Success(Strings.CheckPassed), session.CheckVerdict);
        Assert.Equal(session.CheckVerdict, Assert.Single(_notifications.Published));
        Assert.Empty(_saver.SuggestedNames);
        Assert.Null(_saver.Destination!.Written);
        Assert.False(session.CanSave);
        Assert.False(session.SaveCommand.CanExecute(null));
        Assert.False(session.CanSaveAnyway);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task ACheck_OnAHashMismatch_FailsNamingBothHashes_AndOffersNothingToSave()
    {
        (ScanSessionViewModel session, EncodedBackup backup) = await MismatchedSessionAsync(SessionPurpose.Check);

        await VerificationAsync(session);

        StatusMessage? verdict = session.CheckVerdict;
        Assert.NotNull(verdict);
        Assert.True(verdict.IsError);
        Assert.Contains(backup.Metadata.Sha256Hex, verdict.Text, StringComparison.Ordinal);
        Assert.Contains(WrongHash, verdict.Text, StringComparison.Ordinal);
        Assert.Equal(verdict, Assert.Single(_notifications.Published));
        Assert.False(session.CanSave);
        Assert.False(session.CanSaveAnyway);
        Assert.False(session.SaveAnywayCommand.CanExecute(null));
        Assert.Empty(_saver.SuggestedNames);
        Assert.Equal(0, _confirmations.UnverifiedSaveAsked);
    }

    [Fact]
    public async Task ACheckOfAnEmptyFilesBackup_PassesWithTheMetadataCodeAlone()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync([]);
        ScanSessionViewModel session = await ScanningSessionAsync(SessionPurpose.Check);

        _scanner.Show(backup.Codes[0]);
        await VerificationAsync(session);

        Assert.Equal(StatusMessage.Success(Strings.CheckPassed), session.CheckVerdict);
        Assert.Empty(_saver.SuggestedNames);
    }

    [Fact]
    public async Task StartCheck_IsOnlyOfferedWhileTheSessionIsEmpty()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync();
        Assert.False(session.CanStartCheck);

        _scanner.Show(backup.Codes[1]);
        session.StopScanningCommand.Execute(null);

        Assert.False(session.CanStartCheck);
        Assert.False(session.StartCheckCommand.CanExecute(null));
        Assert.True(session.StartScanningCommand.CanExecute(null));
    }

    [Fact]
    public async Task ResumingAPausedCheck_CarriesOnChecking()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(1_200));
        ScanSessionViewModel session = await ScanningSessionAsync(SessionPurpose.Check);
        _scanner.Show(backup.Codes[1]);
        session.StopScanningCommand.Execute(null);
        Assert.Equal(Strings.ResumeScanning, session.StartLabel);

        await session.StartScanningCommand.ExecuteAsync(null);
        _scanner.Show([.. backup.Codes]);
        await VerificationAsync(session);

        Assert.Equal(SessionPurpose.Check, session.Purpose);
        Assert.Equal(StatusMessage.Success(Strings.CheckPassed), session.CheckVerdict);
        Assert.Empty(_saver.SuggestedNames);
    }

    // Until a code is in, nothing has been committed to: the user may still change their mind.
    [Fact]
    public async Task StartScanning_AfterACheckThatReadNothing_MakesTheSessionARecovery()
    {
        ScanSessionViewModel session = await ScanningSessionAsync(SessionPurpose.Check);
        session.StopScanningCommand.Execute(null);
        Assert.True(session.CanStartCheck);

        await session.StartScanningCommand.ExecuteAsync(null);

        Assert.Equal(SessionPurpose.Recover, session.Purpose);
        Assert.Equal(Strings.ProgressHeader, session.ProgressTitle);
    }

    [Fact]
    public async Task NewSession_AfterACheck_ForgetsTheVerdict_AndChecksAgain()
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(BackupFixtures.IncompressibleBytes(600));
        ScanSessionViewModel session = await ScanningSessionAsync(SessionPurpose.Check);
        _scanner.Show([.. backup.Codes]);
        await VerificationAsync(session);
        _confirmations.NewSessionAnswer = true;

        await session.NewSessionCommand.ExecuteAsync(null);

        Assert.Equal(1, _confirmations.NewSessionAsked);
        Assert.False(session.HasCheckVerdict);
        Assert.False(session.HasAnyCode);
        Assert.Equal(SessionPurpose.Check, session.Purpose);
        Assert.Equal(ScanState.Scanning, session.State);
    }

    [Fact]
    public void Constructor_RejectsAMissingDependency()
        => Assert.Throws<ArgumentNullException>(() => new ScanSessionViewModel(
            null!,
            _haptics,
            _saver,
            _notifications,
            _confirmations,
            NullLogger<ScanSessionViewModel>.Instance));

    private ScanSessionViewModel CreateSession() => new(
        _scanner,
        _haptics,
        _saver,
        _notifications,
        _confirmations,
        NullLogger<ScanSessionViewModel>.Instance);

    private async Task<ScanSessionViewModel> ScanningSessionAsync(SessionPurpose purpose = SessionPurpose.Recover)
    {
        ScanSessionViewModel session = CreateSession();
        await (purpose is SessionPurpose.Check ? session.StartCheckCommand : session.StartScanningCommand)
            .ExecuteAsync(null);

        return session;
    }

    /// <summary>Waits for the verification a check starts by itself when its last code arrives.</summary>
    private static async Task VerificationAsync(ScanSessionViewModel session)
    {
        Task? verification = session.VerifyCommand.ExecutionTask;
        Assert.NotNull(verification);

        await verification;
    }

    private async Task<ScanSessionViewModel> CompleteSessionAsync(byte[] content)
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(content);
        ScanSessionViewModel session = await ScanningSessionAsync();
        _scanner.Show([.. backup.Codes]);
        Assert.Equal(ScanState.Complete, session.State);

        return session;
    }

    /// <summary>
    /// A complete session whose metadata code records a hash the data codes do not have — the one way to reach a
    /// mismatch, since a correct encoder never produces one.
    /// </summary>
    private async Task<(ScanSessionViewModel Session, EncodedBackup Backup)> MismatchedSessionAsync(
        SessionPurpose purpose = SessionPurpose.Recover)
    {
        EncodedBackup backup = await BackupFixtures.EncodeAsync(MismatchedContent);
        Assert.False(backup.Metadata.IsCompressed);
        string doctored = BackupFixtures.MetadataCodeWith(backup, backup.Metadata with { Sha256Hex = WrongHash });

        ScanSessionViewModel session = await ScanningSessionAsync(purpose);
        _scanner.Show([doctored, .. backup.Codes.Skip(1)]);
        Assert.Equal(ScanState.Complete, session.State);

        return (session, backup);
    }

    private static IReadOnlyList<string> Shuffled(IReadOnlyList<string> codes)
    {
        List<string> shuffled = [.. codes];
        Random random = new(79);
        for (int index = shuffled.Count - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        return shuffled;
    }

    private static string Format(string format, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, format, arguments);
}
