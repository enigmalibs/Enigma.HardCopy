# FEATURE-C5C6 — App icon in the output directory

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-c5c6-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The desktop application's icon is written to the build output directory — and so to the publish directory —
next to `Enigma.HardCopy.Desktop(.exe)`, as `app.ico`, so shortcuts, launchers and installers can point at a
file instead of extracting it from the executable. Same as Enigma.GitClient's FEATURE-5096.

## Context & constraints

- `src/Enigma.HardCopy.Desktop/Assets/app.ico` (new artwork from FEATURE-1148) is the `ApplicationIcon` and the
  window's `Icon`; `Assets/**` is an `AvaloniaResource`, so nothing lands in `bin/` today.
- Items with `CopyToOutputDirectory` are also published, and flow to referencing projects' output (the Desktop
  test project), which is where a test can see them.
- `scripts/publish.*` (release zips) publish single-file: a `CopyToOutputDirectory` item is placed beside the
  single-file executable, not inside it — the icon then ships in the zip too.

## Steps

1. `Enigma.HardCopy.Desktop.csproj`: `<None Update="Assets/app.ico" CopyToOutputDirectory="PreserveNewest"
   TargetPath="app.ico" />`, with a one-line comment saying why. It stays an `AvaloniaResource`.
2. Desktop test: `app.ico` sits in `AppContext.BaseDirectory`, byte-for-byte the source asset.
3. Verify: a Release build puts `app.ico` in `bin/Release/net10.0/`; a `win-x64` publish puts it in `publish/`.

## Acceptance criteria

- `bin/<Configuration>/net10.0/app.ico` and the publish folder's `app.ico` exist, byte-for-byte `Assets/app.ico`.
- The embedded resource still resolves (`MainWindow`'s `Icon="/Assets/app.ico"` unchanged).
- Solution builds with zero warnings; whole suite green.

## Out of scope

- The PNG (`app-256.png`) and the Linux hicolor icons (installed by FEATURE-3D14's `install.sh`).
- The Android app (an APK has no output directory a launcher reads).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which file | `app.ico` only | "The app icon" is the `ApplicationIcon`; GitClient's choice | Also the PNG |
| Name and place | `app.ico` at the output root | One predictable name across every Enigma app | `Assets/app.ico`; `Enigma.HardCopy.Desktop.ico` |
| Mechanism | `None` + `CopyToOutputDirectory` + `TargetPath` | Standard MSBuild; flows to publish with no custom target | A custom `Copy` target |
| Test | A Desktop test on the copy in the test output | Pins the behaviour where it can be observed without a platform | Manual check only |
