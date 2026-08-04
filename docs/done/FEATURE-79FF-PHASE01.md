# FEATURE-79FF-PHASE01 — Repo & solution scaffolding — DONE

**Branch:** `feature/feature-79ff-phase01-scaffolding`
**Date:** 2026-08-04

## Summary

Bootstrapped the Enigma.HardCopy repository and solution from nothing: git repository, the house
hygiene/config root files, `Enigma.HardCopy.slnx` with four projects, and an Avalonia 12 desktop app
wired to an `IHost` container. The solution builds clean in Debug *and* Release with zero warnings,
and both test projects run green. Everything downstream phases need is in place; no domain logic was
implemented beyond the one format-constants type the placeholder tests assert against.

The repository did not exist as a git repo at build time. Per the user's decision, `git init` was run
here and the phase branch was created on the unborn `HEAD` — so the whole repository history begins
with this commit on `feature/feature-79ff-phase01-scaffolding`, and no base branch exists yet.

## Files/modules touched

All files are **created**; nothing was modified or deleted (empty repository).

**Root config**
- `.gitignore`, `.gitattributes` — verbatim from the house `git-repo-hygiene` templates.
- `.editorconfig` — the full C# style/naming/analyzer file from `dotnet-solution-config` (not the
  minimal line-endings one).
- `Directory.Build.props` — house template, authors filled (`Josué Clément` / 2026).
- `Directory.Packages.props` — Central Package Management, full v1 package set (see *Versions*).
- `global.json` — .NET 10 SDK pin (`10.0.100`, `rollForward: latestFeature`) + the
  `"test": { "runner": "Microsoft.Testing.Platform" }` entry xUnit v3 requires.
- `LICENSE.md` — MIT, house template, 2026 / Josué Clément.
- `README.md` — short project stub (purpose, status, build commands, license); the full user guide
  is PHASE06's job.
- `RELEASENOTES.md` — 0-byte placeholder, filled at release time by `dotnet-release`.
- `Enigma.HardCopy.slnx` — `/src/` and `/tests/` solution folders, four projects.

**`src/Enigma.HardCopy.Core/`** (net10.0 library)
- `Enigma.HardCopy.Core.csproj` — `GenerateDocumentationFile`, the QuestPDF/QRCoder/ZXing/SkiaSharp
  references, and an in-file comment documenting the `netstandard2.0` → `net10.0` TFM deviation.
- `HardCopyFormat.cs` — barcode-format constants (`Magic`, `Version`, `Prefix` = `EHC1`, separators,
  `MetadataIndex`, the 45-char QR `AlphanumericCharset`), fully XML-documented.

**`src/Enigma.HardCopy.Desktop/`** (net10.0, Avalonia 12.1.1)
- `Enigma.HardCopy.Desktop.csproj` — `WinExe`, compiled bindings on, `ApplicationIcon`,
  `AvaloniaResource` glob, the Avalonia 12 package set with `AvaloniaUI.DiagnosticsSupport` stripped
  from non-Debug builds, CommunityToolkit.Mvvm, Hosting + Console/Debug logging.
- `Program.cs` — synchronous `[STAThread] Main`, `StartWithClassicDesktopLifetime`,
  `.WithDeveloperTools()` under `#if DEBUG`, `.WithInterFont()`, `.LogToTrace()`.
- `App.axaml` / `App.axaml.cs` — Fluent theme; `HostApplicationBuilder` built and started in
  `OnFrameworkInitializationCompleted`, `MainWindowViewModel` registered and resolved from DI into
  `desktop.MainWindow.DataContext`, host stopped on `desktop.Exit`.
- `Views/MainWindow.axaml` / `.axaml.cs` — empty shell window, `x:DataType` set for compiled
  bindings, `Icon="/Assets/app.ico"`.
- `ViewModels/MainWindowViewModel.cs` — `ObservableObject` with an explicit `field`-keyword
  `Title` property (no MVVM source generators, per the house style).
- `Assets/app.ico` — placeholder multi-resolution icon (16/32/48/256 px), generated here.
- `app.manifest` — PerMonitorV2 DPI awareness.

**`tests/`** (xUnit v3 3.2.2, MTP-native, `OutputType=Exe`, no `Microsoft.NET.Test.Sdk`)
- `Enigma.HardCopy.Core.UnitTests/` — csproj (+ `SkiaSharp.NativeAssets.Linux` so image decoding
  works on Linux from PHASE04 on, and the fixture copy-glob for `*.bin`/`*.txt`) and
  `HardCopyFormatTests.cs` (2 tests).
- `Enigma.HardCopy.Desktop.UnitTests/` — csproj and `MainWindowViewModelTests.cs` (2 tests: default
  title, `PropertyChanged` is raised).

**Workflow artifacts**
- `docs/roadmap.md`, `docs/plan/FEATURE-79FF.md` — statuses flipped; `docs/done/` created.

## Versions resolved at build time

