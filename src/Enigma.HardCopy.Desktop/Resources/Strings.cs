using System.Globalization;
using System.Resources;

namespace Enigma.HardCopy.Desktop.Resources;

/// <summary>
/// Every user-facing string of the application, read from <c>Strings.resx</c>.
/// </summary>
/// <remarks>
/// <para>
/// Written by hand rather than generated. The generated accessor is <see langword="internal"/> and lands in
/// the intermediate output, which makes it awkward to reach from XAML and impossible to review; one line per
/// string costs nothing and keeps the surface public, documented and diffable.
/// </para>
/// <para>
/// Every property name <b>is</b> its resource key, and <see cref="Get"/> throws when a key is absent, so a
/// string missing from the <c>.resx</c> is a loud failure rather than a blank label. The accompanying test
/// walks this class by reflection and reads every property, which turns that failure into a build-time one.
/// </para>
/// <para>
/// Names ending in <c>Format</c> are composite format strings, and their arguments are documented in the
/// <c>.resx</c> comment beside each one.
/// </para>
/// </remarks>
public static class Strings
{
    private const string ResourceBaseName = "Enigma.HardCopy.Desktop.Resources.Strings";

    private static readonly ResourceManager _resources = new(ResourceBaseName, typeof(Strings).Assembly);

    // ===== Shell =====

    /// <summary>The application's name, used as the window title.</summary>
    public static string AppTitle => Get(nameof(AppTitle));

    /// <summary>The navigation label of the backup view.</summary>
    public static string NavBackup => Get(nameof(NavBackup));

    /// <summary>The navigation label of the recovery view.</summary>
    public static string NavRecover => Get(nameof(NavRecover));

    // ===== Backup view =====

    /// <summary>The backup view's heading.</summary>
    public static string BackupHeader => Get(nameof(BackupHeader));

    /// <summary>The paragraph explaining what a backup is.</summary>
    public static string BackupIntro => Get(nameof(BackupIntro));

    /// <summary>The heading of the file-selection step.</summary>
    public static string BackupSourceHeader => Get(nameof(BackupSourceHeader));

    /// <summary>The label of the button that opens the file picker.</summary>
    public static string BackupChooseFile => Get(nameof(BackupChooseFile));

    /// <summary>Shown while no file has been chosen.</summary>
    public static string BackupNoFileChosen => Get(nameof(BackupNoFileChosen));

    /// <summary>The label of the chosen file's name.</summary>
    public static string BackupFileLabel => Get(nameof(BackupFileLabel));

    /// <summary>The label of the chosen file's size.</summary>
    public static string BackupSizeLabel => Get(nameof(BackupSizeLabel));

    /// <summary>The label of the chosen file's SHA-256.</summary>
    public static string BackupSha256Label => Get(nameof(BackupSha256Label));

    /// <summary>Formats a byte count.</summary>
    public static string BackupBytesFormat => Get(nameof(BackupBytesFormat));

    /// <summary>The heading of the barcode-density step.</summary>
    public static string BackupSettingsHeader => Get(nameof(BackupSettingsHeader));

    /// <summary>The label of the chunk-size selector.</summary>
    public static string BackupChunkSizeLabel => Get(nameof(BackupChunkSizeLabel));

    /// <summary>The description of the small chunk-size preset.</summary>
    public static string BackupChunkSizeSmall => Get(nameof(BackupChunkSizeSmall));

    /// <summary>The description of the medium chunk-size preset.</summary>
    public static string BackupChunkSizeMedium => Get(nameof(BackupChunkSizeMedium));

    /// <summary>The description of the large chunk-size preset.</summary>
    public static string BackupChunkSizeLarge => Get(nameof(BackupChunkSizeLarge));

    /// <summary>The note that the chunk-size choice is not persisted.</summary>
    public static string BackupChunkSizeNote => Get(nameof(BackupChunkSizeNote));

    /// <summary>The label of the page and code estimate.</summary>
    public static string BackupEstimateLabel => Get(nameof(BackupEstimateLabel));

    /// <summary>Formats the page and code estimate.</summary>
    public static string BackupEstimateFormat => Get(nameof(BackupEstimateFormat));

    /// <summary>Formats the warning shown for an input that needs many pages.</summary>
    public static string BackupLargeInputWarningFormat => Get(nameof(BackupLargeInputWarningFormat));

