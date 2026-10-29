# FEATURE-7B95 — Uniform About dialog

## Summary

The desktop app now has the About dialog that every Enigma app shares, ported from Enigma.GitClient's with the
same markup, size and brushes. It shows:

- the 96 px app icon (`Assets/app-256.png`);
- *Enigma HardCopy*, 18 SemiBold;
- *Version X.Y.Z* in the secondary brush;
- *Build <sha>*, a quieter 11 px tertiary line whose revision is selectable, and which is absent when the build
  recorded no revision;
- the copyright;
- one *Close* button in the window's `ContentDialog`.

The dialog opens from a new **About** row on the Settings page. The row's description is "Enigma HardCopy, version
X.Y.Z." and its *About Enigma HardCopy* button opens the dialog.

The version comes from GitClient's `ProductVersion` parser, ported verbatim. It splits the informational version
at `+` and shortens the sha to 7 characters, then falls back to the assembly version, then to `0.0.0`.
`ProductInformation` reads the version and the copyright from the Desktop assembly. Every label is in
`Strings.resx`.

## Files/modules touched

- Created: `src/Enigma.HardCopy.Desktop/Diagnostics/ProductVersion.cs`, `Diagnostics/ProductInformation.cs`
- Created: `src/Enigma.HardCopy.Desktop/ViewModels/AboutViewModel.cs`
- Created: `src/Enigma.HardCopy.Desktop/Views/AboutView.axaml`, `Views/AboutView.axaml.cs`
- Created: `src/Enigma.HardCopy.Desktop/Services/IAboutDialogService.cs`, `Services/AboutDialogService.cs`
- Modified: `src/Enigma.HardCopy.Desktop/ViewModels/SettingsViewModel.cs`, `Views/SettingsView.axaml`
- Modified: `src/Enigma.HardCopy.Desktop/Hosting/ServiceCollectionExtensions.cs`
- Modified: `src/Enigma.HardCopy.Desktop/Resources/Strings.resx`, `Resources/Strings.cs`
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/Diagnostics/ProductVersionTests.cs`, `AboutViewModelTests.cs`,
  `TestDoubles/FakeAboutDialogService.cs`
- Modified: `tests/Enigma.HardCopy.Desktop.UnitTests/SettingsViewModelTests.cs`,
  `Hosting/ServiceCollectionExtensionsTests.cs`
- Modified: `README.md` (*Settings*)
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-7B95.md`

## Deviations & follow-ups

- The Settings page's intro still talks about appearance only. The intro is true of the page's one preference,
  and the About row is not a preference. The view's comment says so.
- Follow-up, not done: the window title still reads "Enigma.HardCopy", the code name. Moving it to the display
  name would make the shell match the About dialog.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| About row description | "Enigma HardCopy, version X.Y.Z." (`SettingsAboutNoteFormat`) | The plan's "product name and version". Computed once, because neither changes while the app runs |
| Service visibility | `internal sealed AboutDialogService` behind a public interface | The idiom of `ConfirmationService` and the other shell seams |
| How to look at the result without a headless test package | A scratch file-based script on Avalonia.Headless 12.1.4 + Skia. It rendered the Settings page and the open About dialog in both variants through the real App theme | Shows the layout and brushes. The solution gains no test infrastructure |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **818 passed**, 0 failed, up from 799. The 19
  new or extended tests are:
  - `ProductVersionTests`: 10 cases;
  - `AboutViewModelTests`: 6, including that the running build states a plain X.Y.Z version and the solution's
    copyright;
  - `SettingsViewModelTests`: the command shows the dialog once, the about note, and the new null guard;
  - the DI lifetime and resolution of `IAboutDialogService`.
- Rendered check, Dark and Light: icon, *Enigma HardCopy*, *Version 1.2.0*, *Build 6e15497*, the copyright and
  *Close* are laid out as in GitClient's dialog. The Settings page shows the About row under Appearance.
- Doc sweep: README *Settings* now describes the About row. Nothing else names the Settings page's contents.
