# FEATURE-5CBC-PHASE04 — Settings page & theme preference — DONE

**Branch:** `feature/feature-5cbc-phase04-settings`
**Date:** 2026-08-05

## Summary

The application now remembers how it should look. A third page hangs off the rail's footer — one
`SettingsCard`, a `Palette` icon, and three choices: **Follow the system**, **Light**, **Dark**. Choosing one
repaints every window that is already open, immediately, and writes the choice to
`~/.config/Enigma.HardCopy/settings.json` (`%APPDATA%\Enigma.HardCopy\settings.json` on Windows). The next
start reads it back before the window is shown, so the first frame is already in the right colours rather than
flashing the operating system's variant and correcting itself.

Three values rather than a light/dark toggle, because "follow the system" is a state a user has to be able to
come back to — and until this phase, following the system was the only thing the application could do.

Two seams carry it, both the application's own, both for the reason `IFileDialogService` exists: `IThemeService`
is the single place that assigns `RequestedThemeVariant`, and `IAppSettingsStore` is the single place that
touches the file. `SettingsViewModel` names neither Avalonia nor the control library, and the whole page is
asserted with no windowing platform behind it.

**Nothing here can stop the application starting.** A preference file that is absent, unreadable, truncated,
hand-edited into nonsense, naming a theme this version does not have, or written by a later version all resolve
to the same thing: the defaults, a line in the log, and an application that opens. Every one of those six was
exercised against the running app, not only in the suite.