    /// <summary>Formats the message shown when the layout cannot be computed at all.</summary>
    public static string BackupEstimateFailedFormat => Get(nameof(BackupEstimateFailedFormat));

    /// <summary>The heading of the destination step.</summary>
    public static string BackupOutputHeader => Get(nameof(BackupOutputHeader));

    /// <summary>The label of the button that opens the save picker.</summary>
    public static string BackupChooseOutput => Get(nameof(BackupChooseOutput));

    /// <summary>Shown while no destination has been chosen.</summary>
    public static string BackupNoOutputChosen => Get(nameof(BackupNoOutputChosen));

    /// <summary>The label of the chosen destination.</summary>
    public static string BackupOutputLabel => Get(nameof(BackupOutputLabel));

    /// <summary>Formats the file name suggested in the save picker.</summary>
    public static string BackupDefaultPdfNameFormat => Get(nameof(BackupDefaultPdfNameFormat));

    /// <summary>The label of the button that produces the PDF.</summary>
    public static string BackupGenerate => Get(nameof(BackupGenerate));

    /// <summary>The label of the button that cancels generation.</summary>
    public static string BackupCancel => Get(nameof(BackupCancel));

    /// <summary>The progress text while the file is being read.</summary>
    public static string BackupStageReading => Get(nameof(BackupStageReading));

    /// <summary>The progress text while the codes are being encoded.</summary>
    public static string BackupStageEncoding => Get(nameof(BackupStageEncoding));

    /// <summary>The progress text while the PDF is being composed.</summary>
    public static string BackupStageComposing => Get(nameof(BackupStageComposing));

    /// <summary>The progress text while the PDF is being written.</summary>
    public static string BackupStageWriting => Get(nameof(BackupStageWriting));

    /// <summary>Formats the message shown after a PDF has been written.</summary>
    public static string BackupSuccessFormat => Get(nameof(BackupSuccessFormat));

    /// <summary>Shown after generation was cancelled.</summary>
    public static string BackupCancelled => Get(nameof(BackupCancelled));

    /// <summary>Formats the message shown when the chosen file cannot be read.</summary>
    public static string BackupReadFailedFormat => Get(nameof(BackupReadFailedFormat));

    /// <summary>Formats the message shown when encoding or composition fails.</summary>
    public static string BackupGenerateFailedFormat => Get(nameof(BackupGenerateFailedFormat));

    /// <summary>Formats the message shown when the PDF cannot be written.</summary>
    public static string BackupWriteFailedFormat => Get(nameof(BackupWriteFailedFormat));

    // ===== Recover view =====

    /// <summary>The recovery view's heading.</summary>
    public static string RecoverHeader => Get(nameof(RecoverHeader));

    /// <summary>The paragraph explaining how a recovery proceeds.</summary>
    public static string RecoverIntro => Get(nameof(RecoverIntro));

    /// <summary>The heading of the image-import panel.</summary>
    public static string RecoverImportHeader => Get(nameof(RecoverImportHeader));

    /// <summary>The label of the button that opens the image picker.</summary>
    public static string RecoverImportImages => Get(nameof(RecoverImportImages));

    /// <summary>The hint that images may be dropped onto the view.</summary>
    public static string RecoverDropHint => Get(nameof(RecoverDropHint));

    /// <summary>The heading of the manual-entry panel.</summary>
    public static string RecoverManualHeader => Get(nameof(RecoverManualHeader));

    /// <summary>The hint describing what may be typed or pasted.</summary>
    public static string RecoverManualHint => Get(nameof(RecoverManualHint));

    /// <summary>The placeholder shown in the empty manual-entry box.</summary>
    public static string RecoverManualPlaceholder => Get(nameof(RecoverManualPlaceholder));

    /// <summary>The label of the button that offers typed codes to the session.</summary>
    public static string RecoverAddCodes => Get(nameof(RecoverAddCodes));

    /// <summary>The heading of the progress panel.</summary>
    public static string RecoverStatusHeader => Get(nameof(RecoverStatusHeader));

    /// <summary>Shown while the session holds no code at all.</summary>
    public static string RecoverNothingYet => Get(nameof(RecoverNothingYet));

    /// <summary>The label of the recovered backup's ID.</summary>
    public static string RecoverBackupIdLabel => Get(nameof(RecoverBackupIdLabel));

