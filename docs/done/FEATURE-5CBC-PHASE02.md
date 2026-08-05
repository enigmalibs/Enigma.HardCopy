# FEATURE-5CBC-PHASE02 — NavigationView shell, hosts & pickers — DONE

**Branch:** `feature/feature-5cbc-phase02-shell-services`
**Date:** 2026-08-05

## Summary

The shell is now the library's rail. `MainWindow` is a `Panel` z-stack — a `DockPanel` holding a vertical
`NavigationView` plus a `ContentControl` for the page, and behind them the three service hosts
(`Overlay` → `ContentDialog` → `InfoBar`, in that deliberate order) — and `MainWindowViewModel` builds two
`NavigationItem`s and hands the navigation service a container-backed `PageFactory`. The `TabControl`, the
name-matching `ViewLocator`, the `PageViewModel` base class and `App.axaml`'s `DataTemplates` block are gone: a
page is declared exactly once, as a `NavigationItem` carrying its header, its icon, its view type and its
ViewModel type.

The container wiring moved out of `App` into two extensions — `AddEnigmaAvaloniaDesktop()` for the library's six
singletons and `AddEnigmaHardCopyDesktop()` for everything the app owns — in **separate files**, because a
single file importing both `Services` namespaces does not compile (`CS0104` on `IFileDialogService`). Views are
now transient and page ViewModels singletons, so revisiting a page builds a fresh control and re-attaches the
state the user left. `App` then does the one-time handover the library requires — three `RegisterHost` calls,
two `SetStorageProvider` calls — subscribes `NavigationFailed` to the logger, and selects the first rail item:
navigation happens there, never in a ViewModel constructor.

`StorageProviderFileDialogService` no longer takes the window. It takes the library's `IFileDialogService`
(through an alias) and forwards the **same options objects** it always built, so its four methods keep their
titles, filters, suggested names, default extension and overwrite prompt verbatim and keep returning
`StorageProviderFile` / `StorageProviderSaveTarget`. The app's own `IFileDialogService` /
`IPickedFile` / `ISaveTarget` contracts did not move, which is why no Backup or Recover ViewModel test needed
touching — only `MainWindowViewModelTests` was rewritten, as the plan allows.

