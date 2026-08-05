# FEATURE-5CBC-PHASE03 — Page restyle, overlay, InfoBar & confirmations — DONE

**Branch:** `feature/feature-5cbc-phase03-page-restyle`
**Date:** 2026-08-05

## Summary

The two pages stopped drawing their own progress and their own outcomes. A long run now goes behind the
library's modal `Overlay`, on a `ProgressOverlayCard` of ours carrying the operation's title and its live stage
text; an outcome goes to the window's `InfoBar` at the severity it already had. Both arrive through the
application's own seams — `IProgressOverlay` and `INotificationService` — so no ViewModel gained an Avalonia
type: `ProgressText` and `Message` are still exactly the state they were, they are simply also published now.
The two inline `Border.notice` outcome blocks and the two inline `ProgressBar` blocks are gone, and with them
the `notice.success` / `notice.error` styles that had no user left.

The recovery page's two irreversible actions are guarded. **Save anyway** now asks first, naming both hashes —
the one the backup recorded and the one the rebuilt bytes actually have — and it asks *before* the destination
picker, so declining costs nothing and leaves the offer standing. **Start over** asks only when the session
actually holds codes. Both go through `IConfirmationService` over the library's `ContentDialog`, both treat
**only** `DialogResult.Primary` as agreement, and neither may ever carry code text: a `ContentDialog` keeps its
content until the next one is shown, so the dialogs carry hashes and prose only.

Where a library control genuinely fit, it replaced hand-rolled markup: the barcode-density row is a
`SettingsCard` (`Barcode` icon, header and description from the strings that were already there, the `ComboBox`
as its content), and the code box is a `MultiLineTextEditor` with its title, its placeholder and *Add codes* in
`ActionContent`. The section `Border`s stayed plain — a 64-character hash inside a card reads as something the
user could change, and a card fires on `PointerPressed`. Every command button and every backup section header
picked up a Phosphor icon from the plan's pre-verified list.

