# FEATURE-5CBC-PHASE05 — Release readiness: 1.1.0 — DONE

**Branch:** `feature/feature-5cbc-phase05-release`
**Date:** 2026-08-05

## Summary

The repository is release-ready at **1.1.0**. This phase wrote no application code: the four phases before it
built the thing, and this one makes the repository say so consistently — one version number in one place, a
release-notes section that a user reads before deciding whether to upgrade, and a README that describes the
application that now exists rather than the one 1.0 shipped.

**A minor, not a patch and not a major.** User-visible functionality is added — a navigation rail, a settings
page, a remembered appearance, in-window confirmations, a progress overlay and an information bar — and
nothing breaks. The `EHC1` code grammar, the metadata key/value grammar, the recovery instructions printed on
page 1, the A4 page layout and the SHA-256 verification are all untouched, and `Enigma.HardCopy.Core` has not
been modified by any of the five phases. **Every backup printed by 1.0 still recovers, byte for byte** — which
is the single most important thing this release has to communicate, so it is stated in the release notes'
first paragraph, in its own *Compatibility* section, and in the README's what's-new callout.

The package audit found **no vulnerability, direct or transitive**, and one deliberate pin: SkiaSharp 3.119.4
against an available 4.151.0, held there because Avalonia 12.1.1 and the ZXing SkiaSharp binding must resolve
to one Skia runtime. That set moves as a unit or not at all, and not in this item.

**The artifact-growth estimate was measured rather than repeated.** The plan predicted ~9 MB per RID from
Phosphor, BouncyCastle and the library; a real linux-x64 publish measures 125,226,563 → 134,622,655 bytes,
which is +9.0 MB and +7.5 % — the estimate was right, and the release notes now carry measured numbers.

Nothing is published, tagged or pushed. The runbook is printed for the user, per the plan.

## Files/modules touched

**Modified**

- `Directory.Build.props` — `<Version>1.0.0</Version>` → `1.1.0`. This is the only place a version number
  lives: both publish scripts `sed`/parse it out of this file, so the artifact names follow with no script
  change (verified — they would now produce `Enigma.HardCopy-1.1.0-<rid>.zip`).
- `src/Enigma.HardCopy.Desktop/app.manifest` — `assemblyIdentity version` → `1.1.0.0`, the 4-part form
  Windows requires, bumped alongside the props value exactly as that file's comment instructs.
- `RELEASENOTES.md` — restructured from a single-release file into a cumulative one: the title drops its
  version, a `## 1.1.0` section goes on top, and the existing 1.0.0 content is preserved verbatim under a
  `## 1.0.0` heading with its sub-headings demoted one level. Nothing in the 1.0.0 text was reworded.
- `README.md` — the what's-new callout, the Install table's two zip names, a new *Getting around* section
  (the rail, the overlay, the information bar), the rail mentioned where each page is opened, the *Generate
  PDF* step rewritten around the progress card, the hash-mismatch paragraph extended with the confirmation
  and its *Save unverified* button, a *Start over* paragraph, a new *Settings* section, the intro line
  extended with the library and the icon pack, and Phosphor's upstream MIT attribution under *License*.
- `docs/roadmap.md`, `docs/plan/FEATURE-5CBC.md` — PHASE05 and the item itself to `DONE`.

**Created** — `docs/done/FEATURE-5CBC-PHASE05.md` (this file).

**Untouched** — all of `src/` and `tests/` except the one manifest line; `scripts/publish.sh`,
`scripts/publish.ps1` (they need no edit — see above); `Directory.Packages.props`; `LICENSE.md`; `global.json`.
No zip, no tag, no push, no GitHub release.

## Deviations & follow-ups

1. **`RELEASENOTES.md` was restructured, not just prepended to.** The file was titled
   *"Enigma.HardCopy v1.0.0 Release Notes"* with `##` sections beneath it, which cannot hold a second release.
   It is now *"Enigma.HardCopy Release Notes"* with one `##` per version, newest first. The 1.0.0 prose is
   unchanged; only its heading levels moved. The plan asked for "a top 1.1.0 section", which this is — but it
   is worth recording that the file's shape changed rather than only grew.
