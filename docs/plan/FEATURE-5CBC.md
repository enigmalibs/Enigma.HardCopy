# FEATURE-5CBC — Enigma.Avalonia.Desktop adoption & 1.1.0 release

**Status:** TODO
**Type:** multi-phase feature (5 phases)
**Branches (created by `/build`, each from `HEAD`):** one per phase, see *Phases*

## Objective

Rebuild the desktop app's UI on the house control library **`Enigma.Avalonia.Desktop` 1.0.0** — its Fluent
theme, its `NavigationView` shell, its `ContentDialog` / `Overlay` / `InfoBar` host services, its file-picker
service, its `SettingsCard` family and its typed editors — add a persisted theme preference, and release the
result as **1.1.0**.

The app today is "pure" Avalonia: a `FluentTheme`, nine hand-rolled brushes declared per theme variant in
`App.axaml`, fifteen style classes over stock controls, a `TabControl` shell with a name-matching
`ViewLocator`, no dialogs at all, and pickers reaching `Window.StorageProvider` directly. Nothing about that
is broken; the point of this item is that the house library now exists and this app should be built on it, so
the two stay in step and the app inherits the library's look and its services instead of maintaining private
equivalents.

**`Enigma.HardCopy.Core` is not touched by any phase.** No format change, no `EHC1` change, no PDF change:
every backup printed by 1.0 still recovers, byte for byte, and the printed self-recovery instructions stay
true.

## Validated decisions (interview 2026-08-04)

- **The shell becomes a vertical `NavigationView` rail.** *Backup* and *Recover* in `Items`, *Settings* in
  `FooterItems`, each with a Phosphor icon. `INavigationService` owns `Items` / `SelectedItem` /
  `CurrentPage`; a container `PageFactory` resolves views and ViewModels from DI. This app is genuinely
  page-based — a `PageViewModel` base, a `ViewLocator`, two independent flows — which is what
  `NavigationView` is for, and the footer gives the Settings page a home.
- **`ViewLocator` and `PageViewModel` are deleted.** Both exist only to serve the `TabControl` arrangement:
  the `PageFactory` resolves a page from DI by type, which is what `ViewLocator` did by name, and
  `PageViewModel.Title` fed the tab strip. `NavigationItem` becomes the **single declaration site** for a
  page — header, icon, view type, ViewModel type. `App.axaml`'s `DataTemplates` block goes with them.
- **Library controls where they fit, not everywhere.** The chunk-size row becomes a `SettingsCard`; the
  manual-code box becomes a `MultiLineTextEditor` with *Add codes* in `ActionContent`; the section `Border`s
  keep their shape on `EnigmaSurfaceBrush` / `EnigmaBorderSubtleBrush`. They are content groups, not settings
  rows — a 64-character hash inside a `SettingsCard` reads as a value the user could change, and a card fires
  its command on `PointerPressed`, which is wrong for a read-only row.
- **Every long run moves into the `Overlay`**, as a `ProgressOverlayCard` carrying the stage text, for backup
  generation, image import, assembly and saving alike. **Cancel appears on the card only for backup
  generation** — the one operation with a `CancellationTokenSource` today. Adding cancellation to import and
  assembly is a functional change and is **explicitly out of scope**.
- **The recovery completeness bar stays on the page.** Received-of-needed is not activity, it is state; it
  and the missing-index list are what the user reads while standing at a scanner.
- **`InfoBar` carries each page's outcome; the pages keep their detail.** `Message` routes to
  `IInfoBarService` behind an app-owned `INotificationService`; the two inline outcome `Border.notice` blocks
  are removed. The activity log, the missing-index list, the large-input warning and the metadata-missing
  warning all stay where they are — a one-line bar cannot replace a 300-entry log.
- **Two `ContentDialog` confirmations.** *Save anyway* (naming both hashes, *Save unverified* / *Cancel*) and
  *Start over* (asked only when the session already holds codes). Writing bytes that failed their SHA-256 is
  the app's one irreversible, genuinely dangerous action, and today it is guarded only by a button that
  appears with a warning beside it.
- **The Phosphor icon pack is adopted** — `Enigma.Icons`, `Enigma.Icons.Phosphor`, `Enigma.Icons.Avalonia`.
  Every icon slot in the library is a `Geometry?` that renders as nothing when left null, so a rail of two
  bare labels would look unfinished.
- **The pickers keep their `IStorageFile` handles.** The library's **options-object** overloads
  (`ShowOpenFileDialogAsync(FilePickerOpenOptions)` / `ShowSaveFileDialogAsync(FilePickerSaveOptions)`)
  replace the direct `StorageProvider` calls. The string-path extension overloads were declined: they project
  through `TryGetLocalPath()` and **silently drop every item without one**, and the drop handler deals in
  `IStorageFile` regardless, so both shapes would have to be maintained.
- **Theme preference: `System` / `Light` / `Dark`, persisted, defaulting to `System`.** On a `SettingsCard`
  row on the footer Settings page. Two values were not enough: "follow the OS" would exist only until the
  first click and could never be returned to.
