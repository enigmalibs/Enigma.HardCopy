# FEATURE-3941 — Android: check a printed backup — DONE

**Branch:** `feature/feature-3941-check-printed-backup`
**Date:** 2026-10-24
**Run:** feature/2026-10-24-check-printed-backup

## Summary

The Android recovery app now has a second way in, under *Start scanning*: **Check a printed backup**. It scans
exactly as a recovery does — same camera, same `RecoverySession`, same progress, same refusals of damaged and
foreign codes. Once every code is in, it never produces the file:

- The camera stops by itself, as before.
- `VerifyCommand` rebuilds the file in memory off the UI thread (`RecoverySession.TryAssemble`, the same call a
  save makes) and turns the outcome into a verdict.
- The verdict appears in a persistent **Check result** card and in one info-bar banner, which replaces the
  recovery's *Every code is in* banner:
  - *Check passed* (success) — every code was read and the rebuilt file matches the recorded SHA-256;
  - *Check failed* (error) — a hash mismatch naming both hashes, a decompression failure, or an encrypted backup.
- The rebuilt bytes go out of scope with the result. A check never sets the unverified-content slot, so neither
  *Save* nor *Save anyway* can appear, and nothing reaches the save picker.

What a session is for is a `SessionPurpose` (`Recover` / `Check`):

- Whichever start button is pressed on an **empty** session sets it.
- *Resume scanning* keeps it.
- The check button is offered only while the camera is off and the session holds no code.
- While checking, the progress card's heading reads *Checking a printed backup*.
- *New session* after a check checks again and clears the verdict.

"Well printed" is measured by the scan itself. A code that will not decode stays in *Still missing*, so the check
never completes. A code that decodes but fails its CRC-32 is refused as damaged.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Recover/ViewModels/SessionPurpose.cs`
- `docs/done/FEATURE-3941.md` — this file.

**Modified**

- `src/Enigma.HardCopy.Recover/ViewModels/ScanSessionViewModel.cs`:
  - new members `Purpose`, `IsChecking`, `ProgressTitle`, `CanStartCheck`, `CanSave`, `CheckVerdict` /
    `HasCheckVerdict`, `StartCheckCommand` and `VerifyCommand`;
  - *Start scanning* sets the purpose only on an empty session;
  - completion branches on the purpose;
  - *New session* clears the verdict and keeps the purpose.
- `src/Enigma.HardCopy.Recover/ViewModels/ScanState.cs` — `Complete` documented for both purposes.
- `src/Enigma.HardCopy.Recover/ViewModels/OutcomeMessages.cs` — `DescribeCheck(AssemblyResult)`.
- `src/Enigma.HardCopy.Recover/Resources/Strings.resx` + `Strings.cs`:
  - a new *Checking a printout* section with 10 strings;
  - `CameraPermissionDenied` reworded.
- `src/Enigma.HardCopy.Recover/Views/MainView.axaml`:
  - the check paragraph in the introduction card;
  - the *Check result* card;
  - the progress heading bound to `ProgressTitle`;
  - the *Check a printed backup* button;
  - *Save* bound to `CanSave`.
- `tests/Enigma.HardCopy.Recover.UnitTests/ScanSessionViewModelTests.cs`:
  - 8 new tests;
  - initial-state and recovery-completion assertions extended;
  - helpers take a purpose, and a new `VerificationAsync` awaits the automatic verification.
- `tests/Enigma.HardCopy.Recover.UnitTests/OutcomeMessagesTests.cs` — 3 new tests, plus the null-argument test
  extended.
- `README.md`, *Recovering on Android* — the *Checking a printout* paragraph (plan step 7).
- `docs/roadmap.md`, `docs/plan/FEATURE-3941.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Wording of `CameraPermissionDenied` | "Tap the same button again to be asked once more" | A paused session holding codes shows *Resume scanning*, which neither button name matches; the generic wording is right in every state | Naming both start buttons, as the plan said (wrong for a resumed session) |
| Where the verdict card sits | Right after the camera notice, above the last-code line | After a check the verdict is the one thing to read; the last code ("Code 12 read.") is noise by then | Below the progress card (pushed off a phone screen by the card) |
| Haptic for the verdict | None | The last accepted code already ticks; a second pattern immediately after would feel like a refusal | Accept/reject pattern on the verdict |
| `IsComplete` | Kept public, still raised, no longer bound by the view (*Save* binds `CanSave`) | It belongs to the `IsReady` / `IsStarting` / `IsScanning` family derived from `State`, and still means "every code is in" | Removing it |
| `VerifyCommand.CanExecute` | `Complete` and `Check` and not busy, even though only code executes it | Keeps the command honest if a view ever binds it, and the state guard documents when verifying makes sense | No predicate |
| Placement of the new strings | One *Checking a printout* section in both the `.resx` and `Strings.cs` | The feature reads as one block in review and in the resource file | Spreading them over the existing sections |
| Progress bar's accessible name | Bound to `ProgressTitle`, like the heading | A screen reader hears that the session is a check | Leaving it on the static *Progress* |

