# FEATURE-92E2 — Publish scripts for Windows and Linux

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-92e2-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

One-command publish scripts for the desktop app, part of the uniform Enigma-app baseline:

- Windows — `packaging/windows/publish.ps1` and `publish.bat`, both running
  `dotnet publish src/Enigma.HardCopy.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true`;
- Linux — `packaging/linux/publish.sh`, next to `install.sh` / `uninstall.sh`, running
  `dotnet publish src/Enigma.HardCopy.Desktop -c Release -r linux-x64 -p:PublishReadyToRun=true`.

## Context & constraints

- **Reference:** `C:\Dev\EnigmaLibs\Enigma.GitClient\packaging\windows\publish.ps1`, `publish.bat` and
  `packaging\linux\publish.sh` — resolve the project from the script's own directory, accept no arguments, pass
  `dotnet`'s exit code on (`exit $LASTEXITCODE`, `exit /b %ERRORLEVEL%`, `exec`).
- `scripts/publish.ps1` / `publish.sh` already exist with another purpose: the release zips (self-contained,
  single-file, every RID, into `artifacts/`). They stay as they are.
- `.gitattributes`: `*.bat` CRLF, `*.sh` LF; `.sh` committed `100755`.
- The Desktop csproj trims native `.pdb` files from every publish (`TrimNativeSymbolsFromPublish`).

## Steps

1. `packaging/windows/publish.ps1` — GitClient's, for `src/Enigma.HardCopy.Desktop` and "Enigma HardCopy".
2. `packaging/windows/publish.bat` — GitClient's, same substitutions; CRLF in the working tree.
3. `packaging/linux/publish.sh` — GitClient's, same substitutions; mode `100755`.
4. Tests (Desktop test project, file-based): each script names `src/Enigma.HardCopy.Desktop` (the assembly's
   directory), its RID, `-c Release` and `-p:PublishReadyToRun=true`.
5. README *Publishing*: the three scripts, where their output lands, and how they differ from `scripts/publish.*`.
6. Verify: run `publish.ps1` and `publish.bat` (win-x64) and `publish.sh` through Git Bash (linux-x64), from
   another directory; each exits 0 and leaves its `publish/` folder (with `app.ico`, FEATURE-C5C6).

## Acceptance criteria

- The three scripts run from any directory and exit with `dotnet`'s exit code.
- `publish.sh` is `100755` in the index; `publish.bat` is CRLF in the working tree.
- Solution builds with zero warnings; whole suite green.

## Out of scope

- Self-contained or single-file publishing, other RIDs, zips — `scripts/publish.*` and `install.sh` cover those.
- Changing or removing `scripts/publish.*`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where | `packaging/windows/`, `packaging/linux/` | GitClient's layout; `publish.sh` sits "in addition to install.sh and uninstall.sh" as the spec says | `scripts/` (would collide with the release-zip scripts' names) |
| The existing `scripts/publish.*` | Kept unchanged | They build the release zips, a different artifact the release runbook relies on | Replacing them with the spec's command |
| `publish.bat` | Calls `dotnet` itself | No dependency on PowerShell's execution policy | Wrapping `publish.ps1` |
| Arguments | None, as GitClient | The spec fixes the command | Passing `%*` / `"$@"` through |
| Self-contained | Not set (the spec's command verbatim) | One obvious behaviour; the runtime is needed, as the script header says | Adding `--self-contained` |
