# FEATURE-C1D1 — Release 1.4.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-c1d1-release-1-4-0`
**Run:** feature/2026-11-03-icons-msi-release

## Objective

Make the repository ready to release **1.4.0** — 1.3.0 with the minor incremented and the patch at 0 (the spec's
rule: x.y.z → x.(y+1).z, and z → 0 when it was above 0; it is 0 already) — carrying this run's work: the new icon
set without the tile (FEATURE-BA6C) and the Windows installer (FEATURE-4ED9). Nothing is tagged, pushed or
published by the run.

## Context & constraints

- One version for everything: `Directory.Build.props` `<Version>` (1.3.0 today);
  `src/Enigma.HardCopy.Desktop/app.manifest` mirrors it in 4-part form; the Android head derives `versionCode`
  (major×10000 + minor×100 + patch → 10400) and `versionName` from it; `buildmsi.ps1` reads it for the MSI.
- Release history: bare tags `1.0.0`, `1.1.0`, `1.3.0`. FEATURE-407E (1.3.0) is the template: version, manifest,
  release notes, README callout and install names, Release pre-flight, printed runbook.
- The house `dotnet-release` skill applies, app branch: no pack/push; a dependency check whose non-coupled bumps
  are applied and recorded (`old → new`), coupled sets (the Avalonia set with Enigma.Avalonia.Desktop, the
  AndroidX/CameraX set) held back unless they move together. The Windows installer is FEATURE-4ED9's script, so
  the skill's MSI-profile offer does not apply.
- An item of this run that ended quarantined is left out of the notes.

## Steps

1. Check that `git tag --list 1.4.0` is empty.
2. `Directory.Build.props` `<Version>1.4.0</Version>`; `app.manifest` `assemblyIdentity` `1.4.0.0`.
3. Dependency check: `dotnet list package --outdated` on the solution, logged; non-coupled patch and minor bumps
   applied in `Directory.Packages.props`; a bump that breaks the build or a test is reverted and recorded.
4. `RELEASENOTES.md`: a top `## 1.4.0` section in 1.3.0's shape — a summary paragraph; *A Windows installer*
   (`buildmsi`, what it installs, the runtime it needs, the upgrade behaviour from now on); *A new icon* (no tile;
   the desktop, About dialog and Linux launcher; Android unchanged); *Dependencies* (every `old → new`, or none,
   and what was held back); compatibility (format untouched; settings file unchanged).
5. `README.md`: the *What's new in 1.4.0* callout; the *Install* table and the APK name at 1.4.0, and the MSI
   as a Windows install option.
6. Pre-flight in Release configuration: build and whole suite; `ApplicationVersion` / `ApplicationDisplayVersion`
   read back (`10400` / `1.4.0`); the Desktop assembly's file version `1.4.0.0`; no stale `1.3.0` where the
   current version is meant.
7. Print the runbook in the completion doc: merges, the Release pre-flight, the artifacts (desktop zips, the MSI
   with `buildmsi`, the signed APK), the release gates, the bare `1.4.0` tag, the GitHub release.

## Acceptance criteria

- Release build clean (zero warnings beyond the baseline) and the whole suite green against the baseline at
  1.4.0; `versionCode` 10400, `versionName` 1.4.0.
- `RELEASENOTES.md` covers every `DONE` item of this run, and its quoted paths match the files.
- No stale `1.3.0` left where the current version is meant (README, props, manifest).
- The runbook printed; nothing tagged, pushed, published or installed.

## Out of scope

- Tagging, pushing, publishing, building the signed APK with the real key, building the MSI for real, the GitHub
  release.
- `SECURITY.md` and `docs/RELEASE.md` (the repository has neither; 1.3.0 did not add them).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | **1.4.0** | The spec's rule on 1.3.0 | 2.0.0 |
| `app.manifest` | `1.4.0.0` | Windows requires the 4-part form, bumped with the props as the comment there says | Leaving it |
| Dependency refresh | Check, apply non-coupled patch/minor bumps, hold coupled sets | The house release skill's routine release; the last refresh (FEATURE-E291) is days old, so little should move | Skipping the check; bumping coupled sets piecemeal |
| MSI profile | None — `buildmsi.ps1` generates it per build | FEATURE-4ED9's design | A committed `msiProfiles/` file |
| Tag form | Bare `1.4.0`, printed only | Matches the existing tags | `v1.4.0` |
| `SECURITY.md` / `docs/RELEASE.md` | Not created | Neither exists, and 1.3.0 did not add them; the spec asks for a release, not new documents | Creating them |
