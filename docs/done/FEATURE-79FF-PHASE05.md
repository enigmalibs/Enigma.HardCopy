# FEATURE-79FF-PHASE05 — Avalonia desktop app

**Branch:** `feature/feature-79ff-phase05-desktop`
**Status:** DONE

## Summary

The application now has its user interface: a two-page shell over the Core pipeline built in PHASE02–PHASE04,
on stock Avalonia 12 Fluent controls, with every user-facing string read from a `.resx`.

- **Shell.** `MainWindow` hosts a `TabControl` bound to a list of page ViewModels; a `ViewLocator` registered in
  `App.axaml`'s data templates turns each page ViewModel into its view. Navigation state is a single index on
  `MainWindowViewModel`, with `CurrentPage` derived from it, so the two cannot disagree.
- **Backup page.** Choose a file (its SHA-256 and size are shown immediately, so the user can compare them
  against `sha256sum` before printing); choose the barcode density (session-only, three presets); see the
  worst-case code and page estimate, with a warning past 20 pages; choose the destination; generate. Generation
  runs on background tasks with a working Cancel, and reports its stage while it runs.
- **Recover page.** Imported images and typed text feed **one** `RecoverySession`, so a page read off a
  photograph and one code typed by hand combine. Live status shows the backup ID, the recorded name and hash,
  received-of-expected codes, the indexes still missing, a progress bar, and a per-code activity log. A verified
  recovery offers to save; a **hash mismatch saves nothing** — it shows both hashes and puts an explicit
  "Save anyway" beside them. Any new code invalidates that verdict and closes the escape hatch again.
- **Strings.** `Resources/Strings.resx` holds all 99 user-facing strings; `Resources/Strings.cs` exposes them as
  documented public properties whose names *are* the resource keys, and throws on a missing key.
- **DI.** Core's `IBackupEncoder`, `IPdfComposer`, `IBackupIdGenerator` and `TimeProvider`, the window, the file
  dialog service and all three ViewModels are registered in the `IHost` built in `App`.

## Files/modules touched

### Created — `src/Enigma.HardCopy.Desktop/`

| File | What it is |
|---|---|
| `Resources/Strings.resx` | Every user-facing string, with translator comments on each format. |
| `Resources/Strings.cs` | Hand-written public accessor; property name = resource key; throws when a key is absent. |
| `ViewLocator.cs` | `IDataTemplate` mapping `…ViewModels.FooViewModel` → `…Views.FooView`. |
| `Services/IPickedFile.cs` | A file the user chose or dropped, as bytes — no Avalonia types. |
| `Services/ISaveTarget.cs` | A destination to write, with a name and a display location. |
| `Services/IFileDialogService.cs` | The four dialogs the app opens. Deliberately token-free — see *Deviations*. |
| `Services/StorageProviderFile.cs` | `IPickedFile` over `IStorageFile`; `Adapt` is what the drop handler uses. |
| `Services/StorageProviderSaveTarget.cs` | `ISaveTarget` over `IStorageFile`, truncating on overwrite. |
| `Services/StorageProviderFileDialogService.cs` | The real dialogs, through the main window's `StorageProvider`. |
| `ViewModels/PageViewModel.cs` | Base of the two pages: a title for the navigation strip. |
| `ViewModels/MessageSeverity.cs` | Information / Success / Warning / Error. |
| `ViewModels/StatusMessage.cs` | Text + severity, with converter-free `Is…` flags for styling. |
| `ViewModels/ChunkSizeOption.cs` | A `ChunkSizePreset` and the sentence describing its trade-off. |
| `ViewModels/OutcomeMessages.cs` | The whole Core-outcome → sentence + severity mapping, in one testable place. |
| `ViewModels/BackupViewModel.cs` | The backup flow. |
| `ViewModels/RecoverViewModel.cs` | The recovery flow, including the mismatch refusal. |
| `Views/BackupView.axaml(.cs)` | The backup page. |
| `Views/RecoverView.axaml(.cs)` | The recovery page, with the drag-and-drop handler. |

### Modified

| File | Change |
|---|---|
| `src/…/App.axaml` | Registered the `ViewLocator`; added the app's semantic palette (per theme variant) and its type/spacing/severity style classes. |
| `src/…/App.axaml.cs` | Full DI registration; the window is now resolved from the container rather than constructed. |
| `src/…/Views/MainWindow.axaml` | Replaced the PHASE01 placeholder with the two-page `TabControl`. |
| `src/…/ViewModels/MainWindowViewModel.cs` | Replaced the PHASE01 placeholder with real shell navigation. |
| `tests/…/MainWindowViewModelTests.cs` | Rewritten against the real shell. |
| `README.md` | Added a **Run** section — the app is startable for the first time (documentation freshness sweep). |
| `docs/roadmap.md`, `docs/plan/FEATURE-79FF.md` | PHASE05 status. |

### Created — `tests/Enigma.HardCopy.Desktop.UnitTests/`

`BackupFixtures.cs`, `StringsTests.cs`, `StatusMessageTests.cs`, `ChunkSizeOptionTests.cs`,
`OutcomeMessagesTests.cs`, `BackupViewModelTests.cs`, `RecoverViewModelTests.cs`, and the doubles
`TestDoubles/{FakeFileDialogService,FakePickedFile,FakeSaveTarget,FakePdfComposer,FixedBackupIdGenerator}.cs`.

