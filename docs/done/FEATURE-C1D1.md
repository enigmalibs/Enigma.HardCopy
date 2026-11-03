# FEATURE-C1D1 — Release 1.4.0

**Item:** FEATURE-C1D1 — Release 1.4.0
**Branch:** `feature/feature-c1d1-release-1-4-0`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The repository is ready to release **1.4.0**, which is 1.3.0 with the minor incremented (the patch was already 0,
per the spec's rule). The release carries this run's two items:

- the Windows installer (FEATURE-4ED9);
- the icon without its tile (FEATURE-BA6C).

The edits:

- `Directory.Build.props` `<Version>` → 1.4.0;
- `app.manifest` `assemblyIdentity` → 1.4.0.0;
- a top `## 1.4.0` section in `RELEASENOTES.md`, in 1.3.0's shape;
- README:
  - the *What's new in 1.4.0* callout, which still sums up 1.3.0 in one sentence;
  - the archive and APK names at 1.4.0;
  - a paragraph in *Install* on the MSI and its .NET 10 runtime requirement.

The Android `versionCode` follows on its own: 10400. `buildmsi.ps1` reads the same `<Version>`, so the next MSI is
`Enigma.HardCopy.1.4.0.msi`.

**No package was updated.** Every package with a newer version belongs to a set that moves together, and each set is
still blocked for the same reason as at 1.3.0, or a new one:

- Avalonia 12.1.5: `Enigma.Avalonia.Desktop` 1.3.0, still the newest, is built on 12.1.4. Enigma.GitClient's 6.14.0
  held it for the same reason today.
- SkiaSharp 4 and the ZXing binding.
- CameraX 1.6.2.

## Files/modules touched

- Modified:
  - `Directory.Build.props`, `src/Enigma.HardCopy.Desktop/app.manifest`
  - `RELEASENOTES.md`, `README.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-C1D1.md`
- Created: `docs/done/FEATURE-C1D1.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.5 | Held at 12.1.4, and logged under *Dependencies* | The set moves only with `Enigma.Avalonia.Desktop`, whose 1.3.0 nuspec depends on Avalonia 12.1.4 |
| CameraX 1.6.2 | Held, not retried | The NU1608 that FEATURE-E291 hit comes from `Avalonia.Android` 12.1.4's Lifecycle 2.10 pin. That package is held too, so nothing has changed |
| The callout | 1.4.0, with one sentence on 1.3.0 | Matches 1.3.0's callout. A reader upgrading from 1.2 still learns what 1.3 brought |
| The MSI in *Install* | A paragraph under the table, not a table row | The table lists archives and what to run in them. The MSI is a different kind of artifact and has a different requirement (the runtime) |

## Deviations & follow-ups

- The house release skill's pack, pack-verify and MSI-profile steps do not apply. This is an app, no project is
  packable, and FEATURE-4ED9's script generates its profile on every build.
- `SECURITY.md` and `docs/RELEASE.md` were not created, as for 1.3.0. The runbook is printed below.
- Follow-up, unchanged from FEATURE-E291: bump SkiaSharp 4 and the ZXing binding with the first Avalonia release
  built on SkiaSharp 4. Bump CameraX with the first `Avalonia.Android` on AndroidX Lifecycle 2.11. Bump Avalonia
  12.1.5 with the next `Enigma.Avalonia.Desktop`.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BA6C.md` (0 warnings; 875 passed).
- `git tag --list 1.4.0`: empty before the dev.
- `dotnet list Enigma.HardCopy.slnx package --outdated` showed updates only in the held sets:
  - Avalonia: 12.1.4 → 12.1.5;
  - SkiaSharp / `.NativeAssets.Linux`: 3.119.4 → 4.154.0;
  - `ZXing.Net.Bindings.SkiaSharp`: 0.16.22 → 0.16.25;
  - CameraX: 1.6.1 → 1.6.2.
- `dotnet list … package --vulnerable --include-transitive`: no project has a vulnerable package.
- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**, Android head included.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **884 passed**, 0 failed.
- `dotnet msbuild … Recover.Android.csproj -getProperty:ApplicationVersion -getProperty:ApplicationDisplayVersion`
  gave `10400` and `1.4.0`.
- `Enigma.HardCopy.Desktop.dll` (Release) reports FileVersion `1.4.0.0` and ProductVersion
  `1.4.0+075515fd4f67…`, and the `.exe` reports FileVersion `1.4.0.0`. The About dialog therefore shows *Version
  1.4.0*.
- `git grep 1.3.0`, outside the history files: no stale current-version mention. What remains is:
  - the control library's own version, `Enigma.Avalonia.Desktop` 1.3.0;
  - sample values in `AboutViewModelTests`;
  - the callout's sentence about 1.3.0.
- Every path the 1.4.0 notes quote exists.
- Fix budget: 0 cycles used.

## Documentation sweep

- `README.md` and `RELEASENOTES.md` are this release's own edits. `docs/android.md` names no version. Nothing else is
  stale.

## Runbook — printed for the maintainer, never run by the workflow

```bash
# 1. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-11-03-icons-msi-release
git switch main && git pull && git merge --no-ff develop

# 2. Pre-flight, Release configuration
dotnet build Enigma.HardCopy.slnx -c Release
dotnet test --solution Enigma.HardCopy.slnx -c Release

# 3. Release artifacts
scripts/publish.sh                      # or scripts/publish.ps1
#   -> artifacts/Enigma.HardCopy-1.4.0-win-x64.zip, artifacts/Enigma.HardCopy-1.4.0-linux-x64.zip
export HARDCOPY_ANDROID_KEYSTORE=<path> HARDCOPY_ANDROID_KEYSTORE_PASSWORD=<password>
scripts/publish-android.sh              # or scripts/publish-android.ps1
#   -> artifacts/Enigma.HardCopy.Recover-1.4.0.apk
apksigner verify --print-certs artifacts/Enigma.HardCopy.Recover-1.4.0.apk
# Windows, LAST: it runs git clean -dxf, which deletes artifacts\ -- move the zips and the APK out first
packaging\windows\buildmsi.bat
#   -> artifacts\Enigma.HardCopy.1.4.0.msi

# 4. Release gates
#  - Windows: install the MSI; "Enigma HardCopy" is on the desktop and in the Start menu with the new icon;
#    the About dialog shows 1.4.0; it appears in Installed apps; uninstall removes it.
#  - Linux desktop: packaging/linux/install.sh --from <unzipped linux-x64 archive>; the launcher shows the
#    tile-free icon; packaging/linux/uninstall.sh removes it.
#  - Phone: install the APK over 1.3.0; recover a printed backup.

# 5. Tag and push
git tag 1.4.0
git push origin main develop 1.4.0

# 6. GitHub release "1.4.0": notes from RELEASENOTES.md "## 1.4.0";
#    attach the two zips, the MSI and the APK, with the APK signing certificate's SHA-256 digest
```
