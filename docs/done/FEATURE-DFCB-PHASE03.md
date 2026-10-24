# FEATURE-DFCB-PHASE03 — Release 1.2.0: version, notes, runbook — DONE

**Branch:** `feature/feature-dfcb-phase03-release-1-2-0`
**Date:** 2026-10-24
**Run:** feature/2026-10-24-android-app-release

## Summary

The repository is release-ready at **1.2.0**, the first release that ships the Android app. Nothing is tagged,
pushed, published or signed with a real key — the runbook below is for the maintainer.

- **A minor.** A new app and a new feature, nothing broken. The paper format is untouched, the desktop app's
  source is untouched since 1.1.0, and Core gains one additive public method (`ImageDecoder.ReadCodes` over a
  luminance frame).
- **`Directory.Build.props`** `<Version>` 1.1.0 → **1.2.0**, and **`app.manifest`** `assemblyIdentity` →
  `1.2.0.0`. Through PHASE01's scheme the APK now reports `versionName='1.2.0'` and `versionCode='10200'`.
- **`RELEASENOTES.md`** gains a `## 1.2.0` section on top, in the house style of the 1.1.0 section:
  - what HardCopy Recover does and never stores, and *Check a printed backup*;
  - installing it, with the signing certificate and the uninstall-first rule;
  - compatibility (the paper is unchanged, the desktop source is untouched, Core's one addition);
  - for developers;
  - dependencies — new, `old → new`, held back with reasons, and no known vulnerability;
  - the APK's measured size and the license audit;
  - known limitations.
- **`README.md`:**
  - the what's-new callout for 1.2.0;
  - the Install table's zips at 1.2.0, and a paragraph for the APK;
  - *Recovering on Android* now points at the release APK as well as the source build.
- **`docs/android.md`:**
  - a troubleshooting row quoting the exact error a phone with a development build gives;
  - the 1.2.0 checks under *What was verified*.

## Files/modules touched

**Modified**

- `Directory.Build.props` — version.
- `src/Enigma.HardCopy.Desktop/app.manifest` — `assemblyIdentity` version.
- `RELEASENOTES.md` — the 1.2.0 section, with a `---` separator above 1.1.0 as between earlier releases.
- `README.md` — callout, Install, *Recovering on Android* intro.
- `docs/android.md` — troubleshooting row, *What was verified*.
- `docs/roadmap.md`, `docs/plan/FEATURE-DFCB.md` — PHASE03 and the item to `DONE`.

**Created** — `docs/done/FEATURE-DFCB-PHASE03.md` (this file).

**Untouched** — every source file except the manifest line; the scripts; `Directory.Packages.props`.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Release-notes shape | The 1.1.0 section's own style: a lead paragraph, then themed `###` sections, ending with *Dependencies*, size/licences and *Known limitations* | The house skill says to match the repository's style where it differs from the default sub-section list | The skill's default *New Features · Fixes · …* headings |
| Real-phone decoding in the notes | Not claimed, not disclaimed; made **step 4 of the runbook**, before the tag | The notes describe the release as it will be published, after the maintainer's check; the run cannot hold a phone to paper | Writing "not verified on a real phone" into shipped notes |
| The signing certificate's digest | The notes say the release page gives it; the runbook has the maintainer record it from `apksigner` | The real key does not exist yet, and its digest is the maintainer's to publish | A placeholder digest in the notes |
| License audit depth | Read the APK's resolved runtime graph (`project.assets.json`, 135 packages) and each nuspec's license expression; QuestPDF's file license read | The dotnet-release skill audits what ships. This is the first time the APK ships, and it carries far more than the desktop app (AndroidX, Kotlin) | Listing only the direct references |
| README *Recovering on Android* intro | Corrected, since it said the app is "built from source and installed as an APK" | Made factually wrong by a released APK — the documentation-sweep rule | Leaving it |
| MSI profile | Not generated, as planned | An APK cannot have one; the desktop app ships zips | — |

## Deviations & follow-ups

- **Release gate the run could not pass: decoding printed codes with a real phone camera.** It is still
  unobserved since FEATURE-E931. It is step 4 of the runbook, before the tag.
- **The APK's size is 42,231,208 bytes at 1.1.0 and at 1.2.0,** before and after PHASE02's refresh. Page
  alignment of the packed assemblies is the likely reason (see PHASE02); the version change itself is
  confirmed by `aapt2`.
- **Build warning `XA0033` (environmental).** One per inner Android build, so three in Release (outer,
  `android-arm64`, `android-x64`) and one in Debug. Zero compiler or analyzer warnings.
- **Follow-ups for after the release:**
  - the Avalonia-set move (with `Enigma.Avalonia.Desktop` 1.2.0, SkiaSharp 4, the ZXing binding and CameraX
    1.6.2);
  - QuestPDF 2026.9 with its renamed setting and a printed-PDF check;
  - xunit.v3 4.x;
  - dropping QuestPDF's managed assembly from the APK by splitting Core.
- Line endings: LF throughout; nothing to recommend.
- Documentation sweep: `README.md`, `RELEASENOTES.md` and `docs/android.md` are updated as above. No other prose
  doc names the version or the install path.

## Build/test evidence

- **Release configuration:** `dotnet build Enigma.HardCopy.slnx -c Release` — 0 errors. The 3 warnings are all
  `XA0033`, checked with `-clp:WarningsOnly`. `dotnet test --solution Enigma.HardCopy.slnx -c Release` —
  **796 passed, 0 failed**.
- **Debug configuration, after the doc edits:** build 0 errors (1 × `XA0033`); suite **796 passed, 0 failed**.
- `dotnet msbuild … -getProperty:ApplicationVersion -getProperty:ApplicationDisplayVersion` → `10200`, `1.2.0`.
- **Release APK** built by `scripts/publish-android.sh` with a throwaway key in the session scratchpad:
  - `Enigma.HardCopy.Recover-1.2.0.apk`, 42,231,208 bytes;
  - `aapt2 dump badging` → `versionCode='10200' versionName='1.2.0'`, target SDK 36, label *HardCopy Recover*,
    permissions `CAMERA` (+ AndroidX's internal receiver permission), native code `arm64-v8a` `x86_64`;
  - `apksigner verify -v --print-certs` → verified with v2 and v3 schemes, the throwaway certificate.
- **Emulator** (`Pixel_6_API_36`):
  - a debug-signed development build installed with `-t:Install` (`versionCode=10200`);
  - the release APK over it → `INSTALL_FAILED_UPDATE_INCOMPATIBLE: Existing package
    io.github.enigmalibs.hardcopy.recover signatures do not match newer version; ignoring!`;
  - after `adb uninstall`, the release APK → `Success`, `versionCode=10200 versionName=1.2.0`. It starts on its
    start card (*Start scanning*, *Check a printed backup*) with an empty crash log.
- **No stale version:** `README.md`, `Directory.Build.props`, `app.manifest`, the scripts and the Android csproj
  hold no `1.1.0`.
- The scratch `artifacts/` directory and the throwaway key were deleted.

## Runbook — printed for the maintainer, never run by the workflow

```bash
# 1. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-10-24-android-app-release
git switch main && git pull && git merge --no-ff develop

# 2. Pre-flight, Release configuration
dotnet build Enigma.HardCopy.slnx -c Release
dotnet test  --solution Enigma.HardCopy.slnx -c Release

# 3. The release key — once, ever. Keep it outside the repository; back it up with its password.
keytool -genkeypair -v -keystore ~/keys/hardcopy-recover.keystore -storetype PKCS12 \
  -alias hardcopy-recover -keyalg RSA -keysize 4096 -validity 10000 \
  -dname "CN=HardCopy Recover, O=EnigmaLibs"

# 4. Artifacts
scripts/publish.sh                    # Enigma.HardCopy-1.2.0-win-x64.zip, -linux-x64.zip (publish.ps1 on Windows)
export HARDCOPY_ANDROID_KEYSTORE=~/keys/hardcopy-recover.keystore
read -rs HARDCOPY_ANDROID_KEYSTORE_PASSWORD && export HARDCOPY_ANDROID_KEYSTORE_PASSWORD
scripts/publish-android.sh            # Enigma.HardCopy.Recover-1.2.0.apk
apksigner verify --print-certs artifacts/Enigma.HardCopy.Recover-1.2.0.apk   # record the SHA-256 digest
aapt2 dump badging artifacts/Enigma.HardCopy.Recover-1.2.0.apk | head -1     # versionCode='10200' versionName='1.2.0'

# 5. RELEASE GATE — on a physical phone, with that APK:
#    recover a printed backup end to end, and run "Check a printed backup" on a printout.
#    Do not tag until both work.

# 6. Tag (bare, as 1.0.0 and 1.1.0) and push
git tag 1.2.0
git push origin main 1.2.0

# 7. GitHub release "1.2.0": the RELEASENOTES.md 1.2.0 section as notes; attach the two zips and the APK;
#    state the APK signing certificate's SHA-256 digest from step 4.
```

Post-publish: the release page lists three artifacts. The downloaded APK's `apksigner` digest matches the one
stated, it installs on a clean phone, and Android's app info (*Settings → Apps → HardCopy Recover*) shows version
1.2.0.
