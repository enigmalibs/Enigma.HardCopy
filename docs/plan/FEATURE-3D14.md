# FEATURE-3D14 — Linux install and uninstall scripts

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-3d14-linux-install`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Install the desktop app for the current user on Linux, into the XDG directories, with a launcher entry and the
icon — and remove it again — exactly as Enigma.GitClient's `packaging/linux/install.sh` and `uninstall.sh` do.

## Context & constraints

- **Reference** (`C:\Dev\EnigmaLibs\Enigma.GitClient\packaging\linux\`): `install.sh` (`--from DIR`,
  `--framework-dependent`, `--rid`, `-h`; refuses root and relative/empty XDG paths; builds into a sibling staging
  directory and swaps it in; symlink in `$XDG_BIN_HOME`; eight hicolor PNGs; the desktop entry from
  `<id>.desktop.in` with `@EXEC@` substituted in bash; `DOTNET_ROOT` pinned when a framework-dependent build's
  runtime is not where a launcher looks; desktop/icon caches refreshed), `uninstall.sh` (removes exactly what
  install wrote; the symlink only if it points into the install; never the user's settings),
  `enigma-git-client.desktop.in`, `icons/enigma-git-client-{16,24,32,48,64,96,128,256}.png`.
- HardCopy: executable `Enigma.HardCopy.Desktop`; settings in `~/.config/Enigma.HardCopy/settings.json`
  (`AppSettingsStore.DefaultFilePath`); no other user data. Branding PNGs in
  `C:\Dev\EnigmaLibs\Branding\Apps\final\hardcopy\png\` (FEATURE-1148 put the SVG masters in `packaging/icon/`).
- `.gitattributes` keeps `*.sh` LF; shell scripts are committed with mode `100755` (`scripts/publish.sh` is).
- No `shellcheck` and no Linux machine are guaranteed here; Git Bash is.

## Steps

1. `packaging/linux/install.sh` — GitClient's script with `APP_ID='enigma-hardcopy'`,
   `APP_EXE='Enigma.HardCopy.Desktop'`, the project `src/Enigma.HardCopy.Desktop`, the name "Enigma HardCopy",
   the settings path `~/.config/Enigma.HardCopy`; the git-specific notes dropped.
2. `packaging/linux/uninstall.sh` — GitClient's, same substitutions, without the former-launcher-name case.
3. `packaging/linux/enigma-hardcopy.desktop.in` — `Name=Enigma HardCopy`, a one-line `Comment`,
   `Categories=Utility;Archiving;`, keywords (paper, backup, QR code, recovery, print), `StartupWMClass` as the
   app reports it.
4. `packaging/linux/icons/enigma-hardcopy-{16,24,32,48,64,96,128,256}.png` from the branding PNGs.
5. Mode `100755` for both scripts in the index.
6. Tests (Desktop test project, file-based, no platform): both scripts name the same icon sizes, every listed size
   has its PNG and it is that size; the template's `Exec=@EXEC@` and `Icon=enigma-hardcopy`; `APP_EXE` is the
   Desktop assembly name.
7. Verify: `bash -n` on both scripts; in Git Bash, `install.sh --help` and `uninstall.sh --help`; an
   `install.sh --from <a linux-x64 publish>` run with `XDG_DATA_HOME` / `XDG_BIN_HOME` pointed into the
   scratchpad, then `uninstall.sh` with the same, checking what is written and removed (where Git Bash allows).
8. README: an *Installing on Linux* subsection under *Install*.

## Acceptance criteria

- `install.sh` installs into the XDG directories and `uninstall.sh` removes exactly that, never the settings.
- Both scripts are LF, executable in the index, and pass `bash -n`.
- Solution builds with zero warnings; whole suite green.

## Out of scope

- `publish.sh` (FEATURE-92E2); system-wide installs; distribution packages (`.deb`, `.rpm`, Flatpak, AppImage).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Layout | `packaging/linux/` with GitClient's file names | The uniform layout the spec points at | `scripts/linux/` |
| App id | `enigma-hardcopy` | The branding file names; GitClient's `enigma-<name>` pattern | `enigma-hard-copy` |
| Default build | Self-contained, as GitClient | A user install that needs no runtime; matches this app's release zips | Framework-dependent by default |
| Desktop categories | `Utility;Archiving;` | A backup-to-paper tool; both are registered freedesktop categories | `Office;` |
| Tests | File-level consistency tests in the Desktop test project + a scratch run | The packaging files are tied to the assembly name and the icon set; a run is the real proof | No test |
