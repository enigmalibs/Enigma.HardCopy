# FEATURE-79FF-PHASE06 — Release readiness

**Branch:** `feature/feature-79ff-phase06-release`
**Status:** DONE — and with it, FEATURE-79FF as a whole.

## Summary

v1.0.0 is shippable from a clean checkout. The phase added the packaging, the version, the user
documentation and the release notes, and — more usefully — it *proved* the packaging rather than
assuming it.

- **Publish scripts.** `scripts/publish.sh` and `scripts/publish.ps1` each produce every artifact, so a
  release can be cut from Linux or from Windows. Each RID is published self-contained and single-file
  into `artifacts/publish/`, then zipped to `artifacts/Enigma.HardCopy-<version>-<rid>.zip` with the
  README and the licence alongside the binary. The version is read out of `Directory.Build.props`, so
  the artifact names cannot drift from what the executable reports about itself.
- **A packaging bug found and fixed.** The first win-x64 publish weighed 225 MB, because SkiaSharp and
  HarfBuzz ship **native** symbol files in their runtime assets — `libSkiaSharp.pdb` at 84 MB and
  `libHarfBuzzSharp.pdb` at 21 MB. A `TrimNativeSymbolsFromPublish` target in the Desktop csproj drops
  every native `.pdb` from the publish set. That is 105 MB of nothing, and the fix lives in the project
  rather than in the scripts so *any* publish is clean, however it was invoked. The managed `.pdb`
  files are deliberately kept: ~60 KB, and they are what turns a user's crash report into a line number.
- **Version 1.0.0** solution-wide, from `Directory.Build.props`.
- **README** is now the user guide the plan asked for: why paper, install, a step-by-step backup
  walkthrough, both recovery walkthroughs, the app-less recovery route, build and publish instructions,
  project layout, and an explicit limitations section.
- **`RELEASENOTES.md`** written from the house first-release template (the file existed but was empty).

## Files/modules touched

### Created

| File | What it is |
|---|---|
| `scripts/publish.sh` | Bash release build — publishes each RID and zips it. Executable bit set. |
| `scripts/publish.ps1` | The PowerShell twin, producing identical artifacts. |
| `docs/done/FEATURE-79FF-PHASE06.md` | This record. |

### Modified

| File | Change |
|---|---|
| `Directory.Build.props` | `<Version>1.0.0</Version>`, with a note that `app.manifest`'s `assemblyIdentity` version is bumped alongside it. |
| `src/…/Enigma.HardCopy.Desktop.csproj` | The `TrimNativeSymbolsFromPublish` target. |
| `README.md` | Rewritten as the v1 user guide (was a stub with a Build and a Run section). |
| `RELEASENOTES.md` | The v1.0.0 first-release section (the file was empty). |
| `docs/roadmap.md`, `docs/plan/FEATURE-79FF.md` | PHASE06 → DONE, and FEATURE-79FF → DONE with it. |

Plan step 2 — the app icon — needed **no work**: PHASE05 had already wired `Assets/app.ico`,
`<ApplicationIcon>` and the window `Icon`. This phase verified it survives publishing (below) instead
of rebuilding it.

## Verification of the artifacts

Both artifacts were built by `scripts/publish.sh` and then checked as a user would meet them.

| Check | win-x64 | linux-x64 |
|---|---|---|
| Archive size | 55 MB | 52 MB |
| Expands into its own directory | yes | yes |
| Executable bit survives the zip | n/a | yes |
| README + LICENSE alongside the binary | yes | yes |
| Native symbol files excluded | yes (was 225 MB before the fix) | yes |
| All four icon sizes embedded in the executable | yes — 16/32/48/256, byte-matched against `Assets/app.ico` | n/a (Linux takes its icon from the window at runtime) |
| Version resource carries `1.0.0` and the author | yes | n/a |
| Starts from a clean unzip | **not run — see below** | yes |
| Full paper round trip in this packaging mode | **not run — see below** | yes |

**The round trip really was run, inside the bundle.** The unit suite could not answer the one question
that matters here — does the pipeline still work once it is a self-extracting single file — because the
xUnit v3 runner cannot locate itself inside such a bundle (`error: assembly not found`), so the test
projects cannot be published that way at all. Instead a throwaway harness in the session scratchpad
(never committed) ran the real Core pipeline — `BackupEncoder` → `PdfComposer` → `QrRenderer` →
`ImageDecoder` → `RecoverySession` → SHA-256 — from inside a self-contained single-file linux-x64
bundle, across all three chunk sizes with both a compressible and an incompressible fixture:

