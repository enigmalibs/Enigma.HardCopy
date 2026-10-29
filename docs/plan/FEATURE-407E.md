# FEATURE-407E — Release 1.3.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-407e-release-1-3-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Make the repository ready to release **1.3.0** — 1.2.0 with the minor incremented and the patch at 0 — carrying
this run's work: the dependency refresh (FEATURE-E291), the new icons (FEATURE-1148), the icon beside the
executable (FEATURE-C5C6), the About dialog (FEATURE-7B95), the error dialog (FEATURE-555A), the Linux installer
(FEATURE-3D14) and the publish scripts (FEATURE-92E2). Nothing is tagged, pushed or published by the run.

## Context & constraints

- One version for everything: `Directory.Build.props` `<Version>`; `src/Enigma.HardCopy.Desktop/app.manifest`
  mirrors it in 4-part form; the Android head derives `versionCode` (major×10000 + minor×100 + patch → 10300) and
  `versionName` from it.
- Release history: bare tags `1.0.0`, `1.1.0`, `1.2.0` on `main` merges of `develop`; FEATURE-DFCB PHASE03 is the
  last release phase (version, manifest, notes, README, pre-flight, printed runbook) — its shape is reused.
- The house `dotnet-release` skill applies (an app: release notes, README, package audit, printed runbook; the
  MSI profile was not generated for 1.2.0 and is not now).
- Two spec items needed no work and are recorded here: **no splash screen** exists in either app, and **no
  `net8.0` target** exists anywhere (every project is `net10.0` / `net10.0-android`).

## Steps

1. `Directory.Build.props` `<Version>1.3.0</Version>`; `app.manifest` `assemblyIdentity` `1.3.0.0`.
2. `RELEASENOTES.md`: a top `## 1.3.0` section — the About dialog, the error dialog, the new icons (desktop and
   Android), `app.ico` beside the executable, the Linux installer, the publish scripts, the dependencies (every
   `old → new` from FEATURE-E291's completion docs, and the held-back set with its reason), compatibility (format
   untouched; settings file unchanged).
3. `README.md`: the *What's new in 1.3.0* callout; the *Install* table and the APK name at 1.3.0.
4. Pre-flight in Release configuration: build and test; `ApplicationVersion` / `ApplicationDisplayVersion` read
   back (`10300` / `1.3.0`); the desktop app's About dialog shows 1.3.0; no stale `1.2.0` where the current
   version is meant.
5. Print the runbook in the completion doc: merges, the Release pre-flight, the release artifacts (desktop zips,
   signed APK — real-phone gate as in 1.2.0), the bare `1.3.0` tag, the GitHub release.

## Acceptance criteria

- Release build clean and suite green at 1.3.0; `versionCode` 10300, `versionName` 1.3.0.
- No stale `1.2.0` left where the current version is meant (README, props, manifest).
- The runbook printed; nothing tagged, pushed or published.

## Out of scope

- Tagging, pushing, publishing, building the signed APK with the real key, the GitHub release.
- An MSI profile.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | **1.3.0** | The spec's rule: x.y.z → x.(y+1).0 | 2.0.0 |
| `app.manifest` | `1.3.0.0` | Windows requires the 4-part form, bumped with the props as the comment there says | Leaving it |
| Splash screen / net8 | Recorded as not applicable; no work item | Neither exists in the codebase | Empty work items |
| Tag form | Bare `1.3.0`, printed only | Matches the three earlier tags | `v1.3.0` |
| MSI profile | Not generated | The desktop app ships zips; the 1.2.0 decision holds | Generating one |
