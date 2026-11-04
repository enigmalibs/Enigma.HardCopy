# FEATURE-DA65 — Release 1.5.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-da65-release-1-5-0`
**Run:** feature/2026-11-04-icons-release

## Objective

Make the repository ready to release **1.5.0**. That is 1.4.0 with the minor incremented, following the spec's
rule x.y.z → x.(y+1).z; the patch is already 0. The release carries this run's work: the icon set that fills the
frame (FEATURE-3642). The Windows installer build must produce `Enigma.HardCopy.1.5.0.msi`. The run tags, pushes,
publishes and builds nothing.

## Context & constraints

- **One version for everything:**
  - `Directory.Build.props` `<Version>`, 1.4.0 today;
  - `src/Enigma.HardCopy.Desktop/app.manifest`, which mirrors it in 4-part form;
  - the Android head, which derives `versionCode` (major×10000 + minor×100 + patch → 10500) and `versionName`
    from it.
- **`packaging/windows/buildmsi.ps1` carries no version number.** It reads `<Version>` from `Directory.Build.props`
  with `XmlDocument`, checks it against `^\d+\.\d+\.\d+$`, and derives every name from it: the MSI, the profile and
  the `version` field. This is FEATURE-4ED9's single source of truth. Once the props say 1.5.0, the script builds
  `Enigma.HardCopy.1.5.0.msi` with no edit of its own. `WindowsInstallerTests` pins the script's constants, but not
  where its version comes from.
- Windows Installer compares the first three fields of `ProductVersion`, and the upgrade code is unchanged. 1.5.0
  therefore upgrades 1.4.0 in place.
- Release history: bare tags `1.0.0`, `1.1.0`, `1.3.0`, `1.4.0`. FEATURE-C1D1 (1.4.0) is the template: version,
  manifest, release notes, the README callout and install names, a Release pre-flight and the printed runbook.
- The house `dotnet-release` skill applies, on its app branch:
  - no pack or push;
  - a dependency check whose non-coupled bumps are applied and recorded (`old → new`);
  - coupled sets held back unless they can move together: the Avalonia set with `Enigma.Avalonia.Desktop`,
    SkiaSharp with the ZXing binding, and the AndroidX/CameraX set.

  `buildmsi.ps1` generates the MSI profile, so the skill's MSI-profile offer does not apply.
- An item of this run that ends quarantined is left out of the notes.

## Steps

1. Check that `git tag --list 1.5.0` is empty.
2. Set `Directory.Build.props` `<Version>1.5.0</Version>` and `app.manifest` `assemblyIdentity` `1.5.0.0`.
3. `buildmsi.ps1` stays as it is. Add a `WindowsInstallerTests` fact requiring the script to take its version from
   `Directory.Build.props`'s `/Project/PropertyGroup/Version`, so that the MSI's version cannot drift from the
   app's. Then read the version back the way the script does, without running the script (it starts with
   `git clean -dxf`): load the props with `XmlDocument`, select the node, and check the result is `1.5.0`.
4. Dependency check:
   - run `dotnet list package --outdated` and `--vulnerable --include-transitive` on the solution, and log the
     output;
   - apply non-coupled patch and minor bumps in `Directory.Packages.props`;
   - revert and record any bump that breaks the build or a test.
5. `RELEASENOTES.md`: a top `## 1.5.0` section in 1.4.0's shape, with these parts:
   - a summary paragraph;
   - *A larger icon*: fills its frame; the desktop, taskbar, About dialog, MSI shortcuts and Linux launcher;
     Android unchanged;
   - the compatibility section (format untouched; settings file unchanged; the MSI upgrades 1.4.0 in place;
     `versionCode` 10500);
   - *For developers* (suite count);
   - *Dependencies*: every `old → new` change, or none, and what was held back;
   - *Known limitations*, carried over if still true.
6. `README.md`: the *What's new in 1.5.0* callout, and the archive, MSI and APK names at 1.5.0.
7. Pre-flight in Release configuration:
   - build and whole suite;
   - `ApplicationVersion` / `ApplicationDisplayVersion` read back as `10500` / `1.5.0`;
   - the Desktop assembly's file version is `1.5.0.0`;
   - no stale `1.4.0` where the current version is meant.
8. Print the runbook in the completion doc: merges, Release pre-flight, artifacts (desktop zips, the MSI with
   `buildmsi`, the signed APK), release gates, the bare `1.5.0` tag and the GitHub release.

## Acceptance criteria

- At 1.5.0, the Release build is clean (zero warnings beyond the baseline) and the whole suite is green against
  the baseline. `versionCode` is 10500 and `versionName` is 1.5.0.
- The `buildmsi.ps1` version read gives `1.5.0`, so the script would name its output `Enigma.HardCopy.1.5.0.msi`.
  The new `WindowsInstallerTests` fact passes.
- `RELEASENOTES.md` covers every `DONE` item of this run, and the paths it quotes match the files.
- No stale `1.4.0` is left where the current version is meant (README, props, manifest).
- The runbook is printed. Nothing is tagged, pushed, published, built into an MSI or installed.

## Out of scope

- Tagging, pushing, publishing, building the signed APK with the real key, running `buildmsi` for real, and the
  GitHub release.
- `SECURITY.md` and `docs/RELEASE.md`. The repository has neither, and 1.3.0 and 1.4.0 did not add them.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | **1.5.0** | The spec's rule applied to 1.4.0 | 2.0.0; 1.4.1 |
| "Update the new version number in the buildmsi script" | No edit to the script: it reads `<Version>` from the props. A test pins that, and a dry read shows 1.5.0 | The spec's intent is an MSI at 1.5.0, which the props bump delivers. A number written into the script would be a second source of truth, and it would drift at the next release | Hard-coding `1.5.0` in `buildmsi.ps1`; a `-Version` parameter |
| Proving the MSI version without building it | Read the props as the script does, and don't run the script | The script starts with `git clean -dxf`, which would delete the run's untracked files, and it needs the WiX CLI | Running `buildmsi.ps1` |
| `app.manifest` | `1.5.0.0` | Windows requires the 4-part form, bumped with the props as the comment there says | Leaving it |
| Dependency refresh | Check, apply non-coupled patch/minor bumps, hold coupled sets | The house release skill's routine release | Skipping the check; bumping coupled sets piecemeal |
| Tag form | Bare `1.5.0`, printed only | Matches the existing tags | `v1.5.0` |
| `SECURITY.md` / `docs/RELEASE.md` | Not created | Neither exists, and the previous releases did not add them | Creating them |
