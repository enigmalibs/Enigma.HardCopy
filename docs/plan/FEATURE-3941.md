# FEATURE-3941 — Android: check a printed backup

**Status:** TODO
**Type:** single-phase feature
**Branch:** `feature/feature-3941-check-printed-backup`
**Run:** feature/2026-10-24-check-printed-backup

## Objective

Give the Android recovery app (`Enigma.HardCopy.Recover`) a second way in, next to *Start scanning*:
**Check a printed backup**. It scans exactly as a recovery does — same camera, same `RecoverySession`, same
progress, same refusals — but when the last code arrives it does **not** produce the recovered file. It rebuilds
the file in memory, checks it against the SHA-256 recorded in the backup, shows a pass/fail verdict and forgets the
bytes. The point is to prove that a freshly printed backup is readable (every code scans) and intact (the codes
rebuild the file the backup describes) before the paper is filed away.

## Context & constraints

- `ScanSessionViewModel` drives the app's one screen: `ScanState` `Ready` / `Starting` / `Scanning` / `Complete`, a
  per-session set of texts already handled, auto-stop of the camera on completion, `SaveCommand` running
  `RecoverySession.TryAssemble` on `Task.Run` and opening the save picker, `SaveAnywayCommand` behind a
  confirmation on a hash mismatch.
- "Well printed" is already measured by the scan itself: a code that will not decode never arrives and stays in
  *Still missing*; a misprinted one that decodes but fails its CRC-32 is refused as damaged. The check adds the
  end-to-end verdict — the SHA-256 — that only a complete session can give.
- Core is untouched: `TryAssemble` already returns the outcome and the rebuilt hash; the check simply never keeps
  the content.
- House rules in force: CommunityToolkit.Mvvm without source generators (explicit `field` properties, commands
  built in the constructor), strings in `Strings.resx` with the hand-written `Strings` accessor (guarded by the
  reflection test), zero warnings, xUnit v3, explicit usings.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Button label | **Check a printed backup** | Says what is checked (the printout, the backup as a whole), matches the app's verb-phrase buttons (*Start scanning*, *New session*) | "Check QR codes" (reads as checking any QR code); "Verify a printout" ("verify" collides with the hash vocabulary of the save flow); "Test print" (ambiguous) |
| Placement and visibility | A secondary (non-accent) full-width button directly under *Start scanning*, visible only while the camera is off and the session holds no code | Recovery stays the primary action; the choice of purpose only makes sense before the first code | A toggle/switch on the page (one more control to read for the common case); a separate page (the app has a single screen by design) |
| How the mode is modelled | A `SessionPurpose` enum (`Recover`, `Check`) on the existing `ScanSessionViewModel`. Whichever start button is pressed on an empty session sets it; *Resume scanning* keeps it | One session, one camera pipeline, one set of progress properties — the check differs only at completion | A second ViewModel (duplicates the whole ingestion and progress logic); a `ScanState.Checked` value (the state describes the camera, the purpose is orthogonal) |
| What completion does in check mode | The camera stops (as today), then the file is rebuilt in memory off the UI thread, its SHA-256 compared, and the verdict shown; the rebuilt bytes are dropped at once | The draft: no recovered file, only the readability and hash check; dropping the bytes keeps "nothing outlives the process" even within the process | Asking the user to tap *Verify* (an extra tap for no decision); keeping the bytes for a later save |
| Where the verdict shows | A persistent *Check result* card on the page, plus one info-bar banner that replaces the *Every code is in* banner | The banner is transient; the card stays for whoever looks at the phone after putting the paper down | Banner only (gone in seconds); a dialog (blocks the page for a non-decision) |
| Verdict severity | Verified → Success; HashMismatch, DecompressionFailed, EncryptionUnsupported (and the unreachable Incomplete) → Error; a mismatch names both hashes | A check passes or fails — unlike a recovery, where a mismatch is a warning because the bytes are still on offer | Reusing the recovery's sentences and severities ("save them only if you understand…" makes no sense when nothing can be saved) |
| Mode visible while scanning | The progress card's heading reads *Checking a printed backup* instead of *Progress* | The user must not finish a scan expecting a save button that check mode never shows | A banner at start (transient); no indicator |
| Offer a save after a passed check | No | The draft is explicit that a check does not generate the file; a save offer would blur the two purposes | Switching the session to a recovery on demand |
| *New session* after a check | Keeps the check purpose and turns the camera back on, as today | Checking several printouts in a row is the natural follow-up; pausing an empty session brings both start buttons back | Resetting to a recovery |
| Test seam for the automatic verification | `VerifyCommand` (`AsyncRelayCommand`), executed by the completion handler; tests await its `ExecutionTask` | The idiomatic MVVM way to run awaited work from a sync event handler with exceptions flowing to the UI context; no `InternalsVisibleTo` exists and none is added | `async void` handler (untestable, crashes on a fault); synchronous assembly on the UI thread (contradicts the reason Save uses `Task.Run`) |
| Camera-permission wording | `CameraPermissionDenied` names both start buttons | Either button can now raise the prompt | Leaving it naming *Start scanning* only |
| Desktop app | Out of scope | Not asked; the desktop recovery already shows its verification outcome before saving | Adding a check mode to the desktop page |