```
Round-trip harness — single file: True
  OK  Small  random        40000 B   80 codes  PDF 3136865 B  scan-failures 0  outcome Verified
  OK  Small  compressible  40076 B    2 codes  PDF   78078 B  scan-failures 0  outcome Verified
  OK  Medium random        40000 B   41 codes  PDF 2958279 B  scan-failures 0  outcome Verified
  OK  Medium compressible  40076 B    2 codes  PDF   77853 B  scan-failures 0  outcome Verified
  OK  Large  random        40000 B   28 codes  PDF 2755538 B  scan-failures 0  outcome Verified
  OK  Large  compressible  40076 B    2 codes  PDF   77115 B  scan-failures 0  outcome Verified
ALL ROUND TRIPS VERIFIED
```

That is the meaningful part: QuestPDF's bundled Lato fonts, Skia and ZXing all resolve from the
self-extracted bundle. A PDF is composed and every symbol it would print is rendered, read back as an
image and reassembled into the original bytes with a matching hash.

## Deviations & follow-ups

### Deviations from the plan

1. **Plan step 4 says "final full-suite run on both publish targets".** The suite was run in Release on
   linux-x64 only — running a win-x64 test binary needs Windows, and the same limit that blocks the
   Windows smoke test blocks this. Since both projects target a single `net10.0` TFM and the RID
   affects only the native asset set, the platform-sensitive part is the Skia/QuestPDF native path,
   which the harness above covers for Linux and which remains owed for Windows.
2. **No new unit tests.** PHASE06's acceptance criteria are the zero-warning build, the green suite and
   a working artifact — packaging and prose, with no new production logic to test. The suite is
   unchanged at 587. Stated rather than left to inference.
3. **No MSI profile, no NuGet packaging.** The house `dotnet-release` skill offers an MSI profile when
   releasing an app, but the interview's validated decision is explicit: *"no installer, no CI"*, and
   nothing here is published to NuGet. Skipped deliberately, not overlooked. `SECURITY.md` was likewise
   skipped — the skill offers it for publicly published packages, and this repository has no remote.

### Follow-ups

- **The Windows artifact has never been run.** Carried forward from PHASE05 and now sharpened: the
  `win-x64` zip is built, its icon and version resource are verified byte-for-byte inside the
  executable, but no one has double-clicked it. This is the one outstanding item against overall
  acceptance criterion 6 and PHASE06's own criterion 5. Everything cross-platform-sensitive (Skia,
  QuestPDF fonts, single-file self-extraction) is verified on Linux and is the same code path on
  Windows, but that is an argument, not a test.
- **Final icon art is still a placeholder**, as the plan anticipated. Replacing `Assets/app.ico`
  requires no code change — the wiring is verified.
- **`AssemblyInformationalVersion` is `1.0.0+<commit sha>`**, the .NET SDK default. Harmless and
  arguably useful provenance for a tool that produces long-lived artifacts; noted in case a bare
  `1.0.0` is ever preferred (`<IncludeSourceRevisionInInformationalVersion>false</…>`).
- **The version lives in two files** — `Directory.Build.props` and `app.manifest`'s `assemblyIdentity`
  (Windows requires the four-part form there). A comment in the props file says to bump both; nothing
  enforces it.
- **The README's format summary was cross-checked by hand** against `RecoveryInstructions` and
  `HardCopyFormat` — there is no test tying the two together, so an edit to the format spec could
  leave the README behind.
- **No CRLF / line-ending issues were observed** in the files this dev touched; nothing to recommend.

## Build/test evidence

```
dotnet build Enigma.HardCopy.slnx -c Release   → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --solution Enigma.HardCopy.slnx -c Release
  Enigma.HardCopy.Core.UnitTests     passed (4s 887ms)
  Enigma.HardCopy.Desktop.UnitTests  passed (828ms)
  total: 587   failed: 0   succeeded: 587   skipped: 0

scripts/publish.sh
  artifacts/Enigma.HardCopy-1.0.0-win-x64.zip     (55M)
  artifacts/Enigma.HardCopy-1.0.0-linux-x64.zip   (52M)
```

Plus, on the artifacts themselves: the linux-x64 zip unpacked into a clean directory and its executable
started there (host up, window created, no error on stdout or stderr), and the single-file round-trip
harness above returned exit code 0.

## Release runbook (printed, not run)

This repository has **no remote and no tags**, so publishing reduces to a local tag. The house tag
convention defaults to a bare `X.Y.Z`:

```bash
# 1. Pre-flight
dotnet build Enigma.HardCopy.slnx -c Release
dotnet test  --solution Enigma.HardCopy.slnx -c Release

# 2. Merge this branch into the default branch, then:
git tag 1.0.0

# 3. Build the artifacts
scripts/publish.sh          # or scripts/publish.ps1 on Windows
```

No `dotnet pack` and no `dotnet nuget push`: this is an application, not a package. Add
`git push origin 1.0.0` once the repository has a remote.
