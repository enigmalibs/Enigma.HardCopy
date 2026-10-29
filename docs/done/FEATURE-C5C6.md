# FEATURE-C5C6 — App icon in the output directory

## Summary

The desktop app's icon is now also a file named `app.ico` beside `Enigma.HardCopy.Desktop(.exe)`, in the build
output and in every publish folder. Shortcuts, launchers and installers can point at it instead of pulling it
out of the executable. It is the same mechanism as Enigma.GitClient's FEATURE-5096. The icon also stays an
`AvaloniaResource`, so `MainWindow`'s `Icon="/Assets/app.ico"` is unchanged.

## Files/modules touched

- Modified: `src/Enigma.HardCopy.Desktop/Enigma.HardCopy.Desktop.csproj`
- Modified: `tests/Enigma.HardCopy.Desktop.UnitTests/AppIconTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-C5C6.md`

## Deviations & follow-ups

- None. The release zips of `scripts/publish.*` publish single-file. A `CopyToOutputDirectory` item is not
  bundled into the executable, so `app.ico` now ships beside it in the zip, along with README and LICENSE.

## Decisions taken at build time

None beyond the plan.

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **799 passed**, 0 failed. The new test is
  `AppIco_IsCopiedToTheOutputDirectory_ByteForByte`.
- `dotnet build src/Enigma.HardCopy.Desktop -c Release` gave 0 warnings, and `cmp` shows that
  `bin/Release/net10.0/app.ico` is identical to `Assets/app.ico`.
- `dotnet publish src/Enigma.HardCopy.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true` put `app.ico`
  beside `Enigma.HardCopy.Desktop.exe` in `bin/Release/net10.0/win-x64/publish/`, byte-identical.
- Doc sweep: nothing describes the output folder's contents file by file. No edit.