## Phase

Single phase — branch `feature/feature-3941-check-printed-backup`.

### Steps

1. `ViewModels/SessionPurpose.cs` — `Recover` (zero value), `Check`, documented.
2. `ScanSessionViewModel`:
   - `Purpose` (observable), `IsChecking`, `ProgressTitle` (*Progress* / *Checking a printed backup*);
   - `CanStartCheck` (`Ready` and no code) and `StartCheckCommand` (sets `Check`, starts the camera);
   - `StartScanningCommand` sets `Recover` when the session is empty, keeps the purpose otherwise;
   - `CanSave` (`Complete` and `Recover`) — the *Save* button's visibility; `SaveCommand` gated the same way;
   - on completion: `Recover` → the *Every code is in* banner as today; `Check` → `VerifyCommand`, which runs
     `TryAssemble` on `Task.Run` under `IsBusy`, sets `CheckVerdict` (+ `HasCheckVerdict`), publishes it, and
     keeps no content (never `_unverified`);
   - *New session* clears the verdict and keeps the purpose.
3. `OutcomeMessages.DescribeCheck(AssemblyResult)` — the check's sentences and severities.
4. `Strings.resx` + `Strings.cs`: `CheckBackup`, `CheckIntro`, `CheckProgressHeader`, `CheckResultHeader`,
   `CheckPassed`, `CheckHashMismatch`, `CheckHashMismatchFormat`, `CheckDecompressionFailed`,
   `CheckEncryptionUnsupported`, `CheckIncomplete`; `CameraPermissionDenied` reworded.
5. `Views/MainView.axaml`: the check paragraph in the introduction card, the bound progress heading, the
   *Check result* card coloured by severity, the *Check a printed backup* button under *Start scanning*, the
   *Save* button bound to `CanSave`.
6. Tests (`tests/Enigma.HardCopy.Recover.UnitTests`): a check starts the camera as a check; a complete check
   verifies, publishes one success verdict and writes nothing (no picker, no *Save*, no *Save anyway*); a
   mismatched check fails naming both hashes and offers nothing; an empty-file backup's check passes; the check
   button is offered only before the first code; a paused check resumes as a check; an empty paused check
   becomes a recovery through *Start scanning*; *New session* after a check checks again and clears the
   verdict; a recovery's completion still offers *Save* and no verdict. `OutcomeMessagesTests`: every outcome
   has a check sentence, only Verified passes, a mismatch is an error naming both hashes, `null` throws.
7. `README.md`, *Recovering on Android*: a *Checking a printout* paragraph.

### Acceptance criteria

- `dotnet build Enigma.HardCopy.slnx` with zero warnings (Android head included); the whole suite green.
- In check mode no code path reaches `IRecoveredFileSaver` or `ISaveTarget`, and no rebuilt content is retained
  after the verdict.
- A recovery behaves exactly as before (existing tests unchanged and green).
- Best effort: on the `Pixel_6_API_36` emulator the start card's accessibility tree shows both *Start scanning*
  and *Check a printed backup*; if the emulator cannot be run, the completion doc says so.

## Out of scope

- A check mode in the desktop app.
- Reports beyond the verdict (per-code read statistics, a list of codes refused during the check).
- Any change to Core, the `EHC1` format, the PDF or the Android head.
- Converting a passed check into a recovery without rescanning.
