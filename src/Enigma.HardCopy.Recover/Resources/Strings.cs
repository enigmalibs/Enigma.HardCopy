using System.Globalization;
using System.Resources;

namespace Enigma.HardCopy.Recover.Resources;

/// <summary>
/// Every user-facing string of the application, read from <c>Strings.resx</c>.
/// </summary>
/// <remarks>
/// <para>
/// Written by hand rather than generated, as in the desktop app: the generated accessor is
/// <see langword="internal"/> and lands in the intermediate output, which makes it awkward to reach from XAML and
/// impossible to review; one line per string keeps the surface public, documented and diffable.
/// </para>
/// <para>
/// Every property name <b>is</b> its resource key, and <see cref="Get"/> throws when a key is absent, so a string
/// missing from the <c>.resx</c> is a loud failure rather than a blank label. The accompanying test walks this
/// class by reflection and reads every property, which turns that failure into a build-time one.
/// </para>
/// <para>
/// Names ending in <c>Format</c> are composite format strings, and their arguments are documented in the
/// <c>.resx</c> comment beside each one.
/// </para>
/// </remarks>
public static class Strings
{
    private const string ResourceBaseName = "Enigma.HardCopy.Recover.Resources.Strings";

    private static readonly ResourceManager _resources = new(ResourceBaseName, typeof(Strings).Assembly);

    // ===== Shell =====

    /// <summary>The application's name, shown in its header.</summary>
    public static string AppTitle => Get(nameof(AppTitle));

    /// <summary>The line under the name saying what the application is for.</summary>
    public static string AppSubtitle => Get(nameof(AppSubtitle));

    // ===== Introduction =====

    /// <summary>The paragraph explaining what a recovery involves.</summary>
    public static string IntroText => Get(nameof(IntroText));

    /// <summary>What the application keeps on the phone — nothing — and what it blocks.</summary>
    public static string PrivacyNote => Get(nameof(PrivacyNote));

    // ===== Scan session =====

    /// <summary>The button that turns the camera on, before any code was read.</summary>
    public static string StartScanning => Get(nameof(StartScanning));

    /// <summary>The same button once the session holds codes.</summary>
    public static string ResumeScanning => Get(nameof(ResumeScanning));

    /// <summary>The button that pauses the camera, keeping the codes read.</summary>
    public static string StopScanning => Get(nameof(StopScanning));

    /// <summary>The button that discards the session and starts another.</summary>
    public static string NewSession => Get(nameof(NewSession));

    /// <summary>The button that rebuilds and saves the file.</summary>
    public static string SaveFile => Get(nameof(SaveFile));

    /// <summary>The button that saves bytes which failed their hash, after confirmation.</summary>
    public static string SaveAnyway => Get(nameof(SaveAnyway));

    /// <summary>Shown while the camera is being started — the permission prompt may be up.</summary>
    public static string CameraStarting => Get(nameof(CameraStarting));

    /// <summary>How to scan, shown over the camera area.</summary>
    public static string ScanningHint => Get(nameof(ScanningHint));

    /// <summary>Why nothing scans when the permission was refused, and how to undo that.</summary>
    public static string CameraPermissionDenied => Get(nameof(CameraPermissionDenied));

    /// <summary>Why nothing scans when there is no usable camera.</summary>
    public static string CameraUnavailable => Get(nameof(CameraUnavailable));

    /// <summary>Announced when the last missing code arrives.</summary>
    public static string SessionComplete => Get(nameof(SessionComplete));

    /// <summary>The camera preview's name, for screen readers.</summary>
    public static string CameraPreviewName => Get(nameof(CameraPreviewName));

    /// <summary>The caption of the last-code line.</summary>
    public static string LastScanLabel => Get(nameof(LastScanLabel));

    // ===== Progress =====

    /// <summary>The progress card's heading.</summary>
    public static string ProgressHeader => Get(nameof(ProgressHeader));

    /// <summary>The caption of the backup ID.</summary>
    public static string BackupIdLabel => Get(nameof(BackupIdLabel));

    /// <summary>The caption of the recorded file name.</summary>
    public static string FileNameLabel => Get(nameof(FileNameLabel));

    /// <summary>The caption of the recorded file size.</summary>
    public static string FileSizeLabel => Get(nameof(FileSizeLabel));

    /// <summary>The caption of the recorded hash.</summary>
    public static string ExpectedSha256Label => Get(nameof(ExpectedSha256Label));

    /// <summary>The caption of the missing-index list.</summary>
    public static string MissingLabel => Get(nameof(MissingLabel));

    /// <summary>Data codes received out of the total.</summary>
    public static string CodesProgressFormat => Get(nameof(CodesProgressFormat));

    /// <summary>Shown while no code has been read, so the total is not known yet.</summary>
    public static string CodesProgressUnknown => Get(nameof(CodesProgressUnknown));

    /// <summary>The metadata code is in.</summary>
    public static string MetadataReceived => Get(nameof(MetadataReceived));

    /// <summary>The metadata code is not in yet, and why it matters.</summary>
    public static string MetadataMissing => Get(nameof(MetadataMissing));

    /// <summary>The listed ranges, followed by how many indexes were left out.</summary>
    public static string MissingOverflowFormat => Get(nameof(MissingOverflowFormat));

    /// <summary>A file size in bytes.</summary>
    public static string FileSizeFormat => Get(nameof(FileSizeFormat));

    /// <summary>Stands in for a value that is not known.</summary>
    public static string UnknownValue => Get(nameof(UnknownValue));

    // ===== Code outcomes =====

    /// <summary>The metadata code was read.</summary>
    public static string CodeAcceptedMetadata => Get(nameof(CodeAcceptedMetadata));

