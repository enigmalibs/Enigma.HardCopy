# FEATURE-DFCB-PHASE01 — Signed release APK build — DONE

**Branch:** `feature/feature-dfcb-phase01-signed-apk`
**Date:** 2026-10-24
**Run:** feature/2026-10-24-android-app-release

## Summary

The Android app can now be built as a **release**: an APK signed with the project's own release key, with a
version Android can order. Until now the only documented APK was signed with the .NET SDK's per-machine debug key
and carried a hand-kept `versionCode` of `1`.

- **`versionCode` follows the release version.** The Android head derives `ApplicationVersion` from the
  solution's `<Version>` as major × 10000 + minor × 100 + patch. 1.1.0 builds report `10100` and 1.2.0 builds
  will report `10200`. It climbs with every release without a step to remember, and is far above the `1` of
  every earlier build.
- **`scripts/publish-android.sh` / `.ps1`** — twins in the style of the desktop publish scripts. Each:
  - reads the version from `Directory.Build.props`;
  - requires `HARDCOPY_ANDROID_KEYSTORE` (an existing file) and `HARDCOPY_ANDROID_KEYSTORE_PASSWORD`, with
    `HARDCOPY_ANDROID_KEY_ALIAS` optional (default `hardcopy-recover`);
  - publishes Release as an APK, signed through the `AndroidSigning*` properties with the passwords passed as
    `env:` references;
  - copies the signed package to `artifacts/Enigma.HardCopy.Recover-<version>.apk`.

  Without the key they **refuse to build**, so a debug-signed APK cannot be shipped by accident.
- **`.gitignore`** ignores `*.keystore` and `*.jks`, as a safety net for the key.
- **`docs/android.md`** gains a *Release APK* section:
  - the one-time `keytool` command, where to keep the key, and what losing or leaking it means;
  - the environment variables and both scripts;
  - checking the result with `apksigner` and `aapt2`;
  - the uninstall-first rule for phones that have a debug-signed build.

  The standalone debug-signed build stays, now labelled *for testing*. The *Release APK* section becomes 5, so
  troubleshooting and *What was verified* renumber to 6 and 7, and the intro's link follows.
- **`README.md`**, *Publishing the release artifacts*: the Android pair beside the desktop pair.

## Files/modules touched

**Created**

- `scripts/publish-android.sh` (executable) and `scripts/publish-android.ps1`
- `docs/done/FEATURE-DFCB-PHASE01.md` — this file.

**Modified**

- `src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj` — three private
  `_ReleaseVersion*` properties and the derived `ApplicationVersion`, with the scheme and its bounds commented.
- `.gitignore` — the two key patterns. The file had no final newline; it now has one.
- `docs/android.md` — the *Release APK* section, the standalone build relabelled, the renumbering, and the
  release-APK runs under *What was verified*.
- `README.md` — *Publishing the release artifacts*.
- `docs/roadmap.md`, `docs/plan/FEATURE-DFCB.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the `versionCode` is computed | MSBuild property functions over `System.Version.Parse($(Version))`, in three private `_ReleaseVersion*` properties | Readable, no target or task, evaluated before the Android targets read `ApplicationVersion`; a non-`X.Y.Z` version fails loudly at evaluation | An inline one-liner (unreadable); a custom target (runs too late for some Android targets) |
| Guard for minor/patch ≥ 100 | Documented in the csproj comment, not enforced | No realistic version reaches it; an `<Error>` target would be code for a case that will not occur | A build error target |
| Keystore type and password | PKCS12, one password for store and key; one password variable feeding both `AndroidSigningStorePass` and `AndroidSigningKeyPass` | PKCS12 ignores a separate key password, and keytool's default is PKCS12 | Two password variables |
| Absolute keystore path | The scripts resolve it before calling MSBuild | MSBuild resolves `AndroidSigningKeyStore` against the project directory, so a relative path from the caller would break | Requiring an absolute path from the user |
| Which file is the artifact | Exactly one `*-Signed.apk` in the stage, else an error | The build leaves an unsigned package beside the signed one; only the signed one installs | Taking the first `*.apk` |
| Verifying the PowerShell twin | Installed PowerShell 7.6.8 as a local .NET tool **in the session scratchpad** and ran the script under it | The twin would otherwise ship unrun; the tool lives outside the repository and the user's environment | Recording it as not run |
| Docs troubleshooting row for the signature conflict | Deferred to PHASE03, which reinstalls over a debug build on the emulator and captures the exact message | The first attempt's error text was not captured; the guide should quote only what was observed | Writing the expected text unverified |

## Deviations & follow-ups

- **Build warning `XA0033` (environmental), as in FEATURE-3941.** The Android tooling cannot parse this
  machine's JDK version string. There are zero compiler or analyzer warnings. Not fixable from the repository.
- **`publish-android.ps1` has not run on Windows.** It ran under PowerShell 7.6.8 on Linux, which exercises the
  same script. The guide's PowerShell example uses `Read-Host -MaskInput` and says it needs 7.1+; the script
  itself avoids anything newer than Windows PowerShell 5.1.
- **What the release key's file name and alias will be is the maintainer's call.** The guide and the script
  default to `hardcopy-recover`; any other alias works through `HARDCOPY_ANDROID_KEY_ALIAS`.
- Line endings: every new and touched file is LF, as `.gitattributes` requires; nothing to recommend.
- Documentation sweep: `README.md` and `docs/android.md` are updated as planned. `RELEASENOTES.md` is left to
  PHASE03, which writes the 1.2.0 section. Nothing else mentions APK builds.

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` — 0 errors; the one warning is the environmental `XA0033`.
- `dotnet test --solution Enigma.HardCopy.slnx` — **796 passed, 0 failed, 0 skipped**. No test changes; this
  phase changes no application code.
- `dotnet msbuild … -getProperty:ApplicationVersion` → `10100` at 1.1.0; with `-p:Version=3.14.15` → `31415`.
- **Without the signing variables:** both scripts stop with an explanation and exit 1 before building anything.
  A non-existent keystore path is refused too.
- **With a throwaway key** (PKCS12, RSA 4096, `CN=Throwaway test key`, made in the session scratchpad):
  - `scripts/publish-android.sh` and `scripts/publish-android.ps1` each produced
    `artifacts/Enigma.HardCopy.Recover-1.1.0.apk` (42,231,208 bytes).
  - `aapt2 dump badging`: `versionCode='10100' versionName='1.1.0'`, minSdk 24, permissions `CAMERA` (plus
    AndroidX's internal `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`), native code `arm64-v8a` and `x86_64`.
  - `apksigner verify --print-certs`: `CN=Throwaway test key, O=FEATURE-DFCB verification`, not
    `CN=Android Debug`.
  - On the `Pixel_6_API_36` emulator, the APK installed after the debug-signed build was uninstalled. It reported
    `versionCode=10100 versionName=1.1.0` and started, with both start buttons in its accessibility tree and an
    empty crash log.
  - A stale debug APK with `versionCode` 1 was then refused over it (`INSTALL_FAILED_VERSION_DOWNGRADE`).
- The `artifacts/` directory the scripts created was deleted afterwards. The throwaway key stays in the session
  scratchpad for PHASE03's checks and is never in the repository.