`Enigma.HardCopy.Core` is untouched. The suite is 607 green (was 587; +20 from the rewritten shell tests and the
new DI tests), with zero warnings and zero `AVLN*` in both Debug and Release.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Desktop/Hosting/EnigmaAvaloniaServiceCollectionExtensions.cs` —
  `AddEnigmaAvaloniaDesktop()`, registering the library's six services with `TryAddSingleton`.
  `IFolderDialogService` is registered and unused, as planned. Imports only the library's namespace, which is
  what makes the bare names in it unambiguous.
- `src/Enigma.HardCopy.Desktop/Hosting/ServiceCollectionExtensions.cs` — `AddEnigmaHardCopyDesktop()`, holding
  what was `App.ConfigureServices` plus the two view registrations and the shell ViewModel's factory. Reaches
  the library's picker service and navigation service through the aliases `LibraryFileDialogs` /
  `LibraryNavigation`.
- `tests/Enigma.HardCopy.Desktop.UnitTests/Hosting/ServiceCollectionExtensionsTests.cs` — 20 tests over the
  wiring: each library service registered as a singleton over the right implementation, `TryAddSingleton`
  leaving an earlier registration alone, the six resolvable, the app's own picker service winning over the
  library's, **views transient and page ViewModels singleton**, and the pages plus their dependencies
  resolvable and stable across resolutions.
- `docs/done/FEATURE-5CBC-PHASE02.md` — this file.

**Modified**

- `src/Enigma.HardCopy.Desktop/Views/MainWindow.axaml` — the `Panel` → `DockPanel` → `NavigationView` +
  `ContentControl` composition, the three `x:Name`d hosts, and the dialog's size bounds on the host. Four new
  `xmlns` prefixes. The comment records why the host order must not be "corrected" to the library's documented
  one.
- `src/Enigma.HardCopy.Desktop/ViewModels/MainWindowViewModel.cs` — rewritten: takes `IServiceProvider`,
  `INavigationService` and the two rail geometries; sets `PageFactory`; builds the two items; keeps `Title`;
  exposes `Navigation`. `SelectedPageIndex`, `CurrentPage`, `Pages`, `Backup` and `Recover` are gone, and it
  performs **no** navigation.
- `src/Enigma.HardCopy.Desktop/App.axaml.cs` — `ConfigureServices` replaced by the two extension calls; the
  five host/provider handovers before the window is shown; `StartNavigation` subscribing `NavigationFailed`
  and selecting the first item.
- `src/Enigma.HardCopy.Desktop/App.axaml` — the `Application.DataTemplates` block (and the `local` xmlns)
  removed.
- `src/Enigma.HardCopy.Desktop/Services/StorageProviderFileDialogService.cs` — constructor takes the library's
  `IFileDialogService` via the `LibraryFileDialogs` alias; the four methods call `ShowOpenFileDialogAsync` /
  `ShowSaveFileDialogAsync` with unchanged options. Documents why the options overloads were used and not the
  string-path ones.
- `src/Enigma.HardCopy.Desktop/ViewModels/BackupViewModel.cs`,
  `src/Enigma.HardCopy.Desktop/ViewModels/RecoverViewModel.cs` — now derive from `ObservableObject`; the
  `Title` override is gone (nothing bound it once the tab strip did — the rail headers come from `Strings`).
  No other line changed in either file.
- `src/Enigma.HardCopy.Desktop/Resources/AppIcons.cs` — `Backup` (`QrCode`) and `Recover`
  (`ArrowsCounterClockwise`) added, both from the plan's pre-verified list.
- `tests/Enigma.HardCopy.Desktop.UnitTests/MainWindowViewModelTests.cs` — rewritten against the rail.
- `docs/roadmap.md`, `docs/plan/FEATURE-5CBC.md` — statuses.

**Deleted** — `src/Enigma.HardCopy.Desktop/ViewLocator.cs`,
`src/Enigma.HardCopy.Desktop/ViewModels/PageViewModel.cs`.

**Untouched, as the plan requires** — all of `src/Enigma.HardCopy.Core/`, all of
`tests/Enigma.HardCopy.Core.UnitTests/`, `Services/IFileDialogService.cs`, `IPickedFile.cs`, `ISaveTarget.cs`,
`StorageProviderFile.cs`, `StorageProviderSaveTarget.cs`, `Views/RecoverView.axaml(.cs)` (including the drop
handler), `Views/BackupView.axaml(.cs)`, `Program.cs`, `Resources/Strings.*`, `Directory.Packages.props`,
`Directory.Build.props`, `app.manifest`, and `BackupViewModelTests`, `RecoverViewModelTests`,
`OutcomeMessagesTests`, `StatusMessageTests`, `ChunkSizeOptionTests`, `StringsTests`.

## Deviations & follow-ups

1. **The rail icons are constructor arguments, and the `IconData` non-null assertion was dropped.** *(Decision
   taken with the user before building.)* The plan's step 9 asks the rewritten `MainWindowViewModelTests` to
   assert "non-null `IconData`", which cannot be done as the plan assumed:
   `PhosphorIconSet…ToGeometry()` returns a `StreamGeometry`, and constructing one throws
   `InvalidOperationException: Unable to locate 'Avalonia.Platform.IPlatformRenderInterface'` in a process with
   no Avalonia platform. Had `MainWindowViewModel` resolved `AppIcons` itself, **every** test on it would have
   failed, not just the icon one. Rather than add `Avalonia.Headless` to the ViewModel suite (the option
   offered and declined — it would put a rendering/windowing platform and a process-wide static init behind a
   587-test parallel suite that deliberately has none), the ViewModel now **takes** the two geometries and the
   DI factory passes `AppIcons.Backup` / `AppIcons.Recover`, resolved at first resolution when the app has a
   platform. The test hands in platform-free `PathGeometry` sentinels and asserts with `Assert.Same` that each
   landed on the right item — a stronger check than non-null, and the constructor rejects a null icon anyway.
   PHASE04 adds the footer *Settings* icon the same way (a third parameter).
2. **`MainWindowViewModel` sets `PageFactory`, `App` does not.** The plan says both (step 5 and step 7, and the
   *Startup wiring* narrative). Step 5's reading was taken because it is the only thing that justifies the
   `IServiceProvider` dependency step 5 also mandates; `App` keeps the `NavigationFailed` subscription and the
   first selection. The factory is a four-line private method on the ViewModel rather than a separate class:
   the ViewModel already holds the provider, and it already names `Control` under the plan's documented
   `INavigationService` exception.
3. **`AddEnigmaHardCopyDesktop` also registers the two views**, which step 3 asks for, and registers
   `MainWindowViewModel` through a factory instead of `AddSingleton<MainWindowViewModel>()` — the consequence
   of deviation 1. Consequence for the DI test: `MainWindow`, the two views and `MainWindowViewModel` are
   asserted as **descriptors** (service type + lifetime) and not resolved, because a `Window` needs a windowing
   platform and the shell factory resolves icon geometry. Both are covered by the run-time verification below.
4. **`BackupViewModel` / `RecoverViewModel` lost `Title` entirely.** Step 6 says to keep it "only if still
   used": nothing binds it now (the tab strip was its only consumer), so it went, and the rail headers come
   from `Strings.NavBackup` / `Strings.NavRecover` in `MainWindowViewModel` — the plan's stated fallback.
5. **Pointer input cannot be injected into this desktop session, so the click-driven checks were run through
   Avalonia's headless platform** against the real `MainWindow` and the real container. The session is KDE
   Plasma on Wayland with a rootless Xwayland: `XTEST` **key** events reach the app (verified — focus rings
   move), but `XTEST` **pointer** events are delivered at the compositor's pointer position rather than the
   warped one, so an external click cannot be aimed. The rail is also not in the keyboard focus cycle
   (`NavigationItem` is not focusable in library 1.0.0 — worth raising upstream), so there is no keyboard route
   to it either. The harness (scratch, never committed) starts the real `App` under
   `UseHeadless` + `ClassicDesktopStyleApplicationLifetime` and clicks the rail with real pointer events; see
   *Verified in the running app*. This will bite every later phase that needs a click.
6. **The "chosen file survives a page switch" case was verified through the density selection, not a file.**
   A picked file needs a platform picker, which the headless harness has none of; the mechanism is the same
   singleton ViewModel instance, and both the density (Backup) and the typed codes (Recover) were verified to
   survive a round trip on the real controls.
7. **The pickers were driven against Avalonia's managed dialog, not the desktop portal.** On this session the
   portal dialog is a Wayland surface — invisible to X and undrivable — so the probe run forced
   `X11PlatformOptions.UseDBusFilePicker = false` (a scaffold, reverted). What that exercises is the code this
   phase changed — our adapter → the library's service → `IStorageProvider` → the returned handles — with only
   the dialog UI differing.
8. **`RecoverViewModel.ManualEntry` is `TextBox`-bound and was typed into**, so the page-state check went
   through the real control, not the ViewModel property.
9. **Line endings:** nothing to report. Every touched file is LF and `.gitattributes` already declares
   `* text=auto eol=lf`; no action taken, per the workflow's recommendation-only rule.

## Build/test evidence

```
dotnet clean && dotnet build -c Debug   --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet clean && dotnet build -c Release --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx              → total: 607  failed: 0  succeeded: 607  skipped: 0
dotnet test --solution Enigma.HardCopy.slnx -c Release   → total: 607  failed: 0  succeeded: 607  skipped: 0
```

**`AVLN*` warnings: zero.** Both full `--no-incremental` logs were searched for the string `AVLN` explicitly
(0 hits each) on top of the 0-warning summary, because `TreatWarningsAsErrors` does not promote them.

**No protected test file was modified.** `git status` shows exactly one changed file under `tests/` —
`MainWindowViewModelTests.cs`, the one the plan names — plus the new `Hosting/` folder. 607 = 587 baseline + 8
rewritten shell tests (from 6) + 20 new DI tests, minus the 6 removed tab-era assertions.

### Verified in the running app (Linux, `DISPLAY=:1`)

Screenshots were taken with `ffmpeg -f x11grab -window_id …` (the root grab is empty under rootless Xwayland —
recorded in PHASE01), from the **exact code being committed**, after every scaffold was reverted:

- **Dark, backup page** — the rail on the left with both Phosphor icons rendering (`QrCode`,
  `ArrowsCounterClockwise`), *Backup* highlighted, the three section cards and the *Generate PDF* button
  unchanged from PHASE01.
- **Dark, recovery page** — *Recover* highlighted on the rail, both columns (scanned pages / typed by hand,
  progress / activity) laid out correctly inside the `ContentControl`.
- **Light, both pages** — same layout on `#F7F8FA` surfaces; the rail keeps its own accent surface in both
  variants, and every card, button, `ComboBox` and `TextBox` reads correctly.
