# FEATURE-E291 — Update NuGet packages

**Status:** DONE
**Type:** FEATURE (2 phases)
**Branch:** `feature/feature-e291-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every NuGet package of the solution on its latest release, part of the uniform Enigma-app baseline — except
where the latest release cannot coexist with the rest of a version-coupled set, which is then held back on the
newest coherent version and recorded. Done first in the run, so every later dev builds and tests against the
final dependencies.

## Context & constraints

- Central Package Management: every version is in `Directory.Packages.props`, with comments explaining each
  coupled set (Avalonia + Enigma libraries + SkiaSharp; CameraX with `Avalonia.Android`'s AndroidX set).
- `dotnet list package --outdated` (2026-10-29):
  - QuestPDF 2026.7.2 → 2026.9.1 (2026.9 renames `Settings.CheckIfAllTextGlyphsAreAvailable` to
    `ThrowOnMissingTextGlyphs`: CS0618 under `TreatWarningsAsErrors` — reverted in FEATURE-DFCB PHASE02);
  - the Avalonia set 12.1.1 → 12.1.4 (`Avalonia`, `.Desktop`, `.Themes.Fluent`, `.Fonts.Inter`, `.Android`);
  - Enigma.Avalonia.Desktop 1.0.0 → 1.3.0 (backward compatible per its release notes; its floor is Avalonia
    12.1.4); Enigma.Icons / .Phosphor / .Avalonia are already latest (1.0.0);
  - Microsoft.Extensions.Hosting / Logging.Console / Logging.Debug 10.0.13 → 10.0.14;
  - CameraX (Camera2 / Core / Lifecycle) 1.6.1 → 1.6.2 (held back so far: AndroidX Lifecycle 2.11 vs the 2.10 that
    `Avalonia.Android` 12.1.1 pins — NU1608);
  - SkiaSharp / SkiaSharp.NativeAssets.Linux 3.119.4 → 4.154.0 and ZXing.Net.Bindings.SkiaSharp 0.16.22 →
    0.16.25 (0.16.24+ needs SkiaSharp ≥ 4.151.1);
  - xunit.v3 3.2.2 → 4.0.2 (MTP v2 only; coverlet.collector is replaced by
    `Microsoft.Testing.Extensions.CodeCoverage`, as in Enigma.GitClient: `xunit.v3` 4.0.2 +
    `Microsoft.Testing.Extensions.CodeCoverage` 18.12.0).
- Avalonia.Skia 12.1.4 depends on SkiaSharp 3.119.4 and HarfBuzzSharp 8.3.1.3: the newest Avalonia is still on the
  SkiaSharp 3 line.
- Zero warnings (`TreatWarningsAsErrors`); the suite is 796 tests at 1.2.0.

## PHASE01 — Runtime packages to latest

**Status:** DONE
**Branch:** `feature/feature-e291-phase01-runtime-packages`

### Steps

1. `Directory.Packages.props`: the Avalonia set 12.1.4 (incl. `Avalonia.Android`), Enigma.Avalonia.Desktop 1.3.0,
   Microsoft.Extensions.* 10.0.14, QuestPDF 2026.9.1, CameraX 1.6.2 — each comment brought up to date.
2. QuestPDF 2026.9: replace `CheckIfAllTextGlyphsAreAvailable` with `ThrowOnMissingTextGlyphs` (same meaning);
   the PDF composer tests stay green.
3. CameraX 1.6.2: kept only if restore reports no NU1608/NU1605 against `Avalonia.Android` 12.1.4 and the Android
   head builds; otherwise reverted and its comment updated with the new reason.
4. SkiaSharp 4.x and ZXing.Net.Bindings.SkiaSharp 0.16.25: held back, the comment stating that Avalonia 12.1.4
   (the newest) is built on SkiaSharp 3.119.4, so a SkiaSharp 4 runtime under Avalonia.Skia is unsupported.
5. `dotnet list package --vulnerable --include-transitive` — recorded.

### Acceptance criteria

- `dotnet list package --outdated` shows only the held-back packages, each with its reason in the props.
- Restore without NU16xx; solution builds with zero warnings; whole suite green.

## PHASE02 — xunit.v3 4 and MTP code coverage

**Status:** DONE
**Branch:** `feature/feature-e291-phase02-xunit-v4`

### Steps

1. `Directory.Packages.props`: `xunit.v3` 4.0.2; `coverlet.collector` removed,
   `Microsoft.Testing.Extensions.CodeCoverage` 18.12.0 added; the tests comment rewritten (GitClient's wording).
2. The three test csproj: `coverlet.collector` → `Microsoft.Testing.Extensions.CodeCoverage`.
3. Fix whatever xunit.v3 4.x breaks (analyzer rules, API moves), without weakening any assertion.
4. `global.json` keeps `"runner": "Microsoft.Testing.Platform"`.

### Acceptance criteria

- `dotnet test --solution Enigma.HardCopy.slnx` runs every suite on xunit.v3 4.0.2; the same test count, all
  green; zero warnings.
- `dotnet list package --outdated` lists only PHASE01's held-back packages.

## Out of scope

- Moving the Recover app to Enigma.Avalonia.Mobile (a new package, not an update; Recover uses the Desktop
  library's ContentDialog and InfoBar, which Mobile does not have).
- SkiaSharp 4 (needs an Avalonia release built on it).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How far "latest" goes | Every package to latest, coupled sets moved as a whole; SkiaSharp 4 + ZXing binding 0.16.25 held back | The spec asks for latest; Avalonia 12.1.4 — the latest — runs on SkiaSharp 3.119.4, and two Skia runtimes in one process is unsupported | Forcing SkiaSharp 4 under Avalonia.Skia (runtime `MissingMethodException` risk invisible to the suite); holding the whole Avalonia set |
| QuestPDF rename | Do the code change now | The bump was only held back for it; the rename is one property with the same meaning | Holding QuestPDF again |
| xunit.v3 4.x | Migrate, with MTP CodeCoverage replacing coverlet | Explicitly "latest"; Enigma.GitClient, TextEditor and Launchpad already run this exact pair | Staying on 3.2.2 (house release skill's default hold, overridden by the spec) |
| Phasing | Runtime packages / test stack | Two independent, reviewable diffs; a test-stack failure never blocks the runtime bump | One dev |
| CameraX | Try 1.6.2; revert on NU1608 | Whether `Avalonia.Android` 12.1.4 moved its AndroidX set is decided by restore, not by guesswork | Holding it unconditionally |
| Order in the run | First item | Later devs are built and tested against the final dependencies | Last, before the release |