    /// <summary>The label of the recovered file's name.</summary>
    public static string RecoverFileLabel => Get(nameof(RecoverFileLabel));

    /// <summary>The label of the hash recorded in the backup.</summary>
    public static string RecoverExpectedSha256Label => Get(nameof(RecoverExpectedSha256Label));

    /// <summary>Explains what is unknown while the metadata code is missing.</summary>
    public static string RecoverMetadataMissing => Get(nameof(RecoverMetadataMissing));

    /// <summary>The label of the received-versus-expected code count.</summary>
    public static string RecoverChunksLabel => Get(nameof(RecoverChunksLabel));

    /// <summary>Formats the received-versus-expected code count.</summary>
    public static string RecoverChunksFormat => Get(nameof(RecoverChunksFormat));

    /// <summary>Shown as the code count before any code has been read.</summary>
    public static string RecoverChunksUnknown => Get(nameof(RecoverChunksUnknown));

    /// <summary>The label of the missing-code list.</summary>
    public static string RecoverMissingLabel => Get(nameof(RecoverMissingLabel));

    /// <summary>Shown as the missing-code list before any code has been read.</summary>
    public static string RecoverMissingNothingKnown => Get(nameof(RecoverMissingNothingKnown));

    /// <summary>Shown as the missing-code list once every code is in.</summary>
    public static string RecoverMissingNone => Get(nameof(RecoverMissingNone));

    /// <summary>Formats a missing-code list too long to print in full.</summary>
    public static string RecoverMissingOverflowFormat => Get(nameof(RecoverMissingOverflowFormat));

    /// <summary>Shown once every code needed to rebuild the file is present.</summary>
    public static string RecoverCompleteHint => Get(nameof(RecoverCompleteHint));

    /// <summary>The label of the button that rebuilds the file.</summary>
    public static string RecoverAssemble => Get(nameof(RecoverAssemble));

    /// <summary>The label of the button that saves an unverified recovery.</summary>
    public static string RecoverSaveAnyway => Get(nameof(RecoverSaveAnyway));

    /// <summary>The label of the button that discards the session.</summary>
    public static string RecoverStartOver => Get(nameof(RecoverStartOver));

    /// <summary>The heading of the activity log.</summary>
    public static string RecoverLogHeader => Get(nameof(RecoverLogHeader));

    /// <summary>The progress text while images are being read.</summary>
    public static string RecoverStageImporting => Get(nameof(RecoverStageImporting));

    /// <summary>The progress text while the file is being rebuilt.</summary>
    public static string RecoverStageAssembling => Get(nameof(RecoverStageAssembling));

    /// <summary>The progress text while the recovered file is being written.</summary>
    public static string RecoverStageWriting => Get(nameof(RecoverStageWriting));

    /// <summary>Formats the message shown after a verified recovery was saved.</summary>
    public static string RecoverSavedFormat => Get(nameof(RecoverSavedFormat));

    /// <summary>Formats the message shown after an unverified recovery was saved anyway.</summary>
    public static string RecoverSavedUnverifiedFormat => Get(nameof(RecoverSavedUnverifiedFormat));

    /// <summary>Formats the log line of an imported file that is not an image.</summary>
    public static string RecoverImageUnreadableFormat => Get(nameof(RecoverImageUnreadableFormat));

    /// <summary>Formats the log line of a readable image carrying no code.</summary>
    public static string RecoverImageNoCodesFormat => Get(nameof(RecoverImageNoCodesFormat));

    /// <summary>Formats the log line summarising one scanned image.</summary>
    public static string RecoverImageScannedFormat => Get(nameof(RecoverImageScannedFormat));

    /// <summary>Formats the log line of an image that could not be opened.</summary>
    public static string RecoverImageFailedFormat => Get(nameof(RecoverImageFailedFormat));

    /// <summary>Shown when the manual-entry box is empty.</summary>
    public static string RecoverManualEmpty => Get(nameof(RecoverManualEmpty));

    /// <summary>Shown when the manual-entry box holds no recognisable code.</summary>
    public static string RecoverManualNoCodesFound => Get(nameof(RecoverManualNoCodesFound));

