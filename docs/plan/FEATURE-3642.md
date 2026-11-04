# FEATURE-3642 — Icon set that fills the frame

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-3642-fill-frame-icons`
**Run:** feature/2026-11-04-icons-release

## Objective

Replace the application icon with the new set in `C:\Dev\EnigmaLibs\Branding\Apps\final3\hardcopy` (`.ico`, ten
PNG frames, two SVG masters). Every place that shows it must get the new one: the executable, the taskbar and the
window; the About dialog; the Windows installer's shortcuts and *Installed apps* entry; the Linux launcher; and the
SVG masters.

## Context & constraints

- **The new artwork** is FEATURE-BA6C's tile-free drawing (a QR code in the *Soft* palette: `#6A9BF4` modules, a
  `#63C77D` corner module, transparent background), **scaled to fill the frame**:
  - `enigma-hardcopy.svg` wraps final2's drawing in `scale(1.6471)`, so its longest side spans 16 to 240 of the
    256-unit view box instead of 60 to 196;
  - `enigma-hardcopy-small.svg`, which the 16 to 32 px frames come from, is redrawn on that live area with heavier
    marks.

  `Branding/Apps/export.py final3` generates both, and their header comments name it.
- **The new `.ico`** holds the same ten 32-bit PNG frames as today (16, 20, 24, 32, 40, 48, 64, 96, 128, 256). Each
  frame is byte for byte the matching `png/enigma-hardcopy-N.png` (checked while planning).
- **Where the icon lives.** Every copy is byte-identical to the `final2/hardcopy` set today (checked while
  planning):
  - `src/Enigma.HardCopy.Desktop/Assets/app.ico`. It is the `ApplicationIcon`, `MainWindow`'s `Icon`, and the
    `app.ico` copied beside the executable (FEATURE-C5C6). `buildmsi.ps1` uses that copy for the MSI's shortcuts
    and its *Installed apps* entry;
  - `src/Enigma.HardCopy.Desktop/Assets/app-256.png`: the About dialog's image (FEATURE-7B95);
  - `packaging/icon/enigma-hardcopy.svg`, `enigma-hardcopy-small.svg`: the masters;
  - `packaging/linux/icons/enigma-hardcopy-{16,24,32,48,64,96,128,256}.png`: what `install.sh` installs.
- **Android (HardCopy Recover)** draws its launcher icon as vector drawables in the masters' original
  coordinates:
  - the adaptive foreground scales the 136-unit code to 42 of 108 units, inside the 66-unit safe zone;
  - the pre-API-26 icon draws it on the tile.

  A drawing that fills the frame does not apply here. The launcher masks the icon to its own shape, and the safe
  zone caps the code at about 46 units.
- `AppIconTests` pins the `.ico` frames and the PNG's size. Since FEATURE-BA6C it also requires `app-256.png` and
  every Linux PNG to be the `.ico`'s own frame, byte for byte. `LinuxPackagingTests` pins the Linux PNGs' sizes.

## Steps

1. Copy, byte for byte:
   - `enigma-hardcopy.ico` → `src/Enigma.HardCopy.Desktop/Assets/app.ico`;
   - `png/enigma-hardcopy-256.png` → `src/Enigma.HardCopy.Desktop/Assets/app-256.png`;
   - the two SVGs → `packaging/icon/`;
   - the 16, 24, 32, 48, 64, 96, 128 and 256 PNGs → `packaging/linux/icons/`.
2. Android: the artwork stays. Comments that describe the master's coordinates as the code's place in its 256-unit
   view box (`app_icon_foreground.xml`, `app_icon_monochrome.xml`, `mipmap/app_icon.xml`) now say that the
   drawables use the master's untransformed drawing, and why the Android icon does not fill the frame.
3. Check that no prose describes the desktop icon's size or margin as it was (README, comments). Fix any that does.
4. Release build, whole suite, `cmp` of every destination, and the `app.ico` in the Release output.

## Acceptance criteria

- The four destinations hold the new files byte for byte (`cmp`), and none of the old files is left.
- `AppIconTests` and `LinuxPackagingTests` pass unchanged.
- The Android head builds, and its resources are unchanged except for comments.
- The Release build has zero warnings beyond the run's baseline, and the whole suite shows no new failure.

## Out of scope

- The Android launcher artwork's size, colours or shape.
- Release notes and version (FEATURE-DA65).
- The sizes the Linux installer installs (still eight).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which files | All four destinations, from the delivered set unchanged | "Update app's icons (taskbar/window, about, installers…)". The set has today's shape, and the MSI takes the published `app.ico` | Re-packing the `.ico` |
| Installers | No change to the scripts | `buildmsi.ps1` packages the published `app.ico`, and `install.sh` copies `packaging/linux/icons/`. Both pick up the new files | Editing the scripts |
| The SVGs' header comments | Copied verbatim | `export.py` writes them, and an edit would be lost on the next export | Editing them |
| Android launcher | Artwork kept; comments corrected | The adaptive safe zone caps the code near its current size, and the launcher's mask is the tile. A fill-the-frame drawing would be clipped | Scaling the foreground up (clipped by circular masks) |
| Tests | No new test | FEATURE-BA6C's byte-identity facts already fail a half-done swap | A test pinning the new artwork's hash (it would fail on every legitimate redraw) |
| The source directory | Left in place | It is the Branding repository's output, outside this repository | Deleting it |
