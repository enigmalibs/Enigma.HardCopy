# FEATURE-407E — Release 1.3.0

## Summary

The repository is ready to release **1.3.0**. That is 1.2.0 with the minor incremented and the patch at 0, per the
spec's rule. The release carries this run's work:

- the About dialog (FEATURE-7B95);
- the safety net for unexpected failures (FEATURE-555A);
- the new icon on the desktop and the phone (FEATURE-1148);
- `app.ico` beside the executable (FEATURE-C5C6);
- the Linux installer (FEATURE-3D14);
- the publish scripts (FEATURE-92E2);
- the dependency refresh (FEATURE-E291).

The edits:

- `Directory.Build.props` `<Version>` → 1.3.0;
- `app.manifest` `assemblyIdentity` → 1.3.0.0;
- a top `## 1.3.0` section in `RELEASENOTES.md`;
- README: the *What's new in 1.3.0* callout, and the archive and APK names at 1.3.0.

The Android `versionCode` follows on its own: 10300.

Two items of the spec needed no work, and they appear nowhere in the release notes because nothing changed:

- **No splash screen** exists in either app.
- **No `net8.0` target** exists in any project. Every project targets `net10.0` or `net10.0-android`, and Core's
  csproj comment already explains why it has a single `net10.0` target.

## Files/modules touched

- Modified: `Directory.Build.props`, `src/Enigma.HardCopy.Desktop/app.manifest`
- Modified: `RELEASENOTES.md`, `README.md`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-407E.md`

## Deviations & follow-ups

- **1.2.0 was never tagged.** The merge that released it is on `main` (`2048c40 Merge branch 'develop'`), but
  neither the local repository nor `origin` has a `1.2.0` tag; only `1.0.0` and `1.1.0` exist. The runbook below
  offers to tag `1.2.0` on that merge first, if 1.2.0 was published. Whether 1.2.0 shipped is the maintainer's call.
- The house release skill's pack and pack-verify steps do not apply. This is an app, and no project is packable.
- The skill offers an MSI profile for apps. Not generated, as for 1.2.0: the desktop app ships zips.
- `docs/RELEASE.md` was not created, as for 1.2.0. The runbook is printed here.
- TFM normalization: nothing to do. There are no plain-`net` targets outside the house set, and none in a packable
  library.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Version despite the missing 1.2.0 tag | 1.3.0, as the spec says | The rule is explicit, and the 1.2.0 release work is merged to `main`. Folding this run into an untagged 1.2.0 would rewrite notes that are already on `main` |
| Dependency refresh at release time | None beyond FEATURE-E291 | Done earlier in this run, on the same day. Its `old → new` transitions and its held-back sets are what the notes record |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx -c Release` gave 0 warnings and 0 errors. The Android head was included.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build` gave **875 passed**, 0 failed.
- `dotnet msbuild src/Enigma.HardCopy.Recover.Android/… -getProperty:ApplicationVersion
  -getProperty:ApplicationDisplayVersion` gave `10300` and `1.3.0`.
- `Enigma.HardCopy.Desktop.dll` (Release) reports FileVersion `1.3.0.0` and ProductVersion
  `1.3.0+3eb1589c09fdc33f9c87fa415b5bdc7a66ab839c`. The About dialog therefore shows *Version 1.3.0*, *Build
  3eb1589*.
- No stale `1.2.0` is left in `README.md`, `Directory.Build.props` or `app.manifest`.
- Doc sweep: README and RELEASENOTES are the release's own edits. Nothing else names the version.

## Runbook — printed for the maintainer, never run by the workflow

```bash
# 0. Optional, only if 1.2.0 was published: tag the merge that released it, which was never tagged
git tag 1.2.0 2048c40 && git push origin 1.2.0

# 1. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-10-29-uniform-enigma-apps
git switch main && git pull && git merge --no-ff develop

# 2. Pre-flight, Release configuration
dotnet build Enigma.HardCopy.slnx -c Release
dotnet test --solution Enigma.HardCopy.slnx -c Release

# 3. Release artifacts
scripts/publish.sh                      # or scripts/publish.ps1
#   -> artifacts/Enigma.HardCopy-1.3.0-win-x64.zip, artifacts/Enigma.HardCopy-1.3.0-linux-x64.zip
export HARDCOPY_ANDROID_KEYSTORE=<path> HARDCOPY_ANDROID_KEYSTORE_PASSWORD=<password>
scripts/publish-android.sh              # or scripts/publish-android.ps1
#   -> artifacts/Enigma.HardCopy.Recover-1.3.0.apk
apksigner verify --print-certs artifacts/Enigma.HardCopy.Recover-1.3.0.apk

# 4. Release gates
#  - Linux desktop: packaging/linux/install.sh --from <unzipped linux-x64 archive>;
#    the launcher shows "Enigma HardCopy" with the new icon and groups the running window;
#    packaging/linux/uninstall.sh removes it.
#  - Phone: install the APK over 1.2.0; the launcher shows the new icon; recover a printed backup.

# 5. Tag and push
git tag 1.3.0
git push origin main develop 1.3.0

# 6. GitHub release "1.3.0": notes from RELEASENOTES.md "## 1.3.0";
#    attach the two zips and the APK, with the signing certificate's SHA-256 digest
```