    /// <summary>Formats the message shown when the recovered file cannot be written.</summary>
    public static string RecoverWriteFailedFormat => Get(nameof(RecoverWriteFailedFormat));

    /// <summary>Formats the blocking warning shown when the rebuilt file's hash does not match.</summary>
    public static string RecoverMismatchWarningFormat => Get(nameof(RecoverMismatchWarningFormat));

    /// <summary>The file name suggested when the metadata code did not supply one.</summary>
    public static string RecoverDefaultFileName => Get(nameof(RecoverDefaultFileName));

    // ===== One code's outcome =====

    /// <summary>Reports that the metadata code was accepted.</summary>
    public static string CodeAcceptedMetadata => Get(nameof(CodeAcceptedMetadata));

    /// <summary>Formats the report of an accepted data code.</summary>
    public static string CodeAcceptedFormat => Get(nameof(CodeAcceptedFormat));

    /// <summary>Formats the report of a code the session already held.</summary>
    public static string CodeDuplicateFormat => Get(nameof(CodeDuplicateFormat));

    /// <summary>Formats the report of a code that failed its checksum.</summary>
    public static string CodeBadCrcFormat => Get(nameof(CodeBadCrcFormat));

    /// <summary>Formats the report of a code contradicting one already read.</summary>
    public static string CodeConflictFormat => Get(nameof(CodeConflictFormat));

    /// <summary>Formats the report of a code belonging to a different backup.</summary>
    public static string CodeWrongBackupFormat => Get(nameof(CodeWrongBackupFormat));

    /// <summary>Reports text that is not a code of this format, with no index to name.</summary>
    public static string CodeMalformed => Get(nameof(CodeMalformed));

    /// <summary>Formats the report of a malformed code whose index is known.</summary>
    public static string CodeMalformedFormat => Get(nameof(CodeMalformedFormat));

    // ===== Assembly outcome =====

    /// <summary>Reports that codes are still missing.</summary>
    public static string AssemblyIncomplete => Get(nameof(AssemblyIncomplete));

    /// <summary>Reports a backup whose encryption flag this version cannot undo.</summary>
    public static string AssemblyEncryptionUnsupported => Get(nameof(AssemblyEncryptionUnsupported));

    /// <summary>Reports chunks that do not decompress.</summary>
    public static string AssemblyDecompressionFailed => Get(nameof(AssemblyDecompressionFailed));

    /// <summary>Reports a rebuilt file whose hash does not match the backup.</summary>
    public static string AssemblyHashMismatch => Get(nameof(AssemblyHashMismatch));

    /// <summary>Reports a rebuilt file whose hash matches the backup.</summary>
    public static string AssemblyVerified => Get(nameof(AssemblyVerified));

    // ===== File dialogs =====

    /// <summary>The title of the picker that chooses the file to back up.</summary>
    public static string DialogChooseFileTitle => Get(nameof(DialogChooseFileTitle));

    /// <summary>The title of the picker that chooses scanned pages.</summary>
    public static string DialogChooseImagesTitle => Get(nameof(DialogChooseImagesTitle));

    /// <summary>The title of the picker that chooses where to write the PDF.</summary>
    public static string DialogSavePdfTitle => Get(nameof(DialogSavePdfTitle));

    /// <summary>The title of the picker that chooses where to write the recovered file.</summary>
    public static string DialogSaveRecoveredTitle => Get(nameof(DialogSaveRecoveredTitle));

    /// <summary>The name of the image file-type filter.</summary>
    public static string DialogFilterImages => Get(nameof(DialogFilterImages));

    /// <summary>The name of the PDF file-type filter.</summary>
    public static string DialogFilterPdf => Get(nameof(DialogFilterPdf));

    /// <summary>The name of the unrestricted file-type filter.</summary>
    public static string DialogFilterAllFiles => Get(nameof(DialogFilterAllFiles));

    /// <summary>
    /// Reads one string from the resources. Throws rather than returning a placeholder: a key that is not
    /// there is a defect in this file, and a blank label in the running application would hide it.
    /// </summary>
    /// <param name="name">The resource key, which is always the calling property's own name.</param>
    /// <returns>The string for the current UI culture.</returns>
    /// <exception cref="MissingManifestResourceException">The key is not in the resources.</exception>
    private static string Get(string name)
        => _resources.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"'{name}' is missing from {ResourceBaseName}.");
}
