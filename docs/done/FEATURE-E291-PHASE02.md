# FEATURE-E291-PHASE02 — xunit.v3 4 and MTP code coverage

## Summary

The three test projects moved from xunit.v3 3.2.2 to **4.0.2**, the latest release. xunit.v3 4.x runs on
Microsoft.Testing.Platform v2 only, so coverage moved too: `coverlet.collector` 10.1.0 was removed and
**`Microsoft.Testing.Extensions.CodeCoverage` 18.12.0** replaces it. This is the same pair Enigma.GitClient,
Enigma.TextEditor and Enigma.Launchpad run. No test needed a change: the 796 tests compile with no new analyzer
warning and pass.

| Package | Old → new |
|---|---|
| xunit.v3 | 3.2.2 → 4.0.2 (Microsoft.Testing.Platform 2.5.1 underneath) |
| coverlet.collector | 10.1.0 → removed |
| Microsoft.Testing.Extensions.CodeCoverage | new, 18.12.0 |

## Files/modules touched

- Modified: `Directory.Packages.props`
- Modified: `tests/Enigma.HardCopy.Core.UnitTests/Enigma.HardCopy.Core.UnitTests.csproj`
- Modified: `tests/Enigma.HardCopy.Desktop.UnitTests/Enigma.HardCopy.Desktop.UnitTests.csproj`
- Modified: `tests/Enigma.HardCopy.Recover.UnitTests/Enigma.HardCopy.Recover.UnitTests.csproj`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-E291.md`

## Deviations & follow-ups

- Step 3 (fix what 4.x breaks) needed no work: nothing broke.
- `global.json` keeps `"runner": "Microsoft.Testing.Platform"`, unchanged.
- Coverage is now collected with `dotnet test … --coverage`, which writes a `.coverage` file under `TestResults/`
  (ignored by git). Coverlet's `--collect "XPlat Code Coverage"` no longer applies. No doc mentioned it.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Version of the coverage extension | 18.12.0, as GitClient | The newest release, and the one the other house apps pin |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **796 passed**, 0 failed. That is the same count
  as before.
- `project.assets.json` resolves `xunit.v3` and its `.core`/`.assert`/`.runner.*` packages at 4.0.2, and
  Microsoft.Testing.Platform at 2.5.1.
- `dotnet test --project tests/Enigma.HardCopy.Recover.UnitTests --no-build --coverage` gave 99 passed and wrote a
  `.coverage` file.
- `dotnet list package --outdated` now lists only PHASE01's held-back packages: SkiaSharp,
  SkiaSharp.NativeAssets.Linux, ZXing.Net.Bindings.SkiaSharp and the three CameraX packages.
- Doc sweep: README names "the xUnit suites" with no version, and nothing documents coverlet. No edit.