## Deviations & follow-ups

- **Build warning `XA0033` (environmental).** The Android SDK tooling cannot parse this machine's JDK version
  string (`openjdk version "21.0.13.0.2"`, a build dated 2026-10-22). It appears on any build of the Android head
  here, whatever the source code. Every project compiles with zero compiler or analyzer warnings, and the warning
  is not one this change can fix. Follow-up: point `JavaSdkDirectory` at a JDK whose version string the workload
  parses, or update the `android` workload.
- **Not observed: a check of a real printout on a physical phone.** The emulator has no printed codes. The scan
  pipeline is unchanged from FEATURE-E931 and carries that item's same caveat. The verdict logic is covered by
  unit tests over real encoder output.
- The camera-permission wording deviates from the plan (see *Decisions taken at build time*).
- Possible follow-up: a matching check mode on the desktop *Recover* page, if wanted.
- Line endings: every touched file is LF, as `.gitattributes` requires; nothing to recommend.
- Documentation sweep:
  - `README.md` gained the planned paragraph;
  - `docs/android.md` — still accurate; its troubleshooting row quotes only the unchanged start of the
    permission message;
  - `RELEASENOTES.md` — still accurate; the Android app has no release-notes section by FEATURE-E931's decision;
  - no other prose doc mentions the start flow.

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` — build succeeded, 0 errors. Zero compiler/analyzer warnings; the one
  warning is the environmental `XA0033` above.
- `dotnet test --solution Enigma.HardCopy.slnx` — **796 passed, 0 failed, 0 skipped** (Core, Desktop, Recover).
  That includes 11 new tests:
  - `StartCheck_TurnsTheCameraOn_ForACheck`;
  - `ACompleteCheck_VerifiesTheHash_AndSavesNothing`;
  - `ACheck_OnAHashMismatch_FailsNamingBothHashes_AndOffersNothingToSave`;
  - `ACheckOfAnEmptyFilesBackup_PassesWithTheMetadataCodeAlone`;
  - `StartCheck_IsOnlyOfferedWhileTheSessionIsEmpty`;
  - `ResumingAPausedCheck_CarriesOnChecking`;
  - `StartScanning_AfterACheckThatReadNothing_MakesTheSessionARecovery`;
  - `NewSession_AfterACheck_ForgetsTheVerdict_AndChecksAgain`;
  - `DescribeCheck_EveryAssemblyOutcome_HasASentence`;
  - `DescribeCheck_AVerifiedFile_IsTheOnlyPass_AndEveryOtherOutcomeAFailure`;
  - `DescribeCheck_AHashMismatch_IsAFailureShowingBothHashes`.

  The existing recovery tests are unchanged and green. The `StringsTests` reflection walk covers the new strings.
- Emulator smoke check (`Pixel_6_API_36`, headless, `dotnet build src/Enigma.HardCopy.Recover.Android -t:Install
  -f net10.0-android`, inspected with `uiautomator dump`):
  1. The start card's accessibility tree shows the check paragraph and both *Start scanning* and *Check a
     printed backup*.
  2. Tapping *Check a printed backup* raised the camera permission prompt.
  3. Granting it showed the camera preview and *Pause scanning*.
  4. Pausing the still-empty check brought both start buttons back.
  5. The crash log stayed empty throughout.
