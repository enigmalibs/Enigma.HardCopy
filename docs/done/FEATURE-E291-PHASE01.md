# FEATURE-E291-PHASE01 — Runtime packages to latest

## Summary

Every runtime package moved to its latest release where the coupled sets allow it. The Avalonia set and the
house control library moved together; QuestPDF moved with the one-property rename it needed; Microsoft.Extensions
took its patch. CameraX and SkiaSharp 4 (with the ZXing SkiaSharp binding) were tried or checked and stay held
back, each with its reason written beside its pin.

| Package | Old → new |
|---|---|
| Avalonia, Avalonia.Android, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter | 12.1.1 → 12.1.4 |
| Enigma.Avalonia.Desktop | 1.0.0 → 1.3.0 |
| QuestPDF | 2026.7.2 → 2026.9.1 |
| Microsoft.Extensions.Hosting, .Logging.Console, .Logging.Debug | 10.0.13 → 10.0.14 |

Held back:

- **Xamarin.AndroidX.Camera.\*** at 1.6.1 (latest 1.6.2). Restore with 1.6.2 fails with NU1608: 1.6.2 brings
  AndroidX Lifecycle 2.11.0.1, while `Avalonia.Android` 12.1.4 still pins its Lifecycle packages to `[2.10.0.2,
  2.10.1)`.
- **SkiaSharp / SkiaSharp.NativeAssets.Linux** at 3.119.4 (latest 4.154.0). `Avalonia.Skia` 12.1.4, from the
  newest Avalonia, depends on SkiaSharp 3.119.4, and the app needs one Skia runtime.
- **ZXing.Net.Bindings.SkiaSharp** at 0.16.22 (latest 0.16.25). From 0.16.24 on it needs SkiaSharp ≥ 4.151.1.
- `xunit.v3` 3.2.2 → 4.0.2 is PHASE02's.
- Already latest: Enigma.Icons, Enigma.Icons.Phosphor, Enigma.Icons.Avalonia 1.0.0; CommunityToolkit.Mvvm 8.4.2;
  AvaloniaUI.DiagnosticsSupport 2.2.3; QRCoder 1.8.1; ZXing.Net 0.16.11.

## Files/modules touched

- Modified: `Directory.Packages.props`
- Modified: `src/Enigma.HardCopy.Core/PdfComposer.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-E291.md`

## Deviations & follow-ups

- CameraX was planned as "try, revert on NU1608". Restore reported NU1608, so it was reverted.
- Follow-up: bump SkiaSharp 4 and the ZXing binding with the first Avalonia release built on SkiaSharp 4. Bump
  CameraX with the first `Avalonia.Android` on AndroidX Lifecycle 2.11.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How to check the QuestPDF bump beyond the suite | Rendered one backup PDF (gzip, one data code, a CJK file name) through a file-based `dotnet run` script in the scratchpad, and looked at it | The props comment asks for a look at a printed PDF after a QuestPDF bump. The layout is unchanged, and the missing CJK glyphs fall back to boxes instead of throwing, which is what `ThrowOnMissingTextGlyphs = false` is for |

## Build/test evidence

- Baseline before any change: `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.HardCopy.slnx` gave **796 passed**, 0 failed.
- With CameraX 1.6.2, `dotnet restore` failed with 4 × NU1608 (Lifecycle 2.10 vs 2.11). After the revert, restore
  was clean.
- After the change: `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors. The Android head was
  included. `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **796 passed**, 0 failed.
- `dotnet list package --outdated` lists only the held-back packages above and `xunit.v3`.
- `dotnet list package --vulnerable --include-transitive` found no vulnerable package in any project.
- Doc sweep: README and `docs/android.md` name no package version. No edit.
