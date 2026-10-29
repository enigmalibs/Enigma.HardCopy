# FEATURE-3D14 — Linux install and uninstall scripts

## Summary

The desktop app can now be installed on Linux for the current user, into the XDG directories, and removed again.
The scripts are Enigma.GitClient's `packaging/linux/install.sh` and `uninstall.sh`, with every GitClient-specific
part replaced.

`install.sh`:

- builds from the repository with the .NET runtime bundled, or `--framework-dependent`, or installs an
  already-published folder with `--from`; `--rid` picks the target;
- refuses root, and refuses empty or relative XDG paths;
- swaps a sibling staging folder in, so a failed build leaves the old install untouched;
- links `~/.local/bin/enigma-hardcopy`, installs the icon in eight hicolor sizes, and writes
  `enigma-hardcopy.desktop` from its template;
- pins `DOTNET_ROOT` when a framework-dependent build's runtime is not where a launcher looks.

`uninstall.sh` removes exactly that. It removes the symlink only when it points into the install, and it never
touches `~/.config/Enigma.HardCopy`.

The launcher entry is `Name=Enigma HardCopy`, `GenericName=Paper Backup`, `Categories=Utility;Archiving;`,
`StartupWMClass=Enigma.HardCopy.Desktop`.

## Files/modules touched

- Created: `packaging/linux/install.sh`, `packaging/linux/uninstall.sh` (mode `100755`)
- Created: `packaging/linux/enigma-hardcopy.desktop.in`
- Created: `packaging/linux/icons/enigma-hardcopy-{16,24,32,48,64,96,128,256}.png`
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/LinuxPackagingTests.cs`
- Modified: `README.md` (*Installing on Linux*, *Project layout*)
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-3D14.md`

## Deviations & follow-ups

- Dropped from GitClient's scripts:
  - the git checks and notes;
  - the `<repository>` argument hint;
  - the former-launcher-name case in `uninstall.sh`, which this app never had.
- The `--framework-dependent` help text gives this app's measured sizes, for linux-x64 Release: about 51 MB
  rather than 130 MB.
- Not verified on a real Linux desktop. Plan step 7 ran the scripts in Git Bash. A Linux check would look at the
  launcher entry and the icon grouping, and is worth a minute before the release.
- In Git Bash, `ln -s` copies, because Windows symlinks are off. `uninstall.sh` therefore left the bin entry
  alone, correctly: it is not a symlink. On Linux it is a real symlink and is removed.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `GenericName` | "Paper Backup" | What the app is, for a launcher that shows generic names. GitClient repeats its name there, which tells a reader nothing |
| Run without arguments | "Run it from a terminal with: enigma-hardcopy" | The app opens no file from its command line, so GitClient's `<repository>` hint has no equivalent |
| Executable bit | `git add --chmod=+x` | Within the allowed `add` operation, and identical to how `scripts/publish.sh` is stored |

## Build/test evidence

- `bash -n` passes on both scripts. Both are LF and `100755` in the index (`git ls-files -s` / `--eol`).
- `install.sh --help` and `uninstall.sh --help` print their usage.
- In Git Bash, with `XDG_DATA_HOME`, `XDG_BIN_HOME` and `XDG_CONFIG_HOME` pointed into the scratchpad:
  - `install.sh --from <linux-x64 framework-dependent publish>` exited 0 and wrote the application, the bin
    entry, 8 icons under `icons/hicolor/<N>x<N>/apps/` and the `.desktop` file with its `Exec` line;
  - `uninstall.sh` removed the application, the `.desktop` file and the 8 icons, and left `settings.json`;
  - a second `uninstall.sh` reported "Nothing to remove".

  The scratch tree was deleted afterwards.
- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **871 passed**, 0 failed. The 6 new
  `LinuxPackagingTests` check that:
  - both scripts name the Desktop assembly;
  - `install.sh` publishes `src/Enigma.HardCopy.Desktop`;
  - both scripts use one app id and one icon set;
  - every listed size is a PNG of that size, and the folder holds only those;
  - the entry has `Exec=@EXEC@`, its icon and its `StartupWMClass`.
- Doc sweep: README gained *Installing on Linux* and a `packaging/` row in *Project layout*.
