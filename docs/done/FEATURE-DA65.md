# FEATURE-DA65 — Release 1.5.0

**Item:** FEATURE-DA65 — Release 1.5.0
**Branch:** `feature/feature-da65-release-1-5-0`
**Run:** feature/2026-11-04-icons-release

## Summary

The repository is ready to release **1.5.0**: 1.4.0 with the minor incremented, and the patch already 0, per the
spec's rule. The release carries this run's one item, the icon set that fills its frame (FEATURE-3642).

The edits:

- `Directory.Build.props`: `<Version>` set to 1.5.0.
- `app.manifest`: `assemblyIdentity` set to 1.5.0.0.
- `RELEASENOTES.md`: a top `## 1.5.0` section in 1.4.0's shape.
- `README.md`: the *What's new in 1.5.0* callout (one sentence still sums up 1.4.0), and the archive, MSI and APK
  names at 1.5.0.
- `WindowsInstallerTests`: two new facts, described below.

**The buildmsi script needed no edit.** The spec asked to "update the new version number in the buildmsi script".
`buildmsi.ps1` has no version number of its own: it reads `<Version>` from `Directory.Build.props` (FEATURE-4ED9)
and names the MSI and its profile after it. With the props at 1.5.0, it builds `Enigma.HardCopy.1.5.0.msi`.
Nothing tested that yet, so two facts now pin it:

- the script takes its version from the props' `/Project/PropertyGroup/Version` and names its output after it;
- the script contains no `X.Y.Z` literal.

Writing a number into the script now fails the suite instead of going stale at 1.6.0.

The Android `versionCode` follows on its own and is now 10500. **No package was updated.** The outdated packages are
the same three coupled sets as at 1.4.0, each still blocked for the same reason.

## Files/modules touched

- Modified:
  - `Directory.Build.props`, `src/Enigma.HardCopy.Desktop/app.manifest`
  - `RELEASENOTES.md`, `README.md`
  - `tests/Enigma.HardCopy.Desktop.UnitTests/WindowsInstallerTests.cs`
  - `docs/roadmap.md`, `docs/plan/FEATURE-DA65.md`
- Created: `docs/done/FEATURE-DA65.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How to pin the version source | Two facts: the four lines that read the version and name the output, and no `\d+\.\d+\.\d+` anywhere in the script | The first alone would pass if a hard-coded `$version = '…'` were added after the read. Today the script has no three-part number at all, so the second check is not brittle |
| The callout | 1.5.0, plus one sentence on 1.4.0 | Matches 1.4.0's callout, so a reader upgrading from 1.3 still learns about the MSI |
| *Known limitations* | Carried over (32-bit MSI, unsigned) | The worker and the script are unchanged, so both still hold |
| The README's *Building the Windows installer* | Unchanged | It already says "The version is the one in `Directory.Build.props`" |

## Deviations & follow-ups

- The house release skill's pack, pack-verify and MSI-profile steps do not apply: this is an app, no project is
  packable, and `buildmsi.ps1` generates its own profile.
- `SECURITY.md` and `docs/RELEASE.md` were not created, as at 1.3.0 and 1.4.0. The runbook is printed below.
- Follow-ups, unchanged from FEATURE-C1D1:
  - bump SkiaSharp 4 and the ZXing binding with the first Avalonia release built on SkiaSharp 4;
  - bump CameraX with the first `Avalonia.Android` on AndroidX Lifecycle 2.11;
  - bump Avalonia 12.1.5 with the next `Enigma.Avalonia.Desktop`.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-3642.md` (0 warnings; 884 passed).
- `git tag --list 1.5.0` was empty before the dev.
- `dotnet list Enigma.HardCopy.slnx package --outdated` showed updates only in the held sets:
  - Avalonia: 12.1.4 → 12.1.5;
  - SkiaSharp / `.NativeAssets.Linux`: 3.119.4 → 4.154.0;
  - `ZXing.Net.Bindings.SkiaSharp`: 0.16.22 → 0.16.25;
  - CameraX: 1.6.1 → 1.6.2.

  `Enigma.Avalonia.Desktop` 1.3.0 is still the newest, and its nuspec still depends on Avalonia 12.1.4.
- `dotnet list … package --vulnerable --include-transitive`: no project has a vulnerable package.
- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**, Android head included.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **886 passed** (884 + 2 new), 0 failed.
- The version as `buildmsi.ps1` reads it: the script's own `XmlDocument` load and XPath, run in PowerShell without
  the script (which would `git clean -dxf`). Result: `1.5.0`, which matches `^\d+\.\d+\.\d+$`, so the MSI is
  `Enigma.HardCopy.1.5.0.msi`.
- Negative check: with `$version = '1.5.0'` written into `buildmsi.ps1`, both new facts failed (2 of 9 installer
  tests). The line was restored, `git diff --quiet` confirms the script is unchanged, and the 9 pass again.
- `dotnet msbuild … Recover.Android.csproj -getProperty:ApplicationVersion -getProperty:ApplicationDisplayVersion`
  gave `10500` and `1.5.0`.
- `Enigma.HardCopy.Desktop.dll` and `.exe` (Release) report FileVersion `1.5.0.0` and ProductVersion
  `1.5.0+8baa0c3…`, so the About dialog shows *Version 1.5.0*.
- `git grep 1.4.0` outside the history files finds only the callout's sentence about 1.4.0.
- Every path the 1.5.0 notes quote exists.
- Fix budget: 0 cycles used.

## Documentation sweep

- `README.md` and `RELEASENOTES.md` are this release's own edits. `docs/android.md` names no version (`<version>`).
  Nothing else is stale.

## Runbook — printed for the maintainer, never run by the workflow

```bash
# 1. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-11-04-icons-release
git switch main && git pull && git merge --no-ff develop

# 2. Pre-flight, Release configuration
dotnet build Enigma.HardCopy.slnx -c Release
dotnet test --solution Enigma.HardCopy.slnx -c Release

# 3. Release artifacts
scripts/publish.sh                      # or scripts/publish.ps1
#   -> artifacts/Enigma.HardCopy-1.5.0-win-x64.zip, artifacts/Enigma.HardCopy-1.5.0-linux-x64.zip
export HARDCOPY_ANDROID_KEYSTORE=<path> HARDCOPY_ANDROID_KEYSTORE_PASSWORD=<password>
scripts/publish-android.sh              # or scripts/publish-android.ps1
#   -> artifacts/Enigma.HardCopy.Recover-1.5.0.apk
apksigner verify --print-certs artifacts/Enigma.HardCopy.Recover-1.5.0.apk
# Windows, LAST: it runs git clean -dxf, which deletes artifacts\ -- move the zips and the APK out first
packaging\windows\buildmsi.bat
#   -> artifacts\Enigma.HardCopy.1.5.0.msi

# 4. Release gates
#  - Windows: install the MSI over 1.4.0; it upgrades in place (one entry in Installed apps, at 1.5.0);
#    the desktop and Start-menu shortcuts, the taskbar and the title bar show the larger icon;
#    the About dialog shows 1.5.0 and the new icon; uninstall removes it.
#  - Linux desktop: packaging/linux/install.sh --from <unzipped linux-x64 archive>; the launcher shows the
#    larger icon; packaging/linux/uninstall.sh removes it.
#  - Phone: install the APK over 1.4.0; recover a printed backup.

# 5. Tag and push
git tag 1.5.0
git push origin main develop 1.5.0

# 6. GitHub release "1.5.0": notes from RELEASENOTES.md "## 1.5.0";
#    attach the two zips, the MSI and the APK, with the APK signing certificate's SHA-256 digest
```