`Enigma.HardCopy.Core` is untouched. The suite is **632 green** (was 607; +25), zero warnings and zero `AVLN*`
in Debug and Release, and the whole surface was exercised in the running app and against the real controls.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Desktop/Views/ProgressOverlayCard.cs` + `.axaml` — a `ContentControl` exposing `Title`,
  `Message`, `IsIndeterminate`, `Progress` and `CancelCommand`, with its `ControlTheme` merged into
  `App.axaml` **after** the library's dictionary. No `CancelCommand`, no button: import, rebuild and save have
  no cancellation token, and a permanently dead button is worse than none.
- `src/Enigma.HardCopy.Desktop/Services/IProgressOverlay.cs` + `ProgressOverlay.cs` — over `IOverlayService`.
  Show and hide are guarded inside the implementation, `HideAsync` forgets the card in a `finally`, and an
  `Update` arriving after the card is gone is a no-op.
- `src/Enigma.HardCopy.Desktop/Services/INotificationService.cs` + `NotificationService.cs` — over
  `IInfoBarService`, mapping `MessageSeverity` → `InfoBarSeverity` with a per-severity title. The library's
  `ShowAsync` completes on **dismissal**, so it is started and not awaited — and every failure it can produce
  is caught inside, because nothing observes that task.
- `src/Enigma.HardCopy.Desktop/Services/IConfirmationService.cs` + `ConfirmationService.cs` — over
  `IContentDialogService`; two methods, both returning `bool`, both `== DialogResult.Primary`.
- `tests/…/BackupViewModelShellTests.cs` (9 tests) and `tests/…/RecoverViewModelShellTests.cs` (12 tests).
- `tests/…/TestDoubles/FakeProgressOverlay.cs`, `FakeNotificationService.cs`, `FakeConfirmationService.cs`.
- `docs/done/FEATURE-5CBC-PHASE03.md` — this file.

**Modified**

- `src/Enigma.HardCopy.Desktop/ViewModels/BackupViewModel.cs` — two new dependencies; `ProgressText`'s setter
  writes the stage onto the card and `Message`'s publishes the outcome; `ShowAsync` before each run's `try`,
  `HideAsync` in the `finally` that already existed. Generation passes `CancelCommand`; reading does not.
- `src/Enigma.HardCopy.Desktop/ViewModels/RecoverViewModel.cs` — three new dependencies; the same
  `ProgressText` / `Message` change; overlay around import, rebuild and write; `OnSaveAnywayAsync` asks before
  the picker; `OnStartOver` became `OnStartOverAsync` and asks when `HasAnyCode`, so `StartOverCommand` is now
  an `AsyncRelayCommand`.
- `src/Enigma.HardCopy.Desktop/Views/BackupView.axaml` — `SettingsCard` for the density; icons on the three
  section headers (`File`, `Printer`, `FloppyDisk`) and the command buttons (`Files`, `Files`, `FilePdf`); the
  inline progress block and the outcome notice removed. The large-input warning stays.
- `src/Enigma.HardCopy.Desktop/Views/RecoverView.axaml` — `MultiLineTextEditor`; icons on all five commands
  (`Images`, `Keyboard`, `ShieldCheck`, `ShieldWarning`, `Broom`); the inline progress block and the outcome
  notice removed. The completeness bar, the missing-index list, the activity log, and the metadata-missing
  warning all stay.
- `src/Enigma.HardCopy.Desktop/App.axaml` — the card's dictionary merged after the library's;
  `notice.success` / `notice.error` (and their `> TextBlock` variants) deleted; `TextBox.mono` widened to
  `:is(TextBox).mono` so it still reaches the editor.
- `src/Enigma.HardCopy.Desktop/Hosting/ServiceCollectionExtensions.cs` — the three seams, registered by
  implementation type so the library's service names stay out of that file.
- `src/Enigma.HardCopy.Desktop/Resources/Strings.resx` + `Strings.cs` — 20 new strings.
- `src/Enigma.HardCopy.Desktop/Resources/AppIcons.cs` — `ConfirmStartOver` (`WarningCircle`) and
  `ConfirmUnverifiedSave` (`ShieldWarning`).
- `tests/…/Hosting/ServiceCollectionExtensionsTests.cs` — the three registrations, by lifetime and by
  resolution.
- `tests/…/BackupViewModelTests.cs`, `tests/…/RecoverViewModelTests.cs` — **construction lines only**
  (see *Deviations* 1).
- `docs/roadmap.md`, `docs/plan/FEATURE-5CBC.md` — statuses.

**Untouched, as the plan requires** — all of `src/Enigma.HardCopy.Core/` and
`tests/Enigma.HardCopy.Core.UnitTests/`; `App.axaml.cs`, `Views/MainWindow.axaml`,
`ViewModels/MainWindowViewModel.cs`, `Views/RecoverView.axaml.cs` (the drop handler),
`Services/IFileDialogService.cs`, `IPickedFile.cs`, `ISaveTarget.cs`, `StorageProviderFile.cs`,
`StorageProviderSaveTarget.cs`, `StorageProviderFileDialogService.cs`, `Program.cs`,
`Directory.Packages.props`, `Directory.Build.props`, `app.manifest`, `scripts/`; and
`OutcomeMessagesTests`, `StatusMessageTests`, `ChunkSizeOptionTests`, `StringsTests`,
`MainWindowViewModelTests`.

## Deviations & follow-ups

1. **The two protected ViewModel test files were edited — construction lines only.** *(Decision taken with the
   user before building, per the plan's own "raise it before changing the test" rule.)* The plan requires the
   pages to take three new seams **and** requires `BackupViewModelTests` / `RecoverViewModelTests` to pass
   untouched; both files construct the ViewModels positionally, including a
   `Constructor_RejectsMissingDependencies` that calls the constructor directly, so the two requirements
   cannot both hold. The alternative offered — trailing optional parameters defaulting to no-op
   implementations, which would have left the files literally unmodified — was declined in favour of honest
   required dependencies, so a missing registration is a startup failure rather than a page that quietly stops
   reporting anything. What changed: each file's private `Create()` helper and its
   `Constructor_RejectsMissingDependencies` (+2 and +3 null cases respectively). **No test name, assertion or
   fixture moved** — `git diff` on those two files is 19 and 23 lines, all of it construction.
2. **A fifth overlay title.** The plan lists four (generation, import, rebuild, save) but the backup page also
   sets `IsBusy` and a stage while it reads and hashes the chosen file, and that inline progress block was
   being deleted. Rather than lose the feedback or file a read under "Generating the backup",
   `OverlayReadTitle` was added.
3. **One string beyond the plan's list: `RecoverManualCodesTitle` ("Codes").** The plan asks for the code box
   to carry a `Title`; every existing candidate was already on screen in the same card
   (`RecoverManualHeader` is its `h2`), so a title that duplicated the header would have read as a mistake.
4. **The code box's height is set through the editor's text presenter.** The library's `BaseEditor` template
   sizes its input area from the text and honours neither `MinLines` nor the control's own `MinHeight` — both
   were tried in the running app, and both only pad the control around an unchanged three-line box. A style on
   `editors|MultiLineTextEditor.mono /template/ TextPresenter#PART_TextPresenter` restores it. The part is
   Avalonia's own `TextBox` contract, so it is stable; if a later library version renames it the style stops
   applying and the box returns to its natural size — smaller, never broken. **Worth raising upstream:** an
   editor that cannot be made taller is awkward for any multi-line use.
