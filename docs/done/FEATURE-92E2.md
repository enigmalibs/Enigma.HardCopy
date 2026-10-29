# FEATURE-92E2 — Publish scripts for Windows and Linux

## Summary

Three one-command publish scripts for the desktop app, ported from Enigma.GitClient with the names substituted.
Each resolves the project from its own location, accepts no arguments, and passes `dotnet`'s exit code on.

| Script | Runs | Exit code |
|---|---|---|
| `packaging/windows/publish.ps1` | `dotnet publish src\Enigma.HardCopy.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true` | `exit $LASTEXITCODE` |
| `packaging/windows/publish.bat` | the same command, from a cmd prompt, with no PowerShell execution policy involved | `exit /b %ERRORLEVEL%` |
| `packaging/linux/publish.sh` | `dotnet publish src/Enigma.HardCopy.Desktop -c Release -r linux-x64 -p:PublishReadyToRun=true` | `exec` |

The release-zip scripts in `scripts/publish.*` are unchanged. They are self-contained, single-file, for every RID,
and the release runbook relies on them.

## Files/modules touched

- Created: `packaging/windows/publish.ps1`, `packaging/windows/publish.bat` (CRLF)
- Created: `packaging/linux/publish.sh` (mode `100755`)
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/PublishScriptsTests.cs`
- Modified: `README.md` (*Publishing the release artifacts*, *Project layout*)
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-92E2.md`

## Deviations & follow-ups

- None. Running the Windows and the Linux script back to back builds the same `bin/Release` project for two RIDs.
  Each RID publishes into its own `<rid>/publish/` folder, so neither overwrites the other.

## Decisions taken at build time

None beyond the plan.

## Build/test evidence

- All three scripts were run from the session scratchpad, not the repository:
  - `bash packaging/linux/publish.sh` exited 0 and wrote `…/net10.0/linux-x64/publish/`;
  - `powershell -ExecutionPolicy Bypass -File packaging\windows\publish.ps1` exited 0;
  - `cmd /c packaging\windows\publish.bat` exited 0.

  Both Windows scripts wrote `…/net10.0/win-x64/publish/`. Both publish folders hold `Enigma.HardCopy.Desktop(.exe)`
  and `app.ico` (FEATURE-C5C6).
- `git ls-files -s` shows `publish.sh` at `100755`. `git ls-files --eol` shows `publish.bat` as `w/crlf`.
- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **875 passed**, 0 failed. The 4 new tests are 3
  cases checking that each script publishes `src/Enigma.HardCopy.Desktop` with `-c Release -r <rid>
  -p:PublishReadyToRun=true`, and 1 checking the batch file's CRLF endings.
- Doc sweep: README *Publishing* names the three scripts, where their output lands and how they differ from the
  release scripts. The *Project layout* `packaging/` row lists them.