- **No navigation failure and no exception** in the app log across every run: had the `PageFactory` or a
  `RegisterHost` been wired wrongly, the window would have come up empty and `StartNavigation`'s handler would
  have logged it.

**All four pickers, end to end** (probe scaffold calling the app's own `IFileDialogService`, each dialog driven
by keyboard, then reverted). Every title came from `Strings` and every option survived the trip through the
library's service:

| Method | Dialog title seen | Returned |
|---|---|---|
| `ChooseFileToBackUpAsync` | *Choose the file to back up* | `TODO.txt`, **578 bytes read through the handle** |
| `ChooseImagesAsync` | *Choose the scanned pages*, filter shown as **Images** | **2** files (multi-select), both names correct |
| `ChoosePdfDestinationAsync` | *Save the backup PDF*, name pre-filled `probe-backup.pdf` | target at the chosen path |
| `ChooseRecoveredFileDestinationAsync` | *Save the recovered file*, name pre-filled `probe-recovered.bin` | target written — **4 bytes on disk** |

**Rail, page state and drop — real pointer input on the real window** (headless harness, 24 checks, all
passing, rerun against the committed code):

```
PASS  startup selects the first rail item / page is BackupView / DataContext is BackupViewModel
PASS  clicking 'Recover' moved the service's selection      ← the control→service TwoWay binding
PASS  the recovery page is showing
PASS  typing reached the recovery ViewModel                 ← real TextBox input
PASS  clicking 'Backup' navigated back
PASS  the view is a NEW control (transient registration)
PASS  the ViewModel is the SAME instance (singleton registration)
PASS  the chosen density survived leaving the page / the restored view shows it
PASS  the recovery view is a NEW control, the ViewModel the SAME instance
PASS  the typed codes survived the round trip / the restored view shows them
PASS  drag-and-drop is enabled on both freshly built recovery views
PASS  a dropped PNG of a real EHC1 code was imported        ← code rendered by Core.QrRenderer
PASS  the overlay, dialog and info-bar hosts are all in the window
```