- **The preference is Desktop-owned**, in `%APPDATA%\Enigma.HardCopy\settings.json` /
  `~/.config/Enigma.HardCopy/settings.json`, with a `formatVersion` guard. A theme variant means nothing to
  the Core pipeline, and Core stays UI-free. A missing, unreadable, malformed or unknown value falls back to
  `System`, logged at warning level, never fatal.
- **The Settings page holds the theme and nothing else.** A remembered default barcode density was offered
  and declined: `FEATURE-79FF` deliberately made density session-only "so every run starts from the default
  that the page layout is tuned for", and a remembered *Large* would silently change how many pages the next
  backup takes.
- **The activity log stays a plain `ListBox`.** The library's `CollectionView` subsystem was offered and
  declined — it is sorting/filtering/grouping machinery for a need this app does not have; the log is already
  newest-first and capped at 500 entries.
- **The library's services are named only inside `Desktop/Services/` and `Desktop/Hosting/`, behind the
  app's own interfaces — with one documented exception.** `MainWindowViewModel` exposes `INavigationService`
  directly for binding, because `CurrentPage` is an Avalonia `Control` by definition and any wrapper would
  only move the type one file over. It performs **no navigation in its constructor** — `App` selects the
  first item after startup — so it stays constructible in a headless test.
- **The existing ViewModel tests are not modified.** `BackupViewModelTests`, `RecoverViewModelTests`,
  `OutcomeMessagesTests`, `StatusMessageTests`, `ChunkSizeOptionTests` and `StringsTests` must pass
  untouched. `MainWindowViewModelTests` is the one exception and is rewritten in PHASE02 against the rail.
- **Version 1.1.0 — a minor.** User-visible functionality is added (a rail, a settings page, a theme
  preference, in-window confirmations, an overlay, notifications) and nothing breaks: the `EHC1` code grammar,
  the metadata key/value grammar, the printed recovery instructions, the PDF layout and SHA-256 verification
  are all untouched. A patch would misdescribe a redrawn app; a major would overstate a release that breaks no
  compatibility.
- **Release scope: code, version and release docs; the artifacts stay with the user.** In scope:
  `<Version>1.1.0</Version>`, `app.manifest`'s 4-part `assemblyIdentity` version, the `RELEASENOTES.md`
  1.1.0 section, the README what's-new callout / Install table / dependency list / project layout, and the
  package audit. Out of scope: `scripts/publish.sh`, `scripts/publish.ps1`, the zips, the git tag, the
  GitHub release — PHASE05 prints the runbook instead.
- **Verification includes running the app.** Per phase: clean Release build, **zero warnings and zero
  `AVLN*` warnings**, full suite green, then launch on `DISPLAY=:1` and screenshot **both** theme variants,
  recorded in the completion doc. A headless ViewModel suite cannot see a missing `ResourceInclude`, an
  unreadable brush pair or a collapsed card row — every one of those passes every test.
- **Five phases**, each leaving the app fully working and independently reviewable.

## Findings established before planning

- **The packages resolve.** `Enigma.Avalonia.Desktop` 1.0.0 and `Enigma.Icons` / `Enigma.Icons.Phosphor` /
  `Enigma.Icons.Avalonia` 1.0.0 are all in the local NuGet cache; the library ships `net8.0` and `net10.0`
  assets, and this solution targets `net10.0`.
- **The library's Avalonia pins are 12.1.1** — identical to this solution's coupled set, so **nothing in the
  version-coupled group moves** as part of this item.
- **`Enigma.Core` 1.0.0 is new to this solution.** It arrives transitively with the library and brings
  `BouncyCastle.Cryptography` (~4.7 MB). There is no way to opt out short of not referencing the library.
  Nothing in this app calls it; it is a dependency of the two binary editors, which this app does not use.
- **`PhosphorIcon` has 1512 members.** Every icon name this plan cites was verified against
  `Enigma.Icons.Phosphor.xml`: `QrCode`, `ArrowsCounterClockwise`, `Gear`, `Barcode`, `File`, `FilePdf`,
  `Files`, `Images`, `Printer`, `Keyboard`, `Fingerprint`, `FloppyDisk`, `ShieldCheck`, `ShieldWarning`,
  `WarningCircle`, `CheckCircle`, `Warning`, `Palette`, `Monitor`, `Sun`, `Moon`, `Broom`, `Trash`.
  Any substitution during a build must be checked against that enum, not invented.
- **The `IFileDialogService` name collides.** The app already owns
  `Enigma.HardCopy.Desktop.Services.IFileDialogService`, and the library ships
  `Enigma.Avalonia.Desktop.Services.IFileDialogService`. Inside `namespace Enigma.HardCopy.Desktop.Services`
  the app's own member wins (enclosing-namespace members beat using-imported names, so there is no ambiguity
  error) — but the library's must then be reached through an **alias**. In a namespace that owns neither, a
  file importing both gets `CS0104`. See *Risks*.
- **`Enigma.Avalonia` shadows `Avalonia` inside our namespaces.** Once the package is referenced, the
  namespace `Enigma` has a member `Avalonia`, so an inline `Avalonia.Something` written *inside*
  `namespace Enigma.HardCopy.…` resolves against `Enigma.Avalonia` and fails to compile. Today no source
  file does this (verified: the only match is prose in a doc comment), and the house rule — explicit `using`
  directives at **file scope, above the namespace** — keeps it that way.