`Enigma.HardCopy.Core` is untouched. The suite is **682 green** (was 632; +50), zero warnings and zero `AVLN*`
in Debug and Release.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Desktop/Settings/AppTheme.cs` — `System` / `Light` / `Dark`.
- `src/Enigma.HardCopy.Desktop/Settings/AppSettings.cs` — `FormatVersion` (constant `1`) and `Theme`, the
  latter written as its name so the file stays correctable by hand.
- `src/Enigma.HardCopy.Desktop/Settings/IAppSettingsStore.cs` — the contract, whose whole point is that
  neither method may throw into the application.
- `src/Enigma.HardCopy.Desktop/Settings/AppSettingsStore.cs` — one JSON file under
  `Environment.SpecialFolder.ApplicationData`, which is the right place on both target platforms without a
  per-platform branch. A save writes a sibling and moves it over the target, so a crash mid-write cannot
  truncate the settings.
- `src/Enigma.HardCopy.Desktop/Services/IThemeService.cs` + `ThemeService.cs` — the only assignment of
  `Application.Current.RequestedThemeVariant` in the application. `System` maps to `ThemeVariant.Default`,
  which is not a third palette but the absence of a request.
- `src/Enigma.HardCopy.Desktop/ViewModels/ThemeOption.cs` — the three offered variants with their labels,
  mirroring `ChunkSizeOption`.
- `src/Enigma.HardCopy.Desktop/ViewModels/SettingsViewModel.cs` — reads the stored choice into the backing
  field (not through the setter) and applies + persists exactly once per real change.
- `src/Enigma.HardCopy.Desktop/Views/SettingsView.axaml` + `.axaml.cs` — the one page in this application built
  the way the library's settings cards are meant to be used: no surrounding `Border.card`, because here the
  card *is* the row.
- `tests/…/Settings/AppSettingsStoreTests.cs` (24 tests), `tests/…/SettingsViewModelTests.cs` (16),
  `tests/…/ThemeOptionTests.cs` (9).
- `tests/…/TestDoubles/FakeThemeService.cs`, `FakeAppSettingsStore.cs`, `RecordingLogger.cs`.
- `docs/done/FEATURE-5CBC-PHASE04.md` — this file.

**Modified**

- `src/Enigma.HardCopy.Desktop/ViewModels/MainWindowViewModel.cs` — a fifth constructor argument (the
  settings icon) and the footer `NavigationItem`. Which collection an item lands in is the whole statement:
  settings is not one of the two things this application is for.
- `src/Enigma.HardCopy.Desktop/App.axaml.cs` — `ApplyStoredTheme` before the window is shown, and before
  `StartNavigation`. The application's `IThemeService` is reached through an alias (`AppTheming`): this file
  imports the library's `Services` namespace, so importing the application's whole one would reintroduce the
  `CS0104` on `IFileDialogService` the plan warns about.
- `src/Enigma.HardCopy.Desktop/Hosting/ServiceCollectionExtensions.cs` — the store (by factory, since its path
  is a plain string the container cannot supply), the theme service, and the settings page: view transient,
  ViewModel singleton, like the other two.
- `src/Enigma.HardCopy.Desktop/Resources/Strings.resx` + `Strings.cs` — 8 new strings.
- `src/Enigma.HardCopy.Desktop/Resources/AppIcons.cs` — `Settings` (`Gear`).
- `tests/…/MainWindowViewModelTests.cs` — the footer assertion replaces
  `FooterItems_AreEmpty_UntilTheSettingsPageExists`, which existed to be replaced by exactly this; plus the
  fifth null case and the sentinel icon.
- `tests/…/Hosting/ServiceCollectionExtensionsTests.cs` — the two new seams and the settings page.
- `docs/roadmap.md`, `docs/plan/FEATURE-5CBC.md` — statuses.

**Untouched, as the plan requires** — all of `src/Enigma.HardCopy.Core/` and
`tests/Enigma.HardCopy.Core.UnitTests/`; `App.axaml` (it already declared
`RequestedThemeVariant="Default"`, which is exactly the pre-load default the plan asks for);
`Views/MainWindow.axaml` (the rail already bound `FooterItems`); `Views/BackupView.axaml`,
`Views/RecoverView.axaml`, `ViewModels/BackupViewModel.cs`, `ViewModels/RecoverViewModel.cs`, every service
under `Services/` from earlier phases, `Program.cs`, `Directory.Packages.props`, `Directory.Build.props`,
`app.manifest`, `scripts/`; and `BackupViewModelTests`, `RecoverViewModelTests`, `BackupViewModelShellTests`,
`RecoverViewModelShellTests`, `OutcomeMessagesTests`, `StatusMessageTests`, `ChunkSizeOptionTests`,
`StringsTests`.

**No new package.** `System.Text.Json` is in the shared framework, and nothing else was needed.

## Deviations & follow-ups

1. **The `formatVersion` guard refuses only a *higher* version.** It was first written to refuse anything that
   was not exactly `1`, which would have rejected a file with no `formatVersion` at all — but `AppSettings`
   defaults that property to the current version, so an absent key is indistinguishable from `1` without a
   nullable property or a custom converter. Refusing only higher is also what the plan actually specifies
   ("constant `1`, refused if higher"), and it is the honest rule: a later version's file is one this build
   would be guessing at, whereas a file with no version is the only shape there has ever been. Pinned by
   `Load_WithNoFormatVersion_ReadsItAsTheCurrentShape`.
2. **A defined-value check on top of the version check.** `JsonStringEnumConverter` throws on a *name* the enum
   does not have — which the corrupt-file path already catches — but accepts a *number* it does not have, so
   `"theme": 7` would otherwise have become `(AppTheme)7` and reached the UI. An explicit `Enum.IsDefined`
   closes it. Both cases are tested.
3. **`AppSettingsStore` is public, not `internal sealed` like the other adapters.** PHASE03 recorded that the
   three shell adapters could not be unit-tested directly because they are internal and this repository has no
   `InternalsVisibleTo`. This class must be tested directly — the plan's acceptance criteria are a list of
   file-corruption cases — and unlike those adapters it names no framework type at all, so making it public
   costs nothing. Its path comes in through the constructor, which is what lets the same class be pointed at a
   temporary file.
4. **`ThemeOption.Default` exists although `ThemeOption.For` throws.** `SettingsViewModel` falls back rather
   than throwing if the store ever hands back a value outside the enum: a page that throws while being built
   is a blank window with a line in the log, which is a bad trade for a preference.
5. **The settings page's `SettingsCard` is not wrapped in a `Border.card`.** Every other page groups its cards
   inside one; here the card is the row and a wrapper would draw a panel around a single panel.
6. **The barcode density is still not remembered,** per the interview decision. The page intro says so, in
   those words, so the absence reads as a choice rather than an oversight.
7. **The suite reads the real preference file once.** `ThePagesAndTheirDependencies_AreResolvable` resolves
   `SettingsViewModel` from the container, whose constructor loads through the real store at
   `AppSettingsStore.DefaultFilePath`. It is a read, it cannot fail, and it is what proves the page is
   constructible from the real registrations — but it is a test that touches the developer's home directory,
   which is worth knowing.
8. **Line endings:** nothing to report. Every touched file is LF and `.gitattributes` already declares
   `* text=auto eol=lf`; no action taken, per the workflow's recommendation-only rule.

## Build/test evidence

```
dotnet clean && dotnet build -c Debug   --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet clean && dotnet build -c Release --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx              → total: 682  failed: 0  succeeded: 682  skipped: 0
dotnet test --solution Enigma.HardCopy.slnx -c Release   → total: 682  failed: 0  succeeded: 682  skipped: 0
```

**`AVLN*` warnings: zero.** Both full `--no-incremental` logs were searched for the string `AVLN` explicitly
(0 hits each) on top of the 0-warning summary, because `TreatWarningsAsErrors` does not promote them.

682 = 632 baseline + 49 new + 1 rewritten (`FooterItems_AreEmpty_…` became the footer assertion). The new tests
assert, among the rest: a save/load round trip for each of the three variants; the file is written with the
theme as a *name*; the directory is created on a first run; no temporary file survives a save; a missing file
is the defaults **and no log line at all**; corrupt, empty, `null`, a theme name the enum lacks, a theme
*number* it lacks, and a higher `formatVersion` each give the defaults **and** a warning; hand-typed casing on
either the property names or the theme value still reads; an unwritable path warns instead of throwing;
building the page neither repaints nor writes; a change applies and persists exactly once; re-selecting the
same option does nothing; a null selection is ignored.

### Verified in the running app (Linux, `DISPLAY=:1`)

Screenshots taken with `ffmpeg -f x11grab -window_id …` (the root grab is empty under rootless Xwayland — see
PHASE01). Pointer input cannot be injected into this session (recorded in PHASE02), so the page was reached,
and the choices made, through a scaffold in `App.axaml.cs` that was **reverted before this commit** — the file
was restored from a byte-for-byte copy and its checksum re-verified, and the Debug build was remade from the
committed code.

**Startup, one process per case, real file on disk:**

- `theme: "Dark"` → the whole app dark; `theme: "Light"` → the whole app light, which is *not* what this
  desktop's OS variant is. That difference is the proof the preference is being honoured rather than the OS
  followed, and it is the same as a restart: the file was on disk before the process started.
- No file at all → the OS variant (dark here), **and no warning**, because a first run is not a problem.
- Truncated JSON (`{ "formatVersion": 1, "theme": "Chartre`) → the app opened, on the OS variant, with
  `warn: …AppSettingsStore … could not be read; the defaults are used.`
- `formatVersion: 2` → the app opened, on the OS variant, with
  `warn: … declares format version 2, and this version reads 1; the defaults are used.`
- No file was created by any of these: the application writes only when the user chooses.

**In place, in a single process** (started on Settings with `Dark` stored, then Light → Follow the system →
Dark, four grabs across one run):

```
SCAFFOLD chose Light;  variant is now Light;   file says { "formatVersion": 1, "theme": "Light" }
SCAFFOLD chose System; variant is now Default; file says { "formatVersion": 1, "theme": "System" }
SCAFFOLD chose Dark;   variant is now Dark;    file says { "formatVersion": 1, "theme": "Dark" }
```

Every grab shows the whole window repainted — rail, page, card, combo box — with no restart and no flicker of
a half-themed frame, and the selector's own text following the choice.

**Screenshots kept:** the backup page in Dark and in Light; the settings page in Dark and in Light, showing the
footer item selected, the `Palette` icon, the header and description, and the combo box reading back the stored
choice; the recovery page in Dark and in Light — the first time that page has been seen in Light at all, since
before this phase the application could only follow an OS that is set to dark.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `AppSettings`, `AppTheme`, `IAppSettingsStore`, `AppSettingsStore` as described in *Settings storage* | ✅ (`formatVersion` guard as *Deviations* 1) |
| 2 | `IThemeService` + `ThemeService` the only assignment of `RequestedThemeVariant`; `System` → `ThemeVariant.Default` | ✅ |
| 3 | `SettingsViewModel` + `SettingsView` — one `SettingsCard` (`Palette`) with the three choices, applying and persisting exactly once per change | ✅ |
| 4 | Both registered (view transient, ViewModel singleton) and the footer `NavigationItem` (`Gear`, `Strings.NavSettings`) added | ✅ |
| 5 | `App.axaml.cs` applies the stored variant before the window is shown; `App.axaml` keeps `RequestedThemeVariant="Default"` | ✅ (the attribute was already there and is unchanged) |
| 6 | New strings: nav label, page header and intro, card header and description, three choice labels | ✅ (8) |
| 7 | Tests: round trip; missing → `System`; corrupt → `System`, no throw; unknown enum → `System`; higher `formatVersion` → `System` + warning; the ViewModel applies through the seam and persists once per change | ✅ (49 new) |
| 8 | Build clean, zero `AVLN*`; the new tests plus the whole suite green | ✅ (682, Debug and Release) |
| 9 | Manually: the footer item opens the page; each choice repaints immediately; the choice survives a restart; deleting `settings.json` returns to following the OS; a corrupted one still starts the app | ✅ (all five, in the running app) |
| 10 | Screenshots of both variants **and** of the settings page | ✅ (six: backup, settings and recovery, each in both) |