5. **`SaveAnywayCommand`'s button keeps `ShieldWarning`,** which the plan assigns to that action's *dialog*.
   It is in the plan's verified icon list, it is the same action, and reusing it ties the button to the dialog
   it raises.
6. **The recovery page's section headers have no icons.** The plan asks for header icons on the backup page
   (step 1) and for command icons on the recovery page (step 2); the recovery headers were left alone rather
   than invented.
7. **The dialogs' primary button is drawn in the accent colour, including *Save unverified*.** That is the
   library's `ContentDialog` theme, not something `DefaultButton` overrides — `DefaultButton = Close` declares
   intent and the control has no Enter handling. The warning is carried by the title, the icon, the prose and
   the two hashes instead. Changing it would mean either restyling the library's dialog or making the
   dangerous answer the `Close` button, which would break the `== DialogResult.Primary` rule the plan sets.
8. **Adapter internals are not unit-tested directly.** `ProgressOverlay`, `NotificationService` and
   `ConfirmationService` are `internal sealed`, matching `StorageProviderFileDialogService`, and this
   repository has no `InternalsVisibleTo`. Adding one is a repository-wide convention change and was left out
   of scope; the three are covered instead by the headless harness below, which drives them through the
   container against the real controls.
9. **Line endings:** nothing to report. Every touched file is LF and `.gitattributes` already declares
   `* text=auto eol=lf`; no action taken, per the workflow's recommendation-only rule.

## Build/test evidence

```
dotnet clean && dotnet build -c Debug   --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet clean && dotnet build -c Release --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx              → total: 632  failed: 0  succeeded: 632  skipped: 0
dotnet test --solution Enigma.HardCopy.slnx -c Release   → total: 632  failed: 0  succeeded: 632  skipped: 0
```

**`AVLN*` warnings: zero.** Both full `--no-incremental` logs were searched for the string `AVLN` explicitly
(0 hits each) on top of the 0-warning summary, because `TreatWarningsAsErrors` does not promote them.

632 = 607 baseline + 21 new page-shell tests + 4 new DI assertions. The 25 new tests assert, among the rest:
a generation raises the card exactly once and takes it down on success, on a composition failure, on a write
failure **and** on cancellation; only the generation card carries a cancel command, and it is the page's own
`CancelCommand`; every stage reaches the card; each outcome publishes exactly one notification at the right
severity and it is the same object as `Message`; the activity log publishes nothing; a **declined** *Save
anyway* writes nothing, never opens the destination picker and leaves the offer standing; an accepted one
writes and withdraws it; the question names both hashes; and *Start over* asks only once codes are in, keeps
everything when declined and clears when agreed.

### Verified in the running app (Linux, `DISPLAY=:1`)