    /// <summary>A data code was read.</summary>
    public static string CodeAcceptedFormat => Get(nameof(CodeAcceptedFormat));

    /// <summary>A code already in the session, under another spelling.</summary>
    public static string CodeDuplicateFormat => Get(nameof(CodeDuplicateFormat));

    /// <summary>A code that failed its own checksum.</summary>
    public static string CodeBadCrcFormat => Get(nameof(CodeBadCrcFormat));

    /// <summary>A code that contradicts one already read.</summary>
    public static string CodeConflictFormat => Get(nameof(CodeConflictFormat));

    /// <summary>A code from another backup, naming both backups.</summary>
    public static string CodeWrongBackupFormat => Get(nameof(CodeWrongBackupFormat));

    /// <summary>A QR code that is not one of ours at all.</summary>
    public static string CodeMalformed => Get(nameof(CodeMalformed));

    /// <summary>A code with an index whose content is not valid.</summary>
    public static string CodeMalformedFormat => Get(nameof(CodeMalformedFormat));

    // ===== Rebuilding the file =====

    /// <summary>The file was rebuilt and verified.</summary>
    public static string AssemblyVerified => Get(nameof(AssemblyVerified));

    /// <summary>The file was rebuilt but its hash does not match.</summary>
    public static string AssemblyHashMismatch => Get(nameof(AssemblyHashMismatch));

    /// <summary>The same, with both hashes.</summary>
    public static string AssemblyHashMismatchFormat => Get(nameof(AssemblyHashMismatchFormat));

    /// <summary>The backup is marked as encrypted.</summary>
    public static string AssemblyEncryptionUnsupported => Get(nameof(AssemblyEncryptionUnsupported));

    /// <summary>The chunks do not decompress.</summary>
    public static string AssemblyDecompressionFailed => Get(nameof(AssemblyDecompressionFailed));

    /// <summary>Codes are still missing.</summary>
    public static string AssemblyIncomplete => Get(nameof(AssemblyIncomplete));

    // ===== Saving =====

    /// <summary>The save picker's title.</summary>
    public static string SavePickerTitle => Get(nameof(SavePickerTitle));

    /// <summary>The save picker's one file type.</summary>
    public static string SavePickerFileType => Get(nameof(SavePickerFileType));

    /// <summary>The name offered when the backup recorded none.</summary>
    public static string DefaultFileName => Get(nameof(DefaultFileName));

    /// <summary>A verified file was saved.</summary>
    public static string SavedFormat => Get(nameof(SavedFormat));

    /// <summary>Unverified bytes were saved, on the user's explicit decision.</summary>
    public static string SavedUnverifiedFormat => Get(nameof(SavedUnverifiedFormat));

    /// <summary>The destination could not be written.</summary>
    public static string SaveFailedFormat => Get(nameof(SaveFailedFormat));

    // ===== Notifications =====

    /// <summary>The info bar's title for an information.</summary>
    public static string NotificationInformationTitle => Get(nameof(NotificationInformationTitle));

    /// <summary>The info bar's title for a success.</summary>
    public static string NotificationSuccessTitle => Get(nameof(NotificationSuccessTitle));

    /// <summary>The info bar's title for a warning.</summary>
    public static string NotificationWarningTitle => Get(nameof(NotificationWarningTitle));

    /// <summary>The info bar's title for a failure.</summary>
    public static string NotificationErrorTitle => Get(nameof(NotificationErrorTitle));

    // ===== Confirmations =====

    /// <summary>The title of the dialog guarding an unverified save.</summary>
    public static string ConfirmUnverifiedSaveTitle => Get(nameof(ConfirmUnverifiedSaveTitle));

    /// <summary>The prose explaining what an unverified save writes.</summary>
    public static string ConfirmUnverifiedSaveBody => Get(nameof(ConfirmUnverifiedSaveBody));

    /// <summary>The caption of the recorded hash in that dialog.</summary>
    public static string ConfirmUnverifiedSaveExpectedLabel => Get(nameof(ConfirmUnverifiedSaveExpectedLabel));

    /// <summary>The caption of the rebuilt bytes' hash in that dialog.</summary>
    public static string ConfirmUnverifiedSaveActualLabel => Get(nameof(ConfirmUnverifiedSaveActualLabel));

    /// <summary>The button that writes the unverified bytes.</summary>
    public static string ConfirmUnverifiedSaveConfirm => Get(nameof(ConfirmUnverifiedSaveConfirm));

    /// <summary>The button that does not.</summary>
    public static string ConfirmUnverifiedSaveCancel => Get(nameof(ConfirmUnverifiedSaveCancel));

    /// <summary>The title of the dialog guarding the discarding of a session.</summary>
    public static string ConfirmNewSessionTitle => Get(nameof(ConfirmNewSessionTitle));

    /// <summary>The prose explaining what a new session throws away.</summary>
    public static string ConfirmNewSessionBody => Get(nameof(ConfirmNewSessionBody));

    /// <summary>The button that discards the session.</summary>
    public static string ConfirmNewSessionConfirm => Get(nameof(ConfirmNewSessionConfirm));

    /// <summary>The button that keeps it.</summary>
    public static string ConfirmNewSessionCancel => Get(nameof(ConfirmNewSessionCancel));

    /// <summary>
    /// Reads one string from the resources. Throws rather than returning a placeholder: a key that is not there
    /// is a defect in this file, and a blank label in the running application would hide it.
    /// </summary>
    /// <param name="name">The resource key, which is always the calling property's own name.</param>
    /// <returns>The string for the current UI culture.</returns>
    /// <exception cref="MissingManifestResourceException">The key is not in the resources.</exception>
    private static string Get(string name)
        => _resources.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"'{name}' is missing from {ResourceBaseName}.");
}
