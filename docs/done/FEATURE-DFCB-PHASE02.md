# FEATURE-DFCB-PHASE02 — Dependency refresh — DONE

**Branch:** `feature/feature-dfcb-phase02-dependency-refresh`
**Date:** 2026-10-24
**Run:** feature/2026-10-24-android-app-release

## Summary

The release's dependency refresh, per the house release routine: every bump outside a version-coupled set is
taken when it restores, builds and tests clean, and every coupled set is held back. Two of the five planned bumps
turned out not to be free-standing. They were reverted and the reason is written beside each pin.

### Taken (`old → new`)

| Package | Old | New | Reaches |
|---|---|---|---|
| `QRCoder` | 1.8.0 | **1.8.1** | Core (QR rendering in the PDF) — desktop and, unused, the APK |
| `Microsoft.Extensions.Hosting` | 10.0.10 | **10.0.13** | desktop app, Android app |
| `Microsoft.Extensions.Logging.Console` | 10.0.10 | **10.0.13** | desktop app |
| `Microsoft.Extensions.Logging.Debug` | 10.0.10 | **10.0.13** | desktop app, Android app |
| `coverlet.collector` | 10.0.1 | **10.1.0** | test projects only |

### Held back, and why

| Package(s) | Pinned | Available | Reason |
|---|---|---|---|
| `QuestPDF` | 2026.7.2 | 2026.9.1 | 2026.9 renames `Settings.CheckIfAllTextGlyphsAreAvailable` to `ThrowOnMissingTextGlyphs`, so the bump needs a code change (CS0618 under `TreatWarningsAsErrors`). QuestPDF lays out the paper backups; its next bump should come with that change and a fresh look at the printed PDF, not ride in a release about the Android app |
| `ZXing.Net.Bindings.SkiaSharp` | 0.16.22 | 0.16.24 | 0.16.24 requires SkiaSharp ≥ 4.151.1 (NU1605 against the 3.119.4 pin), so it has joined the SkiaSharp / Avalonia set |
| Avalonia set: `Avalonia`, `Avalonia.Android`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter` | 12.1.1 | 12.1.4 | Version-coupled; moves as a unit, with `Enigma.Avalonia.Desktop` (1.0.0 → 1.2.0 available), the icon packs and SkiaSharp (3.119.4 → 4.153.1 available) |
| `Xamarin.AndroidX.Camera.*` | 1.6.1 | 1.6.2 | Moves AndroidX Lifecycle past the 2.10 line `Avalonia.Android` 12.1.1 pins (NU1608); moves with the Avalonia set |
| `xunit.v3` | 3.2.2 | 4.0.2 | A test-framework migration, not a bump: MTP v2 only, analyzer changes, and coverage moving to `Microsoft.Testing.Extensions.CodeCoverage`. Its own work item |

`dotnet list package --vulnerable --include-transitive`: **no vulnerable package** in any of the seven projects.

## Files/modules touched

**Modified**

- `Directory.Packages.props` — the five bumps.
  - New comments beside the QuestPDF and ZXing binding pins and on the test group (xunit.v3).
  - The SkiaSharp comment no longer names a specific newer release, which had gone stale ("4.151.0" → "4.x").
- `docs/roadmap.md`, `docs/plan/FEATURE-DFCB.md` — statuses.

**Created** — `docs/done/FEATURE-DFCB-PHASE02.md` (this file).

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| QuestPDF 2026.9.1 fails the build on an obsolete setting | Held back at 2026.7.2, reason in the props | The plan: a bump that needs code is not absorbed by the release. Here it is the PDF engine of the paper backups, whose output deserves its own look | Renaming the setting in `PdfComposer` (one line, but it changes the paper's engine inside an Android release) |
| ZXing binding 0.16.24 fails restore (NU1605) | Held back at 0.16.22, reason in the props | It now depends on SkiaSharp 4, which is coupled to Avalonia | Lifting the SkiaSharp pin (breaks the one-Skia-runtime rule) |
| Coverage package | `coverlet.collector` kept and bumped | The house would use `Microsoft.Testing.Extensions.CodeCoverage`, but switching is part of the xunit.v3 4.x migration, not a refresh | Swapping it now |

## Deviations & follow-ups

- **Two of the five planned bumps were not taken** (QuestPDF, the ZXing binding). The plan anticipated this
  ("each kept only if restore, build and the suite stay clean; any that does not is reverted and recorded").
- **Follow-up work items worth planning:**
  - the Avalonia-set move (12.1.4 + `Enigma.Avalonia.Desktop` 1.2.0 + SkiaSharp 4 + the ZXing binding + CameraX
    1.6.2);
  - QuestPDF 2026.9 with the renamed setting and a printed-PDF check;
  - xunit.v3 4.x with the coverage switch.
- **The APK's size did not move: 42,231,208 bytes, before and after.** `project.assets.json` confirms the
  Android head resolves Hosting / Logging.Debug 10.0.13 and QRCoder 1.8.1. The likely reason is that .NET for
  Android packs assemblies into 16 KB-page-aligned `.so` files, which absorbs patch-level size changes. This
  explanation was not proven byte by byte.
- **Build warning `XA0033` (environmental), as before.**
- Line endings: LF throughout; nothing to recommend.
- Documentation sweep: no README or guide states package versions that moved. The release notes record every
  transition in PHASE03.

## Build/test evidence

- **First build, with all five planned bumps:**
  - restore failed — NU1605: `ZXing.Net.Bindings.SkiaSharp 0.16.24 → SkiaSharp (>= 4.151.1)` against the 3.119.4
    pin;
  - with the binding reverted, the build failed — CS0618 in `PdfComposer.cs(62,9)`
    (`CheckIfAllTextGlyphsAreAvailable` is obsolete in QuestPDF 2026.9).
- **Fix cycle 1** (QuestPDF held back):
  - `dotnet restore` clean, with no NU16xx;
  - `dotnet build Enigma.HardCopy.slnx` — 0 errors, the one warning being `XA0033`;
  - `dotnet test --solution Enigma.HardCopy.slnx` — **796 passed, 0 failed, 0 skipped**.
- **Android release build on the refreshed set:** `scripts/publish-android.sh` with the throwaway key produced a
  signed APK that `apksigner verify` accepts. The artifact was deleted afterwards.
- `dotnet list package --outdated` after the refresh lists only the held-back packages above.
