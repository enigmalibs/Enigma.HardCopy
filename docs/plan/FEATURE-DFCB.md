# FEATURE-DFCB — Android app release 1.2.0

**Status:** IN PROGRESS
**Type:** multi-phase feature (3 phases)
**Branches (created by the run, each from the run-branch tip):** one per phase, see *Phases*
**Run:** feature/2026-10-24-android-app-release

## Objective

Make the repository ready to release **1.2.0** — the first release that ships the Android recovery app,
*HardCopy Recover* (FEATURE-E931, plus the *Check a printed backup* mode of FEATURE-3941):

- a **release-signed, upgradeable APK**, produced by a repeatable script, the way the desktop zips already are;
- the house release routine: a dependency refresh with the coupled sets held back, the version bump, release
  notes, the README, and a **printed** merge / tag / publish runbook.

Nothing is tagged, pushed, uploaded or published by the run.

## Context & constraints

- **One version for everything.** `Directory.Build.props` carries the only `<Version>` (1.1.0); the desktop
  publish scripts read it, `app.manifest` mirrors it in 4-part form, and the Android head's
  `ApplicationDisplayVersion` is `$(Version)`.
- **The Android head is not release-ready.** `ApplicationVersion` (Android's `versionCode`) is a hand-kept `1`,
  and the only documented APK build is signed with the .NET SDK's local debug key, which `docs/android.md`
  itself calls "fine for your own phones, not for publishing". A debug key differs per machine, so an APK built
  elsewhere could never update one built here.
- **Release history.** Tags `1.0.0` and `1.1.0` (bare), each on a `main` merge of `develop`. 1.1.0 was cut by a
  release phase (FEATURE-5CBC PHASE05): version, manifest, notes, README, package audit, printed runbook.
- **Packages** (`dotnet list package --outdated`, 2026-10-24): QRCoder 1.8.0 → 1.8.1, QuestPDF 2026.7.2 →
  2026.9.1, ZXing.Net.Bindings.SkiaSharp 0.16.22 → 0.16.24, Microsoft.Extensions.Hosting / Logging.Console /
  Logging.Debug 10.0.10 → 10.0.13, coverlet.collector 10.0.1 → 10.1.0 — all outside the coupled sets. Coupled or
  major: the Avalonia set 12.1.1 → 12.1.4 (with `Avalonia.Android`, `Enigma.Avalonia.Desktop` 1.0.0 → 1.2.0, the
  icon packs and SkiaSharp 3.119.4 → 4.153.1), CameraX 1.6.1 → 1.6.2, xunit.v3 3.2.2 → 4.0.2.
- **Build environment.** Android workload 36.1.118, build-tools 36.0.0 (`apksigner`, `aapt2`), `keytool`, the
  `Pixel_6_API_36` AVD. No PowerShell on this machine.
- Zero warnings (`TreatWarningsAsErrors`); the environmental `XA0033` (the Android tooling cannot parse this
  machine's JDK version string) is recorded, not fixed — see FEATURE-3941's completion doc.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | **1.2.0**, solution-wide; `app.manifest` → `1.2.0.0` | A new app and a new feature, nothing broken — a minor. One number per release is the repository's rule, so a printout traces back to its build | A separate Android version line (breaks the one-number rule); 2.0.0 (nothing breaks) |
| Android `versionCode` | Derived from `<Version>` in the csproj: major×10000 + minor×100 + patch (1.2.0 → 10200) | Monotonic with releases by construction, so no release can forget it; far above the `1` of the development builds | Keeping a hand-bumped integer (one more release step to forget); a build counter (needs CI the project lacks) |
| Release signing | A dedicated release keystore created once by the maintainer, kept outside the repository; passwords from environment variables passed to MSBuild as `env:` references; only the release script turns signing on | The signing certificate is the app's identity on every phone — one key, held by the maintainer, is what makes later releases install as updates; `env:` keeps passwords out of the command line and the process list | Keeping the SDK debug key (per-machine, unshareable, not for publishing); signing properties in the csproj (would force every local build to need the key); committing a keystore (a secret in git) |
| Creating the keystore | **Printed** `keytool` command for the maintainer; the run never creates or stores a real key | A signing key is a secret and an identity — the user owns it, as they own tags and publishes | Generating one in the run |
| Guarding against committing a key | `.gitignore` gains `*.keystore` and `*.jks` | Cheap, and the failure it prevents cannot be undone once pushed | Relying on care |
| Release script | `scripts/publish-android.sh` + `scripts/publish-android.ps1`, twins in the style of `publish.sh` / `.ps1`, writing `artifacts/Enigma.HardCopy.Recover-<version>.apk`; they **refuse to run** without the signing variables | Mirrors how the desktop artifacts are produced; failing closed means a debug-signed APK can never be shipped by accident | Extending `publish.sh` with an `android` target (signing variables would then gate desktop publishing too); documenting a raw `dotnet publish` line (not repeatable) |
| Package format and ABIs | One APK, `android-arm64` + `android-x64`, as today | Sideloading is the distribution; x64 serves emulators and Chromebooks | An AAB (Play Store only, out of scope); arm64 only |
| Dependency refresh | Apply the non-coupled bumps; hold back the Avalonia set, CameraX (its 1.6.2 moves AndroidX Lifecycle past what `Avalonia.Android` 12.1.1 pins — NU1608) and xunit.v3 4.x (a test-framework migration, its own work item); log every `old → new` and every held-back set | The house release skill's rule; patch-level fixes reach both apps without moving a coupled set | Bumping everything (breaks the coupled sets); bumping nothing (ships known-stale patches) |
| A bump that breaks the build or the suite | Held back and recorded, not forced | The release is not the place to absorb a dependency's breaking change | Patching code around it |
| Desktop artifacts | Rebuilt at 1.2.0 by the user with the existing scripts, listed in the runbook | Every artifact of a release carries the same number; the scripts already exist | Releasing only the APK under 1.2.0 (two numbers in one release) |
| Real-phone verification | A **release gate** in the printed runbook: install the signed APK on a physical phone, recover a printed backup and run a check, before tagging | Decoding printed codes with a real camera has never been observed (FEATURE-E931); no emulator can do it, so the run cannot either | Releasing without it |
| Phones with a development build | Documented: uninstall first — the release key differs from the debug key, and the app stores nothing to lose | Android refuses an update signed by a different certificate | Ignoring it |
| MSI profile (house release skill, apps) | Not generated | An APK cannot have one; the desktop app ships zips and has no profile history | Generating one for the desktop app |
| `docs/RELEASE.md` | Not created | A routine release of this repository; the skill's template is NuGet-oriented; the runbook is printed and `docs/android.md` documents the APK build | Adapting the NuGet template |
| Tag | Bare `1.2.0` on `main` after merging `develop` — printed, never run | Matches `1.0.0` and `1.1.0` | `v1.2.0` |

## Phases

### PHASE01 — Signed release APK build (DONE)

Branch: `feature/feature-dfcb-phase01-signed-apk`

1. `src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj`: `ApplicationVersion` derived
   from `$(Version)` (major×10000 + minor×100 + patch), the comment explaining the scheme and its bounds.
2. `scripts/publish-android.sh` and `scripts/publish-android.ps1`: read the version from `Directory.Build.props`;
   require `HARDCOPY_ANDROID_KEYSTORE` (path, must exist) and `HARDCOPY_ANDROID_KEYSTORE_PASSWORD`, optional
   `HARDCOPY_ANDROID_KEY_ALIAS` (default `hardcopy-recover`); `dotnet publish` Release, APK format, signed with
   that key through `AndroidSigning*` properties whose passwords are `env:` references; copy the signed APK to
   `artifacts/Enigma.HardCopy.Recover-<version>.apk`; print its path and size.
3. `.gitignore`: `*.keystore`, `*.jks`.
4. `docs/android.md`: a *Release APK* section — creating the key once (printed `keytool` command, where to keep
   it, what losing it means), the environment variables, running either script, verifying the result
   (`apksigner verify --print-certs`), and the uninstall-first note for phones with a development build; the
   debug-signed standalone build kept for local testing.
5. `README.md`, *Publishing the release artifacts*: the Android scripts beside the desktop ones.

**Acceptance:** solution builds with zero warnings (beyond the environmental `XA0033`); suite green; with a
throwaway keystore in the session scratchpad, `scripts/publish-android.sh` produces
`artifacts/Enigma.HardCopy.Recover-1.1.0.apk` whose `aapt2 dump badging` shows `versionCode='10100'` and
`versionName='1.1.0'` and whose `apksigner verify --print-certs` shows the throwaway certificate, not the debug
one; without the variables the script stops with an explanation and builds nothing; the APK installs and
launches on the emulator; the throwaway key and the artifact are deleted afterwards. The PowerShell twin is run
if a PowerShell can be obtained locally (scratchpad only), otherwise recorded as not run.

### PHASE02 — Dependency refresh (DONE)

Branch: `feature/feature-dfcb-phase02-dependency-refresh`

1. `Directory.Packages.props`: QRCoder 1.8.1, QuestPDF 2026.9.1, ZXing.Net.Bindings.SkiaSharp 0.16.24,
   Microsoft.Extensions.Hosting / Logging.Console / Logging.Debug 10.0.13, coverlet.collector 10.1.0 — each kept
   only if restore, build and the suite stay clean; any that does not is reverted and recorded.
2. Held back, with the reason in the props comments where it is not already there: the Avalonia set (incl.
   `Avalonia.Android`, `Enigma.Avalonia.Desktop`, the icon packs, SkiaSharp), CameraX, xunit.v3 4.x.
3. `dotnet list package --vulnerable --include-transitive` — recorded.

**Acceptance:** restore without NU16xx warnings; solution builds with zero warnings (beyond `XA0033`); suite
green; the Android release APK still builds; every `old → new` and every held-back set recorded in the completion
doc for PHASE03's release notes.

### PHASE03 — Release 1.2.0: version, notes, runbook

Branch: `feature/feature-dfcb-phase03-release-1-2-0`

1. `Directory.Build.props` `<Version>1.2.0</Version>`; `app.manifest` `assemblyIdentity` `1.2.0.0`.
2. `RELEASENOTES.md`: a top `## 1.2.0` section — HardCopy Recover for Android (what it does, what it never
   stores, how it is installed and verified, the uninstall-first note), *Check a printed backup*, Core's new
   camera-frame decoding overload, compatibility (format untouched; Android 7.0+, 64-bit; desktop app
   unchanged), dependencies (new Android packages, every `old → new`, the held-back sets), the APK's measured
   size, the license audit of what the APK ships, known limitations.
3. `README.md`: the what's-new callout, the Install table at 1.2.0 with the APK row.
4. Pre-flight in Release configuration: build and test; the release APK built with a throwaway key and
   inspected (`versionName` 1.2.0, `versionCode` 10200, signature), installed on the emulator; the scratch
   artifacts deleted.
5. Print the runbook: physical-phone gate, merges, tag, publishing both artifact sets, the GitHub release.

**Acceptance:** Release build clean and suite green at 1.2.0; no stale `1.1.0` left where the current version is
meant (README, manifest, props); the APK reports 1.2.0 / 10200; the runbook printed; nothing tagged or pushed.

## Out of scope

- Tagging, pushing, publishing, creating the GitHub release, creating the real signing key.
- The Avalonia-set, CameraX and xunit.v3 4.x upgrades (held back, recorded).
- Play Store distribution, an AAB, store listing, Play App Signing.
- Splitting Core so the APK stops carrying QuestPDF's managed assembly (FEATURE-E931 follow-up).
- Any application behaviour change.