The plan's named versions were all verified against nuget.org and are the current latest stables:
Avalonia **12.1.1** (ecosystem: Desktop, Themes.Fluent, Fonts.Inter), AvaloniaUI.DiagnosticsSupport
**2.2.3**, CommunityToolkit.Mvvm **8.4.2**, QuestPDF **2026.7.2**, QRCoder **1.8.0**, ZXing.Net
**0.16.11**, xunit.v3 **3.2.2**, coverlet.collector **10.0.1**, Microsoft.Extensions.* **10.0.10**.

Two items the plan named only generically:

- The ZXing SkiaSharp binding is **`ZXing.Net.Bindings.SkiaSharp` 0.16.22**.
- **SkiaSharp is pinned to 3.119.4, not the newest 4.151.0** — 3.119.4 is what Avalonia 12.1.1
  itself depends on, and the ZXing binding requires ≥ 3.119.1. Pinning to Avalonia's version keeps a
  single Skia runtime across the whole graph. It is now part of the version-coupled Avalonia group in
  `Directory.Packages.props` and must be bumped with it, never alone.

## Deviations & follow-ups

1. **Test projects renamed `*.Tests` → `*.UnitTests`.** The plan named them
   `Enigma.HardCopy.Core.Tests` / `.Desktop.Tests`; the house `xunit-v3` skill mandates the
   `<ProjectUnderTest>.UnitTests` suffix so a sibling `.IntegrationTests` can appear later without a
   rename. Raised at build time; the user chose the house convention. The plan's architecture block
   was corrected to match (the dated *Assumptions accepted at validation* section is left as the
   historical record).
2. **`HardCopyFormat` is slightly more than an empty placeholder.** PHASE01 asked for "one
   placeholder test per test project"; rather than assert a trivial `true`, the placeholder tests
   assert against the format constants transcribed from the plan's barcode specification. These
   constants are PHASE02's starting point, so the work is not throwaway — but it is marginally ahead
   of PHASE01's literal scope.
3. **`LICENSE.md`, not `LICENSE`.** The plan wrote "LICENSE (MIT)"; the house template is
   `LICENSE.md` and `README.md` links to it under that name.
4. **`README.md` is a short stub, not a 0-byte placeholder.** The plan asked for a stub;
   `dotnet-solution-setup` asks for an empty file. Split the difference: `README.md` carries a real
   (small) description, `RELEASENOTES.md` is the 0-byte one. PHASE06 replaces the README wholesale.
5. **App icon is placeholder art**, generated programmatically (dark page + three QR-ish blocks) so
   `<ApplicationIcon>` resolves and the taskbar icon isn't blurry. Final `.ico` art is user-supplied
   in PHASE06 — carried forward as PHASE06 already specifies.
6. **No base branch exists.** Because the repo was initialized on this branch, there is nothing to
   merge into yet. Consider creating `main` from this commit after committing.
7. **Line endings:** nothing to report — every file was authored LF and `.gitattributes`
   (`* text=auto eol=lf`) is present from the first commit, so no `git add --renormalize` pass is
   needed. No action taken, per the workflow's recommendation-only rule.
8. **Not yet exercised:** `Avalonia.Fonts.Inter` and `RequestedThemeVariant` are wired but visually
   unverified beyond app start; no `.resx` yet (PHASE05 owns localization from its first line of UI).

## Build/test evidence

```
dotnet build Enigma.HardCopy.slnx              → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build Enigma.HardCopy.slnx -c Release   → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --solution Enigma.HardCopy.slnx   → Passed! total: 4  failed: 0  succeeded: 4  skipped: 0
```

Zero warnings holds under `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` + the full
`.editorconfig`, and under `GenerateDocumentationFile` on Core (so every public member is documented).

Beyond the acceptance criteria, the app was smoke-launched on Linux/X11 for ~12 s to prove the
bootstrap is not merely compilable: the window appeared and the host logged
`Application started` → `Application is shutting down`, confirming DI resolution of
`MainWindowViewModel` and the `desktop.Exit` → `host.StopAsync()` path both work.

Release output was checked to confirm `AvaloniaUI.DiagnosticsSupport` is excluded from non-Debug
builds, and `git status` to confirm `bin/`/`obj/` are ignored.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | Hygiene files from house templates (`.gitignore`, `.gitattributes`, full `.editorconfig`, `Directory.Build.props` with authors, `Directory.Packages.props`) | ✅ |
| 2 | `global.json` pins the .NET 10 SDK and sets the MTP test runner | ✅ |
| 3 | `.slnx` + 4 projects per the architecture; CPM entries for the full package set | ✅ |
| 4 | Desktop bootstrapped per the avalonia skill (sync `Main`, classic desktop lifetime, IHost, empty `MainWindow` + VM from DI) | ✅ |
| 5 | `LICENSE` (MIT), `README.md` stub, `Assets/` placeholder icon wiring | ✅ (as `LICENSE.md`) |
| 6 | `dotnet build` zero warnings; `dotnet test` runs ≥1 placeholder test per test project | ✅ (0 warnings Debug + Release; 2 tests per project) |