The drop check is a genuine end-to-end import: the harness encoded a small payload with `BackupEncoder`,
rendered code 1 with `QrRenderer`, dropped the PNG on the recovery page through Avalonia's drag-and-drop
plumbing, and the session came back with the backup id read off the image.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `AddEnigmaAvaloniaDesktop()` registers the library's six with `TryAddSingleton` | ✅ (asserted per service) |
| 2 | `AddEnigmaHardCopyDesktop()` holds the app's registrations, in its own file | ✅ (the `CS0104` reason is documented in both files) |
| 3 | Views transient, page ViewModels singleton, `MainWindow` singleton | ✅ (asserted, and observed in the running app) |
| 4 | `MainWindow` is the `Panel`/`DockPanel`/rail composition with the three hosts in `Overlay` → `Dialog` → `InfoBar` order and the dialog sizes on the host | ✅ |
| 5 | `MainWindowViewModel` builds the two items, exposes `Navigation`, keeps `Title`, selects nothing; the tab-era members are gone | ✅ (`CurrentPage` still `null` after construction) |
| 6 | `ViewLocator` and `PageViewModel` deleted; the pages derive from `ObservableObject`; no `Application.DataTemplates` | ✅ |
| 7 | `App` performs the five handovers, sets nothing else up before the window, subscribes `NavigationFailed`, selects the first item | ✅ (`PageFactory` set by the ViewModel — *Deviations* 2) |
| 8 | `StorageProviderFileDialogService` takes the library's service, keeps every option and return type; the window-taking registration is gone | ✅ (all four verified against real dialogs) |
| 9 | `MainWindowViewModelTests` rewritten against the rail; `ServiceCollectionExtensionsTests` added | ✅ (`IconData` asserted by identity, not non-nullness — *Deviations* 1) |
| 10 | Build clean, zero `AVLN*`; suite green with **no** Backup/Recover ViewModel test changed | ✅ (607/607 Debug + Release) |
| 11 | Manually: the rail switches pages, page state survives, all four pickers return usable paths, drag-and-drop still imports | ✅ (pickers on the real app; rail/state/drop through the headless harness — *Deviations* 5) |
| 12 | Screenshots of both variants | ✅ (both variants × both pages, from the committed code) |