## Deviations & follow-ups

### Deviations from the plan

1. **Progress is by stage, not by percentage** (plan step 2 says "progress"). Core's `EncodeAsync` and `Compose`
   are single calls with no progress callback, so a percentage would have been invented. The backup page names
   the stage it is in — reading, encoding, composing, writing — beside an indeterminate bar. The recovery page
   *does* show a real percentage, because there the denominator is genuine: codes received out of codes needed.
2. **The file-dialog methods take no `CancellationToken`**, against the house async convention's "every public
   async API takes one last". A modal picker is cancelled by the user dismissing it, which is already reported as
   `null`; a token would have promised something no platform picker can honour. The reads and writes that follow
   *do* take one. Recorded here rather than silently.
3. **The theme brushes the plan implied do not exist.** The first cut styled the cards and secondary text with
   WinUI names (`CardBackgroundFillColorDefaultBrush`, `TextFillColorSecondaryBrush`, `SystemFillColorCriticalBrush`).
   Avalonia 12's Fluent theme has none of them — it exposes the `SystemControl*Brush` family — so the styles
   silently resolved to nothing and the text was invisible. The app now declares its own semantic palette in
   `App.axaml` under `ThemeDictionaries`, with light **and** dark values. Caught by looking at the running app,
   not by the build.
4. **Avalonia 12 API drift.** `DragEventArgs.Data`/`DataFormats.Files` are gone (now `DataTransfer` /
   `DataFormat.File` / `TryGetFiles()`), and `TextBox.Watermark` is obsolete in favour of `PlaceholderText`.
   Both fixed; the resource key was renamed to `RecoverManualPlaceholder` to match.
5. **`MainWindow` is registered in the container.** Unusual for a window, but the dialog service must own its
   dialogs from *this* window, and the container is the right single place to know which window that is.

### Follow-ups

- **Windows was not smoke-tested.** Plan step 6 asks for a manual smoke on Windows and Linux; only Linux was
  available here (Arch/Wayland, native — not WSLg). The app starts, both pages render, and the flows are covered
  by tests, but the Windows pass is still owed — PHASE06 publishes a `win-x64` artifact and is the natural place
  for it.
- **The two flows were verified through their ViewModels and through the running app's rendering, not by
  clicking through a real file dialog.** A platform picker cannot be driven from a headless session. What *is*
  covered end to end: a real `PdfComposer` producing a `%PDF` document through `BackupViewModel`, and real
  QR PNGs decoded through `RecoverViewModel` into a complete session.
- **Activity log capped at 500 entries.** A 400-page backup produces more; the oldest are dropped. Worth
  revisiting only if users ask to keep the whole history.
- **No CRLF / line-ending issues were observed** in the files this dev touched; nothing to recommend.

## Build/test evidence

```
dotnet build                            → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx
  Core.UnitTests    ....................  passed
  Desktop.UnitTests ....................  passed
  total: 587   failed: 0   succeeded: 587   skipped: 0
```

Baseline before this phase was 505 tests; this phase adds **82**, all in `Desktop.UnitTests`:

- `StringsTests` — every string property resolves (a missing `.resx` key fails the build, not the running app);
  every `…Format` string still carries a placeholder.
- `MainWindowViewModelTests` — page order, default page, index-drives-page notification, clamping, null guards.
- `ChunkSizeOptionTests`, `StatusMessageTests` — presets and message severities.
- `OutcomeMessagesTests` — all six `AddCodeOutcome` values and all five `AssemblyOutcome` values map to text
  with the intended severity; the metadata code is named as such rather than as "code 0"; the mismatch message
  carries **both** hashes.
- `BackupViewModelTests` — command enablement, name/size/hash publication, estimate recomputed on density
  change, large-input warning sized against the estimator itself, read failure, composition failure, write
  failure, **cancellation mid-compose writes nothing**, and one full pass through the real `BackupEncoder` and
  `PdfComposer` asserting a `%PDF` document reaches the destination.
- `RecoverViewModelTests` — `SplitCodes` over wrapped/multiple/lower-case/no-code input; out-of-order and
  duplicate codes; foreign backup ID reported with the foreign ID; damaged payload reported with the index to
  re-scan; missing-index list and its truncation; verified recovery saves the original bytes; **hash mismatch
  saves nothing and opens the escape hatch**; save-anyway writes and closes it; a new code invalidates it;
  encrypted flag refused; write failure; real QR PNGs imported through both the picker and the drop command;
  a non-image reported unreadable; one unreadable file in a batch does not abandon the rest; start-over clears
  everything.

Manual verification on Linux: the app was launched and both pages screenshotted — the shell, tabs, cards,
mono-spaced hashes, progress panel ("3 of 6", still missing "4, 5, 6"), and the colour-coded activity log all
render correctly, and `Generate PDF` / `Recover file` are correctly disabled until their preconditions hold. The
recovery page was screenshotted with real state fed in through a temporary scaffold in `App.axaml.cs`, which was
**reverted** before this commit.