2. **The project-layout table was left alone.** The plan says to update it "if it moved". It did not: the four
   rows still describe the repository accurately at that granularity, and the new subsystems all live inside
   `src/Enigma.HardCopy.Desktop/`, which the table already covers in one line.
3. **The artifact figures are measured, not estimated.** A `dotnet publish` was run into the session scratchpad
   with the publish scripts' exact flags, purely to weigh the result; no artifact was written to `artifacts/`
   and no script was invoked. The 1.0.0 comparison uses the real `artifacts/publish/…-1.0.0-linux-x64` stage
   still on disk from the previous release. The 1.1.0 stage lacks the `README.md`/`LICENSE.md` the script
   copies in (~8 KB), which does not move either figure.
4. **win-x64 was not measured.** Only linux-x64 was published, on Linux. The release notes give the linux-x64
   numbers explicitly and describe the growth as "about 9 MB per platform", since the added assemblies are
   managed and RID-independent. A win-x64 figure is available to the user from their own publish run.
5. **SkiaSharp 4.151.0 is available and deliberately not taken.** Recorded in the release notes as part of the
   Avalonia coupling rather than as a to-do, because it is a standing constraint, not an oversight. It becomes
   actionable only when the whole Avalonia group moves.
6. **The screenshots were taken from the published self-contained artifact**, not from `bin/Release`. The
   framework-dependent apphost in `bin/` cannot start without `DOTNET_ROOT` set in the environment, and the
   published binary is in any case the thing users will run — a better subject for a release-readiness check.
7. **The theme was selected through the real preference file**, not a scaffold. Unlike every earlier phase,
   this one needed no temporary code at all to reach both variants: `settings.json` was written before each
   launch and deleted afterwards, leaving the developer's machine in the clean first-run state it was in
   before (it had no settings file).
8. **Pointer input still cannot be injected** into this session (recorded in PHASE02), so both screenshots show
   the Backup page — whichever page the rail selects at startup. The Settings page and the Recovery page in
   both variants were photographed in PHASE04 and PHASE03 respectively, on the same code.
9. **Line endings:** nothing to report. Every touched file is LF and `.gitattributes` already declares
   `* text=auto eol=lf`; no action taken, per the workflow's recommendation-only rule.

## Build/test evidence

```
dotnet clean && dotnet build -c Debug   --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet clean && dotnet build -c Release --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx              → total: 682  failed: 0  succeeded: 682  skipped: 0
dotnet test --solution Enigma.HardCopy.slnx -c Release   → total: 682  failed: 0  succeeded: 682  skipped: 0
```

**`AVLN*` warnings: zero.** Both full `--no-incremental` logs were searched for the string `AVLN` explicitly
(0 hits each), because `TreatWarningsAsErrors` does not promote them.

**No test was added or changed by this phase** — it changes no behaviour. 682 is PHASE04's count, unmoved.

**The version reached the assembly:** the Release `Enigma.HardCopy.Desktop.dll` carries `1.1.0.0` and an
informational version of `1.1.0+feeeb3c…`.

**No stale version string.** A repository-wide grep for `1.0.0` outside `docs/plan/`, `docs/done/` and
`artifacts/` returns only the 1.0.0 release-notes section and the two deliberate comparisons against it. The
manifest reads `1.1.0.0`.

### Package audit

```
dotnet list package --outdated
  Enigma.HardCopy.Core            SkiaSharp                    3.119.4 → 4.151.0   (pinned, see below)
  Enigma.HardCopy.Core.UnitTests  SkiaSharp.NativeAssets.Linux 3.119.4 → 4.151.0   (same pin)
  Enigma.HardCopy.Desktop         no updates
  Enigma.HardCopy.Desktop.UnitTests  no updates

dotnet list package --vulnerable --include-transitive
  no vulnerable packages in any of the four projects
```

The two SkiaSharp packages are held at the version Avalonia 12.1.1 itself depends on: the ZXing SkiaSharp
binding and Avalonia.Skia must resolve to one Skia runtime, and that group moves as a unit or not at all. Not
in this item, by the plan's own decision.

### Artifact size, measured

