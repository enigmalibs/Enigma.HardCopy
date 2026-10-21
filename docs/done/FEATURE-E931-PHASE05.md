# FEATURE-E931-PHASE05 — README & Android guide — DONE

**Branch:** `feature/feature-e931-phase05-docs`
**Date:** 2026-10-19
**Run:** feature/2026-10-19-android-recover-app

## Summary

The Android app is documented for both of its readers.

- **README.md**
  - The introduction names *HardCopy Recover*.
  - A new **Recovering on Android** section, placed after the desktop recovery section, covers:
    - the scanning flow: camera permission, any order, haptic ticks, each code read once;
    - the progress card;
    - completion and saving to the phone or Google Drive;
    - the refusal of codes from another backup, and what to do when the first code was the stray one;
    - the hash-mismatch guard;
    - exactly what is — not — kept on the phone, and why screenshots are blocked;
    - pause and new session.
  - *Build from source* points to the guide, and *Project layout* names it.
  - *Limitations* adds the camera-only input and the 64-bit / Android 7.0+ / not-in-a-store facts.
  - *License* credits AndroidX CameraX (Apache-2.0).
- **docs/android.md** — a new step-by-step guide:
  - prerequisites;
  - building;
  - running on an emulator, including the `ANDROID_SDK_ROOT` trap with Visual Studio's SDK;
  - installing on a phone over USB or as a standalone Release APK, and what its debug-key signature means;
  - troubleshooting: the Fast Deployment abort, black screenshots, a permanently refused permission, the slow
    first camera start on an emulator, codes that will not read, codes from another backup;
  - an explicit *What was verified* list.

## Files/modules touched

**Created**

- `docs/android.md`
- `docs/done/FEATURE-E931-PHASE05.md` — this file.

**Modified**

- `README.md` — the sections listed above.
- `docs/roadmap.md`, `docs/plan/FEATURE-E931.md` — PHASE05 and FEATURE-E931 to `DONE`.

## Deviations & follow-ups

- **Every command in the guide was run on the development machine, or is listed under *Not run here*** — the
  workload install (already present), the emulator with a window, USB deployment to a physical phone, and
  installing the APK from the phone itself.
- Facts were checked against the Release APK rather than assumed. `aapt dump` shows:
  - the only permission requested is `CAMERA`, so the README's "no network access" holds for the Release build;
  - minSdk 24 and targetSdk 36;
  - native code for `arm64-v8a` and `x86_64` only, which the README states as "64-bit phones".
- The Release APK (trimmed) was installed on the emulator and exercised: it starts, asks for the camera, scans.
- Follow-up: the `CAMERA` permission makes the build tools infer `android.hardware.camera` (a **back** camera) as
  required, next to the declared `camera.any`. It does not matter for sideloading, but a store listing would
  hide the app from front-camera-only devices. Declaring `android.hardware.camera` with `required="false"` fixes
  it, if a store release ever happens.
- Line endings: `.gitattributes` enforces `eol=lf`; nothing to recommend.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the usage text lives | A README section, next to the desktop recovery section | Users read the README; the two recovery paths are siblings | A separate user manual |
| Where the build/deploy text lives | `docs/android.md` | Long, tool-specific, and of no interest to desktop users | Inlined in the README's *Build from source* |
| Unverified steps | Kept, and listed explicitly as not run | USB deployment is the normal way onto a phone; omitting it would leave the guide incomplete | Dropping every step that could not be run here |

## Build/test evidence

- Documentation only — no source changed in this phase, so there was nothing new to build or test. The checks
  behind the documented facts were run during this phase:
  - `dotnet publish src/Enigma.HardCopy.Recover.Android -c Release -f net10.0-android -p:AndroidPackageFormat=apk`:
    0 warnings, a 42 MB APK.
  - `aapt dump permissions` / `badging` on that APK (see *Deviations*).
  - The Release APK installed and run on the emulator: the camera starts after the permission is granted.
- Fix cycles used: **0 of 3**.
