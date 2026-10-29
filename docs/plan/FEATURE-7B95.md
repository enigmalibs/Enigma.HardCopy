# FEATURE-7B95 — Uniform About dialog

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-7b95-about-dialog`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The desktop app gets the About dialog every Enigma app shares, with Enigma.GitClient's as the reference: same
layout, size, type scale, brushes and behaviour — the 96 px app icon, the product name, *Version X.Y.Z*, a quieter
selectable *Build <sha>* line (absent when the build recorded none), the copyright, and one *Close* button, shown
in the window's `ContentDialog`. It is opened from a new *About* card on the Settings page.

## Context & constraints

- **Reference** (`C:\Dev\EnigmaLibs\Enigma.GitClient\src\`):
  - `Enigma.GitClient.Desktop/Views/Dialogs/AboutView.axaml(.cs)` — `Width="380"`, `StackPanel Spacing="14"`,
    the 96 × 96 `/Assets/app-256.png`, `productName` 18 SemiBold `EnigmaForegroundBrush`, `version`
    `EnigmaForegroundSecondaryBrush`, `build` 11 `EnigmaForegroundTertiaryBrush` with a `SelectableTextBlock`,
    `copyright` 11 centred `EnigmaForegroundSecondaryBrush`;
  - `Enigma.GitClient.Desktop/ViewModels/Dialogs/AboutViewModel.cs` — the values injected through a second
    constructor, `HasBuildSha`, `HasCopyright`;
  - `Enigma.GitClient.Desktop/Services/AboutDialogService.cs` — `IAboutDialogService.ShowAsync()`; `Title = null`,
    `Content = view`, `CloseButtonText = "Close"`, `DefaultButton = DefaultButton.Close`;
  - `Enigma.GitClient.Core/Diagnostics/ProductVersion.cs` (parses `X.Y.Z+<sha>`, sha shortened to 7, fallback to
    the assembly version, `0.0.0` last) and `ProductInformation.cs` (`DisplayName`, `Version`, `BuildSha`,
    `Copyright` read from the assembly);
  - the Settings page's *About* `SettingsCardExpander` with a version row and an *About Enigma Git Client* button.
- **This codebase:** one `MainWindow` whose `HostDialog` (min 380, max 640 × 480) every question shares; the
  app's own seams wrap the library's services (`IConfirmationService` over `IContentDialogService`); every
  user-facing string is in `Resources/Strings.resx` behind `Strings` (a reflection test reads them all); folders
  are flat (`Services/`, `ViewModels/`, `Views/`); ViewModels derive from `ObservableObject`; tests have no
  Avalonia platform (no headless package) — ViewModels and pure logic are tested, views are not.
- `Assets/app-256.png` arrives with FEATURE-1148. `Directory.Build.props` stamps `Version` and `Copyright`; the
  .NET 10 SDK appends `+<sha>` to the informational version in a git checkout.

## Steps

1. `Diagnostics/ProductVersion.cs` — GitClient's record struct verbatim (namespace
   `Enigma.HardCopy.Desktop.Diagnostics`).
2. `Diagnostics/ProductInformation.cs` — `DisplayName` (from `Strings.AboutProductName`, "Enigma HardCopy"),
   `Version`, `BuildSha`, `Copyright`, read once from the Desktop assembly.
3. `ViewModels/AboutViewModel.cs` — GitClient's shape: the running-build constructor and the value-injecting one;
   `DisplayName`, `Version`, `BuildSha`, `HasBuildSha`, `Copyright`, `HasCopyright`.
4. `Views/AboutView.axaml(.cs)` — GitClient's markup verbatim, the labels from `Strings` (`AboutVersionLabel`,
   `AboutBuildLabel`).
5. `Services/IAboutDialogService.cs` + `Services/AboutDialogService.cs` (over `IContentDialogService`,
   `Strings.AboutClose`); registered as a singleton in `AddEnigmaHardCopyDesktop`.
6. Settings page: `SettingsViewModel` takes `IAboutDialogService` and exposes `ProductName`, `ProductVersion` and
   `OpenAboutCommand` (`AsyncRelayCommand`); `SettingsView` gains an *About* `SettingsCard` (`Info` icon,
   header *About*, description the product name and version) holding the *About Enigma HardCopy* button.
7. Strings: `AboutProductName`, `AboutVersionLabel`, `AboutBuildLabel`, `AboutClose`, `SettingsAboutLabel`,
   `SettingsAboutNoteFormat`, `SettingsAboutButton` — in the `.resx` and in `Strings`.
8. Tests: `ProductVersion.From` (plain, `+sha`, long sha shortened, empty before `+`, null → fallback → `0.0.0`);
   `AboutViewModel` (no sha → `HasBuildSha` false; empty copyright → `HasCopyright` false); `SettingsViewModel`
   (`OpenAboutCommand` calls the service once — a `FakeAboutDialogService`); DI resolves `IAboutDialogService`;
   `StringsTests` covers the new keys.
9. README *Settings*: the About card.

## Acceptance criteria

- The About dialog shows icon, name, version, build line (only with a sha) and copyright, with GitClient's sizes,
  spacing and brushes, and closes with *Close* or Escape.
- The Settings page's About card opens it.
- Solution builds with zero warnings; whole suite green.

## Out of scope

- An About in the Android Recover app; a menu, title-bar or keyboard entry point; links, credits or licences in
  the dialog (GitClient's has none).
- Renaming the window title ("Enigma.HardCopy").

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Entry point | An *About* card on the Settings page with a button | GitClient's and Enigma.Backup's place; the rail has no other natural home | A rail footer item; a keyboard shortcut |
| Card type | `SettingsCard` with the button as its control | The page's only other row is a `SettingsCard`; one row needs no expander | GitClient's `SettingsCardExpander` (its card holds git details HardCopy lacks) |
| Product display name | "Enigma HardCopy" | The house pattern ("Enigma Git Client", "Enigma Text Editor"); matches the Linux launcher name of FEATURE-3D14 | "Enigma.HardCopy" (the code name) |
| Where the version code lives | `Desktop/Diagnostics/` | Only the desktop app shows it; Core is the format library the Android app also ships | Core, as GitClient (whose Core needs it for HTTP) |
| Strings | In `Strings.resx` | Every user-facing string of this app is; the reflection test guards them | GitClient's literals |
| Seam | `IAboutDialogService` over `IContentDialogService` | GitClient's shape and this app's own seam idiom (`IConfirmationService`) | Building the dialog in the ViewModel |
| AboutViewModel base | Plain sealed class | Nothing changes while it is open; this app has no `ViewModelBase` | `ObservableObject` for symmetry |
| View tests | None (ViewModel, parser and wiring tested) | No headless platform in this solution, and adding one is infrastructure the spec does not ask for | Adding Avalonia.Headless |
