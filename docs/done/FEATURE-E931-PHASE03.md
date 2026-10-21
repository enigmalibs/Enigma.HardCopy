# FEATURE-E931-PHASE03 — Scan session page & save flow — DONE

**Branch:** `feature/feature-e931-phase03-scan-session`
**Date:** 2026-10-19
**Run:** feature/2026-10-19-android-recover-app

## Summary

The app's one screen now works end to end, everything except the camera itself. `ScanSessionViewModel` drives one
Core `RecoverySession`:

- **Start / pause / resume scanning** through the `ICameraScanner` seam. A refused permission or a missing camera
  is explained on the page and can be retried. The start button reads *Start scanning*, or *Resume scanning* once
  the session holds codes.
- **Codes in any order.** Every code text the camera reports goes into the session **once**. A per-session set of
  texts already handled makes a code held in view (reported on every frame) a no-op, so haptics do not stutter and
  a warning is not repeated.
- **Different files are refused.** The session adopts the backup from the first verified code. A code from
  another backup is refused with **one** warning banner naming both backup IDs ("…if ABCD is the wrong backup,
  start a new session") and a reject haptic. Damaged codes and foreign QR codes are refused the same way, as
  errors.
- **Progress.** The card shows:
  - *N of T data codes*, plus whether the metadata code (index 0) is in.
  - A bar counting the metadata code as one more code, so it never reads 100 % without it.
  - Missing indexes as compressed ranges (`1, 3–9, 12`), the tail summarised after 8 ranges.
  - Backup ID, then file name, size and recorded SHA-256 once the metadata code is in.
  - The last code read, coloured by severity.
- **Completion** stops the camera by itself and announces it. *Save the recovered file* rebuilds the file off the
  UI thread, checks the hash and opens the system save picker (Storage Access Framework: phone storage, Google
  Drive or any document provider) with the recorded name, then writes the bytes.
- **Hash mismatch.** As on Desktop: nothing is written. A warning shows both hashes, and *Save anyway* asks for
  confirmation in a `ContentDialog` naming both hashes before the picker opens.
- **New session** asks first only when codes are held. It forgets the session, the texts seen and any rebuilt
  bytes, then turns the camera straight back on.

The default `UnavailableCameraScanner` / `NoHapticFeedback` are registered with `TryAdd` after the platform head
has registered its own, so the app runs — and explains why nothing scans — until PHASE04 brings the camera.

## Files/modules touched

**Created** (`src/Enigma.HardCopy.Recover/`)

- `Services/ICameraScanner.cs`, `CameraStartResult.cs`, `CodesScannedEventArgs.cs`, `UnavailableCameraScanner.cs`
- `Services/IHapticFeedback.cs`, `NoHapticFeedback.cs`
- `Services/IRecoveredFileSaver.cs`, `ISaveTarget.cs`, `StorageProviderRecoveredFileSaver.cs`,
  `StorageProviderSaveTarget.cs` — the library's picker service, one `application/octet-stream` file type.
- `Services/INotificationService.cs`, `NotificationService.cs` — the library's info bar.
- `Services/IConfirmationService.cs`, `ConfirmationService.cs` — the library's content dialog, prose and hashes
  only, never code text.
- `ViewModels/ScanSessionViewModel.cs`, `ScanState.cs`, `IndexRanges.cs`, `OutcomeMessages.cs`,
  `StatusMessage.cs`, `MessageSeverity.cs`

**Created** (`tests/Enigma.HardCopy.Recover.UnitTests/`)

- `ScanSessionViewModelTests.cs` (22), `IndexRangesTests.cs` (11), `OutcomeMessagesTests.cs` (13),
  `BackupFixtures.cs`, `TestDoubles/` (camera, haptics, saver, save target, notifications, confirmations, backup-ID
  generator).
- `docs/done/FEATURE-E931-PHASE03.md` — this file.

**Modified**

- `App.axaml.cs` — the platform head registers first; the main view gets the session ViewModel as its DataContext.
- `Hosting/ServiceCollectionExtensions.cs` — the session (singleton), the five services, the two `TryAdd` defaults.
- `Views/MainView.axaml` — introduction, camera area (`CameraHost`, guidance text until PHASE04), starting card,
  camera-problem notice, last code read, progress card, actions (primary first, full width, 48 dip).
- `Resources/Strings.resx` + `Strings.cs` (+60 strings), `Resources/AppIcons.cs` (two dialog icons).
- `tests/…/Hosting/ServiceCollectionExtensionsTests.cs` — lifetimes, defaults, platform precedence, and the whole
  ViewModel graph resolved without a rendering platform.
- `docs/roadmap.md`, `docs/plan/FEATURE-E931.md` — statuses.

## Deviations & follow-ups

- **`FrameAvailable` / `CameraFrame` moved to PHASE04.** The plan put the preview-frame contract on
  `ICameraScanner` in this phase. It has no producer or consumer until PHASE04 brings both the CameraX scanner
  and the preview control, so it lands there, with them.
- **A New session starts the camera immediately.** A new session is for scanning, which saves a tap. A user who
  only wants to wipe a finished session sees the camera come on; pausing is one tap.
- **A dismissed save picker is silent.** The Desktop app re-announces the verification outcome when the user
  backs out of the picker. On the phone the *Save* button simply stays there.
- Documentation sweep: nothing stale. The README describes no Android behaviour yet, which is PHASE05.
- Line endings: `.gitattributes` enforces `eol=lf`; nothing to recommend.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| States | `Ready`, `Starting`, `Scanning`, `Complete` + a separate `CameraProblem` text | A refused permission is "ready, with an explanation", not a different screen | Two more enum states for the two problems |
| Where code-by-code feedback goes | Last-code line + haptics. Refusals also go to the info bar | A banner per accepted code would bury the one banner that matters | Info bar for every code |
| Platform default registration | `TryAdd`, platform head registered first | One descriptor per service; the head's implementation wins without `Replace` | Plain `AddSingleton` and "last wins" |
| Save MIME type | `application/octet-stream` | Document providers keep the suggested name as given for an octet stream | Guessing a MIME type from the extension |
| Missing list cap | 8 ranges, then "and N more" | Fits a phone's width; the first ranges are the actionable ones | The Desktop cap of 12 indexes (ranges read better on a phone) |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` (Debug, Android head included): **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.HardCopy.slnx`: **763 passed, 0 failed, 0 skipped** (was 706; +57).
- Fix cycles used: **0 of 3**. The first build and the first test run were green.
- Emulator `Pixel6_API36`, deployed with `-t:Install`:
  - `uiautomator dump` shows the header, the introduction card and a full-width *Start scanning* button
    (bounds `[42,762][1038,888]`, 126 px ≈ 48 dip).
  - Tapping it shows *No camera could be started on this device.* (the PHASE03 default scanner) with the button
    still offered.
  - No error-level log line for the app's process.