| | 1.0.0 | 1.1.0 | Δ |
|---|---|---|---|
| linux-x64 stage, unpacked | 125,226,563 B (120 MB) | 134,622,655 B (129 MB) | +9.0 MB, +7.5 % |
| linux-x64 zipped | 54,274,070 B (51.8 MB) | 58,170,891 B (55.5 MB) | +3.7 MB, +7.2 % |

Matching the plan's estimate of ~9 MB: Phosphor's glyph data ~3.9 MB, BouncyCastle ~4.7 MB, the control
library and the two icon assemblies ~0.3 MB.

### Verified in the running app (Linux, `DISPLAY=:1`)

The self-contained **linux-x64 publish of 1.1.0** — the artifact shape users actually run — was launched twice,
once per stored appearance, and grabbed by its own X window id (`ffmpeg -f x11grab -window_id … -i :1`; an
X-root grab is empty under this rootless Xwayland session, as PHASE01 recorded).

- **`theme: "Dark"`** → the whole application dark: rail on `#1E1F22`, cards `#2B2D30`, the `QrCode` /
  `ArrowsCounterClockwise` / `Gear` rail glyphs, the density `SettingsCard` with its `Barcode` icon, and
  icons on all three command buttons.
- **`theme: "Light"`** → the same window light, on a desktop whose OS variant is dark. That difference is the
  proof the persisted preference is being honoured by the shipped artifact and not merely following the OS,
  and it is the strongest single check available on a release build: the preference was on disk before the
  process started.
- Both launches logged a clean start (`Application started`) with no warning, and the settings file was left
  exactly as written.

**Screenshots kept:** the finished 1.1.0 application in Dark and in Light.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `<Version>1.1.0</Version>` in `Directory.Build.props`; `app.manifest` `assemblyIdentity` at `1.1.0.0` | ✅ (both; artifact names follow with no script change) |
| 2 | `RELEASENOTES.md` 1.1.0 section — the new UI, the four packages + ~9 MB note, the new `settings.json` (additive, absent is normal), and an explicit statement that every 1.0 backup still recovers | ✅ (size note measured, not estimated — *Deviations* 3) |
| 3 | `README.md` — what's-new callout, Install zip names, walkthroughs updated for the rail / overlay / InfoBar / confirmations, a *Settings* subsection, dependencies + Phosphor MIT attribution, layout table if it moved | ✅ (layout table unmoved — *Deviations* 2) |
| 4 | `dotnet list package --outdated` and `--vulnerable --include-transitive`, findings recorded | ✅ (no vulnerabilities; one deliberate SkiaSharp pin) |
| 5 | The merge / tag / publish runbook printed; no zips, no tag, no push | ✅ (printed to the console; nothing published) |
| 6 | Release build clean and suite green at 1.1.0 | ✅ (682, Debug and Release, zero warnings, zero `AVLN*`) |
| 7 | No stale `1.0.0` version string in the docs or the manifest | ✅ (grep; only historical references remain) |
| 8 | Both variants screenshotted one last time on the finished app | ✅ (from the published self-contained artifact) |

## Item-level acceptance (FEATURE-5CBC, all five phases)

| # | Criterion | Status |
|---|---|---|
| 1 | Desktop references `Enigma.Avalonia.Desktop` and uses its theme, `NavigationView`, `ContentDialog`, `Overlay`, `InfoBar`, picker service, `SettingsCard` and editors; no retired brush key, no `TabControl`, no `ViewLocator`, no `PageViewModel` | ✅ (PHASE01–04) |
| 2 | Core byte-for-byte unchanged; no ViewModel names an Avalonia or library type except `MainWindowViewModel`'s `INavigationService` | ✅ |
| 3 | Debug and Release clean — zero warnings, zero `AVLN*` — full suite green with the six protected test classes unmodified | ✅ (682) |
| 4 | Backup and recovery still work end to end, including cancellation, mixed recovery, rejection paths and the hash-mismatch refusal with its confirmed save | ✅ (PHASE02–03) |
| 5 | The theme preference persists, defaults to following the OS, and survives a missing, corrupt or future-versioned file | ✅ (PHASE04) |
| 6 | Every phase verified with the app running on Linux, screenshots of both variants in its completion doc | ✅ (five completion docs) |
| 7 | Release-ready at 1.1.0 — version, manifest, release notes and README consistent — zips, tag and GitHub release left to the user | ✅ (this phase) |
