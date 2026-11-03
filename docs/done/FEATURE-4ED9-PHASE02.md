# FEATURE-4ED9-PHASE02 — The buildmsi script

**Item:** FEATURE-4ED9 — Windows MSI build script
**Branch:** `feature/feature-4ed9-phase02-buildmsi-script`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

`packaging/windows/buildmsi.ps1` builds the Windows installer in one command, from any directory. It is
Enigma.GitClient's script. Only the product's constants differ, plus one help line about the runtime:

1. It checks that `git`, `dotnet`, `wix` and the vendored worker are there, **before** anything is cleaned.
2. It reads the version from `Directory.Build.props` and checks it is `X.Y.Z`.
3. It runs `git -C <root> clean -dxf`.
4. It runs `publish.ps1`, the existing `-c Release -r win-x64 -p:PublishReadyToRun=true` publish, then checks that
   the exe and `app.ico` are in the publish folder.
5. It writes `artifacts/Enigma.HardCopy.X.Y.Z.msipkg.json`, an Enigma.Msi profile with:
   - absolute paths, and a new `productId` for each build;
   - upgrade code `4b3ca1c8-32bb-4c88-83fb-24c08dbc4f61`;
   - "Enigma HardCopy", "Josué Clément", `PerMachine`, `%ProgramFiles%\Enigma HardCopy`;
   - `compression` `None`;
   - `app.ico` as the Control Panel icon and as both shortcuts' icon;
   - shortcuts in `%Desktop%` and `%ProgramMenu%`.
6. It runs `Enigma.Msi.Worker.exe build` on that profile, which writes `artifacts/Enigma.HardCopy.X.Y.Z.msi`.

Every failure prints `buildmsi: <reason>` and exits non-zero, with the failing tool's exit code when there is one.
`buildmsi.bat` runs the script from `cmd` with `-ExecutionPolicy Bypass` and passes its exit code on.

The upgrade code is new: no HardCopy MSI existed before. From now on it is the product line's identity, so a later
MSI upgrades an earlier one instead of installing beside it. `WindowsInstallerTests` pins it, along with the product
name (= `ProductInformation.DisplayName`) and the assembly whose `.exe` the shortcuts start.

## Files/modules touched

- Created:
  - `packaging/windows/buildmsi.ps1`, `packaging/windows/buildmsi.bat`
  - `tests/Enigma.HardCopy.Desktop.UnitTests/WindowsInstallerTests.cs`
  - `docs/done/FEATURE-4ED9-PHASE02.md`
- Modified: `README.md`; `docs/roadmap.md`, `docs/plan/FEATURE-4ED9.md` (statuses).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Changes against GitClient's script | Only the four constants, the names in the help, and one help line saying the MSI does not install the .NET 10 runtime | The spec asks for "exactly like" GitClient. HardCopy's release zips are self-contained, so the runtime need is worth stating here |
| What the tests pin | `$UpgradeCode`, `$ProductName`, `$Assembly` and the `publish.ps1` call, as whole lines; the worker's path and presence; the `.bat`'s invocation and its CRLF | The script derives the folder, the shortcuts, the publish folder and the exe from these. A whole-line match catches a changed constant |
| Where the tests live | One `WindowsInstallerTests` class in the Desktop tests | HardCopy's packaging tests are all there (`PublishScriptsTests`, `LinuxPackagingTests`). Its Core has none |
| README placement | A *Building the Windows installer* section after *Publishing the release artifacts*, and the *Project layout* row | GitClient's section, placed next to the other release builds |

## Deviations & follow-ups

- **A 32-bit package.** Enigma.Msi has no platform setting, so the MSI's template is `Intel;1033`, and
  `%ProgramFiles%` is `Program Files (x86)` on 64-bit Windows. GitClient's MSI is the same. The app itself is
  x64 and runs from there.
- **The MSI carries the managed PDBs** (`Enigma.HardCopy.Core.pdb`, `Enigma.HardCopy.Desktop.pdb`). The publish
  keeps them on purpose (csproj, `TrimNativeSymbolsFromPublish`). The native ones are already dropped, so this MSI
  is 88 MB where GitClient's is 189 MB.
- **WixSharp's "source base directory"** is the caller's current directory: it printed this repository while
  building in the clone. It resolves relative paths only, and the profile's paths are absolute. The repository had
  no new file after the build (`git status --porcelain` showed only this dev's edits).
- **Not run, by design:** installing, launching or uninstalling the MSI.
- Line endings: `buildmsi.bat` is CRLF, as `.gitattributes` asks; the `.ps1` is ASCII with LF.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BA6C.md` (0 warnings; 875 passed).
- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **884 passed** (877 + 7 new), 0 failed.
  `WindowsInstallerTests` on its own: 7/7.
- `diff` against GitClient's `buildmsi.ps1`/`.bat`: only the constants, the names and the runtime line.
- **End to end, in a scratch clone** of this branch, with the dev's tree committed there (the run may not
  `git clean` this repository):
  - **No `wix` on the `PATH`.** The script printed `buildmsi: wix is not on the PATH: dotnet tool install --global
    wix.` and exited **1**. An untracked canary file was still there, so nothing had been cleaned.
  - **`buildmsi.bat`.** Git removed the canary file, the publish ran, and the worker built the MSI. It exited
    **0** after **19 s** of wall time. `artifacts/` held `Enigma.HardCopy.1.3.0.msi` (88,089,677 bytes,
    uncompressed) and its `.msipkg.json` (`"compression": "None"`). Afterwards `git status --porcelain` was empty.
  - **The MSI's tables**, read through the Windows Installer API:
    - `ProductName` Enigma HardCopy; `Manufacturer` Josué Clément (code points checked: two U+00E9);
      `ProductVersion` 1.3.0;
    - `UpgradeCode` `{4B3CA1C8-32BB-4C88-83FB-24C08DBC4F61}`, `ALLUSERS` 1, `ARPPRODUCTICON` `app_icon.ico`;
    - `INSTALLDIR` "Enigma HardCopy" under `ProgramFilesFolder`;
    - two "Enigma HardCopy.lnk" shortcuts, in `DesktopFolder` and `ProgramMenuFolder`, on
      `[INSTALLDIR]\Enigma.HardCopy.Desktop.exe`, with icon `IconFile1_app.ico`;
    - `Upgrade`: rows `VersionMax 1.3.0` (attributes 1) and `VersionMin 1.3.0` (attributes 2);
    - 84 files, the publish folder's 84; template `Intel;1033`.
- Fix budget: 0 cycles used.

## Documentation sweep

- `README.md` gains a new *Building the Windows installer* section, which covers:
  - what `buildmsi` does, with the `git clean -dxf` warning;
  - what the MSI installs;
  - the prerequisites;
  - the .NET 10 runtime the installed app needs.

  The *Project layout* row for `packaging/` now names the installer build and the vendored worker.
- The release notes and the *Install* table come with 1.4.0 (FEATURE-C1D1).
