# FEATURE-4ED9-PHASE01 — Vendor the Enigma.Msi worker CLI

**Item:** FEATURE-4ED9 — Windows MSI build script
**Branch:** `feature/feature-4ed9-phase01-vendor-msi-worker`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The repository now carries Enigma.Msi's headless worker CLI in `packaging/windows/Enigma.Msi.Worker/`. PHASE02's
`buildmsi.ps1` will run it as `Enigma.Msi.Worker.exe build <profile>`. It is Enigma.Msi `1.7.0` (`a7a1bfb`), built
against WixSharp_wix4 2.14.2: the 20 files of its `bin/Release/net472/` output (3.4 MB), without the PDBs, the API
XML and `.wixsharp/`. These are the same 20 files Enigma.GitClient vendors (FEATURE-EAB7).

- The folder's `README.md` covers:
  - where the files come from;
  - the CLI's exit codes;
  - the requirements: .NET Framework 4.7.2 and the WiX CLI, with WiX 7's EULA;
  - the components' licences: Enigma.Msi, WixSharp and .NET are MIT, and the WiX 4.0.1 assemblies are MS-RL;
  - how to refresh it.
- `.gitignore` ignores `packaging/windows/Enigma.Msi.Worker/.wixsharp/`. WixSharp writes that folder beside the
  worker on its first build. `git clean -dxf` removes it, and the next build writes it again.

## Files/modules touched

- Created:
  - `packaging/windows/Enigma.Msi.Worker/`: 20 binaries (`Enigma.Msi.Worker.exe`, `Enigma.Msi.Worker.exe.config`,
    `Enigma.Msi.dll`, the `WixSharp*`, `WixToolset.*`, `System.*`, `Microsoft.Bcl.AsyncInterfaces.dll` and
    `mbanative.dll`)
  - `packaging/windows/Enigma.Msi.Worker/README.md`
  - `docs/done/FEATURE-4ED9-PHASE01.md`
- Modified: `.gitignore`; `docs/roadmap.md`, `docs/plan/FEATURE-4ED9.md` (statuses).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which build to copy | Enigma.Msi's existing `bin/Release/net472/` output | Its `Enigma.Msi.dll` carries `1.0.0+a7a1bfb…`, the `1.7.0` tag's commit, and the Enigma.Msi tree is clean. Rebuilding it would have meant writing into another repository |
| README | GitClient's, with this repository's MSI name, plus one line saying GitClient vendors the same files | The spec asks for this to be done "exactly like" GitClient. Two copies that match are easier to refresh together |

## Deviations & follow-ups

- `Enigma.Msi.Worker.exe.config` will differ from its source only in line endings once committed. The build writes
  CRLF, and `.gitattributes` (`* text=auto eol=lf`) stores LF. .NET Framework reads either. Every `.exe`/`.dll` is
  byte for byte, because `.gitattributes` marks them binary.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BA6C.md` (0 warnings; 875 passed).
- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **877 passed**, 0 failed. The count is
  unchanged since FEATURE-BA6C: this phase adds no test.
- `cmp` of each copied file against its source: all 20 identical before staging.
- `git status --porcelain --ignored -uall packaging/windows/`: the 21 files (20 binaries and the README) are
  untracked, and none is ignored. `git check-ignore -v` matches `.wixsharp/…` to the new rule (`.gitignore:374`).
- `Enigma.Msi.Worker.exe` with no arguments printed "No arguments were given." and the two usage forms, then exited
  with code **2**.
- Version resources: `Enigma.Msi.dll` reports ProductVersion `1.0.0+a7a1bfb0cf…`, `WixSharp.dll` reports
  `2.14.2.0`, and `WixToolset.Dtf.WindowsInstaller.dll` reports `4.0.1.0`. These match the README.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale yet. The README and its *Project layout* row change with the script itself, in PHASE02.