- **`AVLN*` XAML warnings are not promoted by `TreatWarningsAsErrors`** (they come from an MSBuild task), so
  `Build succeeded` can hide them. Every phase that touches `.axaml` reads the warning count explicitly.
- **`GenerateDocumentationFile` is true for Core only**, not for Desktop, so a malformed `cref` in new
  Desktop code is not a build error. House style still documents every public member.
- **The library ships no `AddEnigmaServices()`** — the registration extension for its six singletons is ours
  to write.
- **`ContentDialog` handles `Escape` only.** `DefaultButton` declares intent and there is no Enter handling;
  a dialog that should submit on Enter needs an explicit `KeyBinding` in its content. Dismissal (Escape or a
  scrim click) yields `DialogResult.None`, **not** `Close`, so `== DialogResult.Primary` is the only correct
  check for "the user agreed".
- **`ContentDialog` keeps its `Content` until the next `ShowAsync`.** Both confirmations therefore carry only
  hashes and prose — **never** code payload text, which is the secret this app exists to protect.
- **Nothing on `INavigationService` throws.** A `PageFactory` or lifecycle failure is reported on
  `NavigationFailed` with a `Phase` string, and a `PageFactory` failure clears `CurrentPage` to `null` — an
  empty window with no error unless the event is subscribed. Navigation is also serialized by a
  `SemaphoreSlim` taken with a **zero** timeout: a navigation requested while one is in flight is **dropped**,
  not queued.
- **The publish stage is 120 MB (linux-x64) / 127 MB (win-x64)** today, self-contained and single-file with no
  trimming. The new assemblies add roughly **9 MB** unzipped — Phosphor ~3.9 MB, BouncyCastle ~4.7 MB, the
  library and the two icon assemblies ~0.3 MB — about 7 %. Recorded in the release notes so it is not a
  surprise.
- **Line endings are clean.** `.gitattributes` declares `* text=auto eol=lf` and every tracked file is LF; no
  normalization is needed and none is in scope.

## Architecture

### What the seams are, and why they barely move

The app's ViewModels never name an Avalonia type: pickers enter through the app's own `IFileDialogService`,
which deals in `IPickedFile` / `ISaveTarget` — an abstraction that exists, in its own words, so "a ViewModel
that took an `IStorageFile` could only be tested by standing up a windowing platform". The library's services
do not satisfy that on their own: `IContentDialogService.ShowAsync(Action<ContentDialog>)` hands the caller an
Avalonia control, and its picker service deals in `IStorageFile` and `FilePickerOpenOptions`. So:

> **The library's six services are consumed only inside `src/Enigma.HardCopy.Desktop/Services/` and
> `Hosting/`, behind the app's own interfaces. The one exception is `INavigationService`, which
> `MainWindowViewModel` exposes for binding.**

That is also what keeps the Desktop suite alive: `BackupViewModelTests` and `RecoverViewModelTests` new up the
real ViewModels against `FakeFileDialogService`, `FakePickedFile`, `FakeSaveTarget` and `FakePdfComposer`, and
none of those change.