Screenshots taken with `ffmpeg -f x11grab -window_id …` (the root grab is empty under rootless Xwayland — see
PHASE01), from the code being committed, after every scaffold was reverted and the tree confirmed clean:

- **Backup page, Dark and Light** — the three section-header icons, the `SettingsCard` holding the density
  with its `Barcode` icon and its description, the picker buttons, *Generate PDF*. No progress block and no
  outcome panel anywhere.
- **Recovery page, Dark and Light** — the `MultiLineTextEditor` with its title, its placeholder and *Add
  codes* inside the box, and the five icon commands. The Progress and Activity panels unchanged.
- **The overlay card, both variants** — *Generating the backup* with a live stage line and a working cancel
  button; *Importing scanned pages* with **no** cancel button. Both legible over the scrim.
- **The info bar, all three severity colours** — success (green), warning (amber), error (red), each with its
  title and the outcome sentence.
- **Both confirmations, both variants** — *Save bytes that failed their check?* with the `ShieldWarning` icon
  in the warning brush, the prose and both 64-character hashes in fixed pitch; *Discard this recovery?* with
  `WarningCircle`. Neither carries code text.

**Behaviour, against the real controls** — a scratch harness (never committed, reverted) started the real
`App` and the real container under Avalonia's headless platform, because pointer input cannot be injected into
this desktop session (rootless Xwayland — recorded in PHASE02). **39 checks, all passing:**

```
PASS  the overlay starts closed / ShowAsync opens the real Overlay host
PASS  the card is in the real visual tree, carries its title, offers cancel, renders the button
PASS  Update writes the stage onto the card
PASS  HideAsync closes the real Overlay host
PASS  an Update after the card is gone is a no-op, not a throw
PASS  a card raised with no command offers no cancel and renders no cancel button
PASS  the info bar opens with the right Severity, Title and Message  ×4 (Success/Warning/Error/Info)
PASS  the unverified-save dialog opened; it names both hashes; it carries no code text
PASS  the primary button answers yes / the close button answers no
PASS  Escape answers no (DialogResult.None, not Close)
PASS  a click on the scrim answers no
PASS  the start-over dialog opened, says what will be lost, and its primary button answers yes
```

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | Density row is a `SettingsCard`; icons on the backup commands and section headers; section `Border`s keep their shape | ✅ |
| 2 | Code box is a `MultiLineTextEditor` with `Title`, `PlaceholderText`, *Add codes* in `ActionContent`, `mono` and its height; icons on the five recovery commands | ✅ (height via the presenter — *Deviations* 4) |
| 3 | `ProgressOverlayCard` + its theme, merged after the library's; cancel hidden when no command | ✅ (both cases verified in the app) |
| 4 | `IProgressOverlay` over `IOverlayService`, guarded inside the implementation; a late `Update` is a no-op | ✅ |
| 5 | `INotificationService` over `IInfoBarService`, severity-mapped and titled, not awaited into the run | ✅ (all four severities verified) |
| 6 | `IConfirmationService` over `IContentDialogService`; two methods; only `Primary` is agreement; hashes and prose only | ✅ (button / Escape / scrim all verified) |
| 7 | The ViewModels change only where they must; the `StatusMessage` / `MessageSeverity` surface is unchanged | ✅ (`OutcomeMessagesTests` and `StatusMessageTests` unmodified) |
| 8 | Inline outcome and progress blocks removed; `notice.success` / `notice.error` deleted; everything else stays | ✅ |
| 9 | New strings with translator comments where they format | ✅ (20 strings; +1 beyond the plan — *Deviations* 3) |
| 10 | New fakes and assertions for the three seams | ✅ (25 new tests) |
| 11 | Build clean, zero `AVLN*`; suite green; the pre-existing Backup/Recover tests unmodified | ✅ apart from construction lines — *Deviations* 1 |
| 12 | Manually: overlay with live stage text on all four runs, cancel only on generation, a failing run still removes the scrim, InfoBar at the right severity, both confirmations on button/`Escape`/scrim, both variants | ✅ (screenshots + 39 harness checks) |