| ViewModel-facing seam | Before | After |
|---|---|---|
| `IFileDialogService` (app's own, **interface unchanged**) | `Window.StorageProvider` directly | library `IFileDialogService`, options overloads, handles kept |
| `IProgressOverlay` (**new**) | inline `ProgressBar` + `ProgressText` per page | `IOverlayService` + our `ProgressOverlayCard` |
| `INotificationService` (**new**) | `Border.notice` bound to `StatusMessage` | `IInfoBarService` |
| `IConfirmationService` (**new**) | — | `IContentDialogService` |
| `IThemeService` (**new**) | — | `Application.Current.RequestedThemeVariant` |
| `IAppSettingsStore` (**new**) | — | `<config>/Enigma.HardCopy/settings.json` |
| `INavigationService` (library, **the exception**) | `TabControl` + `ViewLocator` + `SelectedPageIndex` | bound straight from `MainWindowViewModel` |
| `IPickedFile` / `ISaveTarget` | `IStorageFile` wrappers | unchanged |

`ViewLocator` and `PageViewModel` are deleted. `StorageProviderFile` / `StorageProviderSaveTarget` and the
drag-and-drop handler in `RecoverView.axaml.cs` are unchanged — a drop is still adapted through
`StorageProviderFile.Adapt(e.DataTransfer.TryGetFiles())`.

### Window composition

```
Window  Background="{DynamicResource EnigmaBackgroundBrush}"
└─ Panel                            ← z-stacks; the hosts are the LAST children
   ├─ DockPanel
   │  ├─ nav:NavigationView         ← DockPanel.Dock="Left", Orientation="Vertical", PaneSize="90"
   │  └─ ContentControl             ← Content="{Binding Navigation.CurrentPage}"
   ├─ controls:Overlay          x:Name="HostOverlay"     ← first host: the run scrim
   ├─ contentDialog:ContentDialog x:Name="HostDialog"    ← above the scrim
   └─ infoBar:InfoBar           x:Name="HostInfoBar"     ← always on top
```

The host order is **`Overlay` → `ContentDialog` → `InfoBar`**, deliberately *not* the order in the library's
own `setup.md`. A `Panel` z-stacks its children, so the documented order would draw the overlay's scrim on top
of a `ContentDialog`. No confirmation is raised mid-run today, but the ordering costs nothing, documents the
constraint, and must not be "corrected" later.

Five `xmlns` prefixes are needed: `…Controls.Navigation`, `…Controls` (`Overlay`), `…Controls.ContentDialog`,
`…Controls.InfoBar`, `…Controls.Editors`, plus `xmlns:ei="https://github.com/josueclement/Enigma.Icons"` for
the icon markup extensions. A lone `using:Enigma.Avalonia.Desktop.Controls` reaches only `SettingsCard`,
`SettingsCardExpander` and `Overlay`.

The six `ContentDialog` size properties are set **on the host in XAML** (`DialogMinWidth="380"`,
`DialogMaxWidth="640"`, `DialogMaxHeight="480"`) — they are not reset between dialogs.

### Startup wiring (`App.axaml.cs`)

Unchanged in shape — build the `IHost`, `Start()` it (never `Run()`), resolve `MainWindow`, stop the host on
`Exit`. Added, all **before the window is shown**, because every one of these services throws
`InvalidOperationException` from its first call if it is skipped:

```
IContentDialogService.RegisterHost(window.HostDialog)
IOverlayService      .RegisterHost(window.HostOverlay)
IInfoBarService      .RegisterHost(window.HostInfoBar)
IFileDialogService   .SetStorageProvider(window.StorageProvider)     ← the library's
IFolderDialogService .SetStorageProvider(window.StorageProvider)     ← registered, unused
```

then the `PageFactory` is pointed at the container, `NavigationFailed` is subscribed to a logger, the persisted
theme variant is applied to `Application.Current.RequestedThemeVariant`, and finally the rail's first item is
selected — navigation happens **here**, not in a ViewModel constructor.

### Theme

`App.axaml` keeps `<FluentTheme />` in `Application.Styles` (the library themes only its own controls plus
`TextBox` and `ComboBox`; dropping it leaves every other framework control — including those nested inside the
library's templates — without one) and merges
`avares://Enigma.Avalonia.Desktop/Themes/Fluent.axaml` as a **`ResourceInclude`** in `Application.Resources`
(a `StyleInclude` fails the build with `AVLN2000`), then `ProgressOverlayCard.axaml` **after** it — an override
merged before is silently ignored, since the last dictionary wins on a duplicate key.

The nine `App*`-style brushes and their `ThemeDictionaries` block are retired in favour of the library's keys,
always through `DynamicResource` — `StaticResource` freezes the colour and the control stops following a
variant switch:

| Retired | Replacement |
|---|---|
| `SubtleForegroundBrush` | `EnigmaForegroundSecondaryBrush` |
| `CardBackgroundBrush` | `EnigmaSurfaceBrush` |
| `CardBorderBrush` | `EnigmaBorderSubtleBrush` |
| `SuccessForegroundBrush` | `EnigmaSuccessBrush` |
| `WarningForegroundBrush` | `EnigmaWarningBrush` |
| `ErrorForegroundBrush` | `EnigmaErrorBrush` |
| `SuccessBackgroundBrush` | `EnigmaSuccessBackgroundBrush` |
| `WarningBackgroundBrush` | `EnigmaWarningBackgroundBrush` |
| `ErrorBackgroundBrush` | `EnigmaErrorBackgroundBrush` |

The `notice` `Border`s take `Enigma…BorderBrush` for their border (the library ships a
`…Background` / `…Border` pair per severity, which is what the `InfoBar` itself uses).

Style classes: `page`, `h1`, `h2`, `body`, `hint`, `field`, `value`, `mono` (both selectors) and `log` all
stay, repointed. `notice` and `notice.warning` stay — the large-input warning and the metadata-missing warning
are still inline. `notice.success` and `notice.error` (and their `> TextBlock` variants) are **deleted** as
unused once outcomes move to the `InfoBar`. The three `:is(TextBlock).success/.warning/.error` classes stay:
the activity-log `DataTemplate` binds them.

`TextBox` and `ComboBox` restyle themselves — the dictionary overrides FluentTheme's own `TextControl*` /
`ComboBox*` keys — so neither is hand-styled.

### Icons

Structural assignments, all verified against `PhosphorIcon`:

| Slot | Icon |
|---|---|
| Rail — Backup | `QrCode` |
| Rail — Recover | `ArrowsCounterClockwise` |
| Rail footer — Settings | `Gear` |
| Settings card — theme | `Palette` |
| Backup `SettingsCard` — density | `Barcode` |
| *Save anyway* dialog | `ShieldWarning`, with `IconBrush` = `EnigmaWarningBrush` |
| *Start over* dialog | `WarningCircle`, with `IconBrush` = `EnigmaWarningBrush` |

Section headers and command buttons draw from the same verified pool (`File`, `FilePdf`, `Files`, `Images`,
`Printer`, `Keyboard`, `Fingerprint`, `FloppyDisk`, `ShieldCheck`, `Broom`). XAML uses
`{ei:IconGeometry QrCode}`; code building `NavigationItem`s uses
`PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.QrCode, PhosphorWeight.Regular).ToGeometry()`.

### Settings storage

`AppSettings` — a `FormatVersion` int (constant `1`, refused if higher) and a `Theme` value
(`System` / `Light` / `Dark`). `IAppSettingsStore.Load()` returns defaults for a missing, unreadable or
malformed file, logging at warning level; `Save()` writes then replaces, and never throws into the UI. Path:
`Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "Enigma.HardCopy", "settings.json")`
— one code path that lands in `%APPDATA%` on Windows and `~/.config` on Linux.

## Package versions (CPM — `Directory.Packages.props`)

Added to the Desktop group, which stays version-coupled with Avalonia 12.1.1:

| Package | Version | Why |
|---|---|---|
| `Enigma.Avalonia.Desktop` | 1.0.0 | the control library |
| `Enigma.Icons` | 1.0.0 | icon core (`Geometry` conversion) |
| `Enigma.Icons.Phosphor` | 1.0.0 | the `PhosphorIcon` glyph set |
| `Enigma.Icons.Avalonia` | 1.0.0 | the `Icon` control + `{ei:IconGeometry}` markup extensions |

`Avalonia.Themes.Fluent` and `CommunityToolkit.Mvvm` stay explicit references — they now also arrive
transitively, and keeping them pinned here documents the versions rather than inheriting them. Nothing else
moves; the Avalonia set is *not* bumped as part of this item.

## Phases

### PHASE01 — Package, theme & icon foundation (TODO)

Branch: `feature/feature-5cbc-phase01-theme-foundation`

1. Add the four packages to `Directory.Packages.props` (Desktop group, with the comment explaining the
   coupling) and version-less `PackageReference`s to `Enigma.HardCopy.Desktop.csproj`.
2. `App.axaml`: keep `<FluentTheme />`; merge the library dictionary as a `ResourceInclude`; delete the
   `ThemeDictionaries` block holding the nine brushes.
3. Repoint every brush reference in the style classes and in `BackupView.axaml` / `RecoverView.axaml` to its
   `Enigma*` equivalent (table above), all `DynamicResource`. Keep every style class for now — the outcome
   `notice` blocks are removed in PHASE03, not here.
4. Set `Background="{DynamicResource EnigmaBackgroundBrush}"` on `MainWindow` (the theme paints controls, not
   windows).
5. Confirm the icon markup extension resolves before later phases depend on it: one icon in the UI (the
   *Generate PDF* button) via `{ei:IconGeometry …}`, and one `Geometry` built from C# via
   `PhosphorIconSet.Instance.GetGlyph(...).ToGeometry()`.

**Acceptance:** Debug **and** Release build clean — zero warnings **and zero `AVLN*` warnings**; the full
suite green with **no test changes at all**; the app launches and every panel, list, notice and status colour
still reads correctly in both variants (screenshots in the completion doc); no retired brush key remains
anywhere in the solution (`grep`).

### PHASE02 — NavigationView shell, hosts & pickers (TODO)

Branch: `feature/feature-5cbc-phase02-shell-services`

1. `Hosting/EnigmaAvaloniaServiceCollectionExtensions.cs` — `AddEnigmaAvaloniaDesktop()` registering the
   library's six singletons with `TryAddSingleton`. `IFolderDialogService` is registered for completeness and
   unused: this app picks files, never folders.
2. `Hosting/ServiceCollectionExtensions.cs` — move `App.ConfigureServices`'s registrations here as
   `AddEnigmaHardCopyDesktop()`, so a test can assert them. **Keep the library's registrations in their own
   file**: a single file importing both `Enigma.HardCopy.Desktop.Services` and
   `Enigma.Avalonia.Desktop.Services` gets `CS0104` on `IFileDialogService`.
3. Views become DI-resolvable: `AddTransient<BackupView>()`, `AddTransient<RecoverView>()` (a fresh control
   per navigation, no leaked visual tree); the page ViewModels stay singletons, so page state survives
   leaving a page. `MainWindow` stays a singleton.
4. `MainWindow.axaml`: replace the `TabControl` with the `Panel` → `DockPanel` → `NavigationView` +
   `ContentControl` composition above, and add the three `x:Name`d hosts in the order
   `HostOverlay` → `HostDialog` → `HostInfoBar`, with the dialog size properties set on the host.
5. `MainWindowViewModel`: takes `IServiceProvider` and `INavigationService`, sets `PageFactory` to a
   container factory, builds the two `Items` and the footer *Settings* item (added in PHASE04 — until then
   the footer is empty), exposes `Navigation` and keeps `Title`. **It selects nothing**: `App` assigns the
   first item after startup. Delete `SelectedPageIndex`, `CurrentPage`, `Pages`, `Backup`, `Recover`.
6. Delete `ViewLocator.cs` and `ViewModels/PageViewModel.cs`; `BackupViewModel` / `RecoverViewModel` derive
   from `ObservableObject` and keep their `Title` override as a plain property only if still used — otherwise
   the rail headers come from `Strings.NavBackup` / `Strings.NavRecover` directly. Remove the
   `Application.DataTemplates` block from `App.axaml`.
7. `App.axaml.cs`: the five `RegisterHost` / `SetStorageProvider` calls, the `PageFactory` assignment, the
   `NavigationFailed` subscription (logging `Phase` and `Exception` — nothing on that service throws), and the
   initial `SelectedItem`.
8. `StorageProviderFileDialogService`: constructor takes the library's `IFileDialogService` (reached through
   an alias — see *Risks*) instead of `MainWindow`; each of the four methods keeps its title, filters,
   `SuggestedFileName`, `DefaultExtension` and `ShowOverwritePrompt` verbatim, and keeps returning
   `StorageProviderFile` / `StorageProviderSaveTarget`. The `MainWindow`-taking factory registration goes.
9. Tests: rewrite `MainWindowViewModelTests` against the rail — two items in order with headers from
   `Strings` and non-null `IconData`, the right `PageType` / `PageViewModelType`, `Navigation` exposed, and
   **no navigation performed by the constructor** (`CurrentPage` still `null`). Add
   `Hosting/ServiceCollectionExtensionsTests` asserting that both extensions register what the app resolves,
   including the library's six.

**Acceptance:** build clean, zero `AVLN*` warnings; the full suite passes **with no changes to any Backup or
Recover ViewModel test**; manually, in the running app — the rail switches between the two pages, page state
survives switching away and back (a chosen file, a half-fed session, typed text), all four pickers (backup
file, images, PDF destination, recovered-file destination) still return usable paths, and drag-and-drop onto
the recovery page still imports. Screenshots of both variants.

### PHASE03 — Page restyle, overlay, InfoBar & confirmations (TODO)

Branch: `feature/feature-5cbc-phase03-page-restyle`

1. Backup page: the density row becomes a `SettingsCard` (`Header` / `Description` from the existing
   `BackupChunkSizeLabel` / `BackupChunkSizeNote`, `IconData` `Barcode`, the `ComboBox` as `Content`); icons
   on the three command buttons and the section headers; the section `Border`s keep their shape.
2. Recovery page: the manual-code box becomes a `MultiLineTextEditor` with `Title`, `PlaceholderText` and
   *Add codes* in `ActionContent`, keeping its `MinHeight` / `MaxHeight` and the `mono` styling; icons on the
   import, add, recover, save-anyway and start-over commands.
3. `Views/ProgressOverlayCard.cs` + `Views/ProgressOverlayCard.axaml` — a `ContentControl` subclass of ours
   (the library ships none) exposing `Title`, `Message`, `IsIndeterminate`, `Progress` and `CancelCommand`,
   with its control theme merged after the library's dictionary. `Cancel` is hidden when `CancelCommand` is
   null, which is how import, assembly and saving show a card without one.
4. `Services/IProgressOverlay.cs` + `ProgressOverlay.cs` over `IOverlayService`: `ShowAsync(title,
   cancelCommand?)`, `Update(message, isIndeterminate, percent)`, `HideAsync()`, with `show`/`hide` wrapped
   in `try`/`finally` **inside the implementation, not at the call sites**, so an exception can never leave
   the scrim up. An update arriving after the card is gone is a no-op, not a failure.
5. `Services/INotificationService.cs` + `NotificationService.cs` over `IInfoBarService`, mapping
   `MessageSeverity` → `InfoBarSeverity` (`Success`→`Success`, `Warning`→`Warning`, `Error`→`Error`,
   `Information`→`Info`) with a per-severity title string. The library's `ShowAsync` completes when the banner
   is **dismissed**, not when it appears, so the task is not awaited into the run.
6. `Services/IConfirmationService.cs` + `ConfirmationService.cs` over `IContentDialogService`: two methods —
   `ConfirmUnverifiedSaveAsync(expectedSha256, actualSha256)` and `ConfirmStartOverAsync()` — each returning
   `bool`, treating **only `DialogResult.Primary`** as agreement. Content is prose plus the two hashes,
   never code text.
7. `BackupViewModel` / `RecoverViewModel` change **only** where they must: `IsBusy` / `ProgressText` now drive
   `IProgressOverlay`, `Message` also publishes through `INotificationService`, `OnSaveAnywayAsync` and
   `OnStartOver` first ask `IConfirmationService` (start-over only when the session holds codes). The
   `StatusMessage` / `MessageSeverity` surface is unchanged, so `OutcomeMessagesTests` and
   `StatusMessageTests` cannot move.
8. Remove the two inline outcome `Border.notice` blocks and the two inline `ProgressBar` + `ProgressText`
   blocks; delete the now-unused `notice.success` / `notice.error` styles. The recovery completeness
   `ProgressBar`, the missing-index list, the activity log, the large-input warning and the metadata-missing
   warning all stay.
9. New strings in `Strings.resx` (+ `Strings.cs` properties, with translator comments on every format): the
   four overlay card titles, the four notification titles, and the two dialogs' titles, bodies and button
   labels.
10. Tests: new fakes for the three seams; assert that a generation opens and closes the overlay exactly once
    including on failure and on cancellation, that each outcome publishes exactly one notification of the
    right severity, that a **declined** *Save anyway* writes nothing and leaves the offer standing, that an
    accepted one writes and withdraws it, and that *Start over* asks only when codes are present.

**Acceptance:** build clean, zero `AVLN*` warnings; suite green including the new assertions, with the
pre-existing Backup/Recover ViewModel tests unmodified; manually — a generation, an image import, an assembly
and a save each raise the card with live stage text, Cancel works on generation and is absent elsewhere, a
failing run still removes the scrim, the InfoBar reports every outcome at the right severity, both
confirmations behave on button, `Escape` and scrim click, and the whole app reads correctly in Light and Dark
(screenshots).

### PHASE04 — Settings page & theme preference (TODO)

Branch: `feature/feature-5cbc-phase04-settings`

1. `Settings/AppSettings.cs`, `Settings/AppTheme.cs`, `Settings/IAppSettingsStore.cs`,
   `Settings/AppSettingsStore.cs` as described in *Settings storage*.
2. `Services/IThemeService.cs` + `ThemeService.cs` — the only place that assigns
   `Application.Current.RequestedThemeVariant` (`ThemeVariant.Default` for `System`), so no ViewModel names
   Avalonia.
3. `ViewModels/SettingsViewModel.cs` + `Views/SettingsView.axaml(.cs)` — one `SettingsCard` row
   (`IconData` `Palette`) holding the three choices; selecting one applies through `IThemeService` and
   persists through `IAppSettingsStore`, exactly once per change.
4. Register both in DI (view transient, ViewModel singleton) and add the footer `NavigationItem`
   (`Gear`, `Strings.NavSettings`) to `MainWindowViewModel`.
5. `App.axaml.cs` loads the stored variant and applies it **before the window is shown**;
   `App.axaml` keeps `RequestedThemeVariant="Default"` as the pre-load default.
6. New strings: `NavSettings`, the page header and intro, the theme card's header and description, and the
   three choice labels.
7. Tests: store round-trip; missing file → `System`; corrupt JSON → `System` and no throw; unknown enum value
   → `System`; a higher `formatVersion` → `System` and a warning; the ViewModel applies through the seam and
   persists exactly once per change.

**Acceptance:** build clean, zero `AVLN*` warnings; the new tests plus the whole suite green; manually — the
footer item opens the page, each of the three choices repaints the app immediately, the choice survives a
restart, deleting `settings.json` returns the app to following the OS, and a hand-corrupted `settings.json`
still starts the app. Screenshots of both variants **and** of the Settings page.

### PHASE05 — Release readiness: 1.1.0 (TODO)

Branch: `feature/feature-5cbc-phase05-release`

1. `<Version>1.1.0</Version>` in `Directory.Build.props`, and `app.manifest`'s `assemblyIdentity` version to
   `1.1.0.0` (Windows requires the 4-part form; the props comment says to bump both together).
2. `RELEASENOTES.md`: a top `1.1.0` section — what the UI now is (rail, settings page, theme preference,
   in-window confirmations, overlay, notifications), the four new packages with the ~9 MB artifact note, the
   new `settings.json` (additive; absent is normal), and an explicit compatibility statement that the `EHC1`
   grammar, the metadata grammar, the printed instructions and the PDF layout are unchanged, so **every 1.0
   paper backup still recovers**.
3. `README.md`: the what's-new callout and the Install table's zip names at 1.1.0; the page walkthroughs
   updated where the UI moved (the rail instead of tabs, the overlay, the InfoBar, the two confirmations, the
   Settings page); a *Settings* subsection; the dependency line extended with the library and the icon packs,
   with Phosphor's upstream MIT attribution; the project-layout table if it moved.
4. `dotnet list package --outdated` and `--vulnerable --include-transitive`, findings recorded in the notes.
   The Avalonia set moves as a unit or not at all — and not in this item.
5. Print the merge / tag / publish runbook. **No zips, no tag, no push** — `scripts/publish.sh`,
   `scripts/publish.ps1`, the win-x64 build on Windows and the GitHub release stay with the user.

**Acceptance:** Release build clean and suite green at 1.1.0; no stale `1.0.0` version string left in the docs
or the manifest; both variants screenshotted one last time on the finished app; the runbook printed.

## Files expected to change

**Created** — `Hosting/EnigmaAvaloniaServiceCollectionExtensions.cs`, `Hosting/ServiceCollectionExtensions.cs`;
`Views/ProgressOverlayCard.cs` + `Views/ProgressOverlayCard.axaml`; `Views/SettingsView.axaml(.cs)`;
`ViewModels/SettingsViewModel.cs`; `Services/IProgressOverlay.cs` + `ProgressOverlay.cs`;
`Services/INotificationService.cs` + `NotificationService.cs`; `Services/IConfirmationService.cs` +
`ConfirmationService.cs`; `Services/IThemeService.cs` + `ThemeService.cs`; `Settings/AppSettings.cs`,
`Settings/AppTheme.cs`, `Settings/IAppSettingsStore.cs`, `Settings/AppSettingsStore.cs`; test files for the
settings store, the four new seams and the DI registrations, plus their fakes;
`docs/done/FEATURE-5CBC-PHASE01..05.md`.

**Modified** — `Directory.Packages.props`, `Directory.Build.props` (version),
`Enigma.HardCopy.Desktop.csproj`, `app.manifest`, `App.axaml`, `App.axaml.cs`, `Views/MainWindow.axaml`,
`ViewModels/MainWindowViewModel.cs`, `Views/BackupView.axaml`, `Views/RecoverView.axaml`,
`ViewModels/BackupViewModel.cs`, `ViewModels/RecoverViewModel.cs`,
`Services/StorageProviderFileDialogService.cs`, `Resources/Strings.resx`, `Resources/Strings.cs`,
`tests/…/MainWindowViewModelTests.cs`, `README.md`, `RELEASENOTES.md`, `docs/roadmap.md`, and this file.

**Deleted** — `ViewLocator.cs`, `ViewModels/PageViewModel.cs`.

**Expected untouched** — all of `src/Enigma.HardCopy.Core/`, all of `tests/Enigma.HardCopy.Core.UnitTests/`,
`Services/IFileDialogService.cs`, `Services/IPickedFile.cs`, `Services/ISaveTarget.cs`,
`Services/StorageProviderFile.cs`, `Services/StorageProviderSaveTarget.cs`, `Views/RecoverView.axaml.cs`
(the drop handler), `Program.cs`, `scripts/`, `global.json`, `.editorconfig`, `.gitattributes`, and every
Backup/Recover/Outcome/Status/ChunkSize/Strings test.

## Risks

- **The `IFileDialogService` collision.** Ours and the library's share a simple name. Inside
  `namespace Enigma.HardCopy.Desktop.Services` ours wins silently, which is convenient until a file needs the
  library's — then it must be aliased
  (`using LibraryFileDialogs = Enigma.Avalonia.Desktop.Services.IFileDialogService;`). A file that owns
  neither and imports both fails with `CS0104`, which is why the two registration extensions live in separate
  files. Getting this wrong is a compile error, not a silent bug — but it will look baffling without this
  note.
- **`Enigma.Avalonia` shadowing `Avalonia`.** Any new code that writes `Avalonia.Something` inline inside
  `namespace Enigma.HardCopy.…` will not compile once the package is referenced. Keep `using` directives at
  file scope, above the namespace; reach for `global::Avalonia.…` only where a using cannot express it.
- **The host z-order.** If the three hosts are reordered to match the library's `setup.md`, any future
  mid-run dialog disappears behind the scrim and the app looks hung. Asserted by a comment in
  `MainWindow.axaml`.
- **A stuck overlay.** An exception between show and hide leaves the scrim up and the app unusable. Mitigated
  by `try`/`finally` inside `ProgressOverlay`, not at the call sites, plus tests that a failing and a
  cancelled run both still hide it.
- **Silent navigation failures.** Nothing on `INavigationService` throws: a `PageFactory` failure clears
  `CurrentPage` to `null`, so a DI mistake shows as an **empty window with no error**. `NavigationFailed`
  must be subscribed and logged in PHASE02, and a `Phase`-tagged log line is the acceptance evidence.
- **Dropped navigations.** The service's semaphore has a zero timeout, so a navigation requested while one is
  in flight is discarded. Never fire two back to back and assume both land.
- **Page state and transient views.** ViewModels are singletons and views transient; if a view is
  accidentally registered singleton the visual tree leaks across navigations, and if a ViewModel is
  accidentally transient the user's chosen file and half-fed session vanish on every page switch. Covered by
  the PHASE02 manual check.
- **Plaintext lingering in a closed dialog.** `ContentDialog` keeps its `Content` until the next `ShowAsync`.
  Neither confirmation may carry code payload text — hashes and prose only. This is an acceptance criterion,
  not a convention.
- **Existing ViewModel tests quietly rewritten.** If PHASE02 or PHASE03 needs to change one of the six
  protected test classes, that is a signal the seams moved further than planned — raise it before changing
  the test.
- **`AVLN` warnings hiding behind a green build.** `TreatWarningsAsErrors` does not promote them. Every phase
  reads the count.
- **Artifact growth (~9 MB per RID, ~7 %)** from Phosphor plus the transitive `Enigma.Core` / BouncyCastle
  that no code here calls. Accepted at planning; recorded in the release notes.

## Overall acceptance criteria

1. The Desktop app references `Enigma.Avalonia.Desktop` and uses its theme, `NavigationView`, `ContentDialog`,
   `Overlay`, `InfoBar`, file-picker service, `SettingsCard` and typed editors; no retired brush key, no
   `TabControl`, no `ViewLocator` and no `PageViewModel` remain.
2. `Enigma.HardCopy.Core` is byte-for-byte unchanged; no ViewModel references an Avalonia or library type
   except `MainWindowViewModel`'s `INavigationService`; every other library service is consumed only from
   `Desktop/Services/` and `Desktop/Hosting/`.
3. Debug and Release builds are clean — zero warnings and zero `AVLN*` warnings — and the full suite passes,
   with `BackupViewModelTests`, `RecoverViewModelTests`, `OutcomeMessagesTests`, `StatusMessageTests`,
   `ChunkSizeOptionTests` and `StringsTests` unmodified.
4. Backup and recovery still work end to end: hash and estimate before generating, cancellation mid-run,
   mixed image and typed-code recovery, duplicate and foreign-backup rejection, and the hash-mismatch refusal
   with its explicit confirmed save.
5. The theme preference persists, defaults to following the OS, and survives a missing, corrupt or
   future-versioned settings file.
6. Every phase is verified with the app running on Linux and screenshots of both variants in its completion
   doc.
7. The repository is release-ready at 1.1.0 — version, manifest, release notes and README consistent — with
   the zips, the tag and the GitHub release left to the user.
