# FEATURE-1148 — New application icons

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-1148-new-app-icons`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Both applications carry the new Enigma.HardCopy icon from `C:\Dev\EnigmaLibs\Branding\Apps\final\hardcopy` —
a QR code in the Soft palette (`#6A9BF4` modules, a `#63C77D` corner module) on the `#1E1F22` rounded tile.

## Context & constraints

- Branding set: `enigma-hardcopy.ico` (multi-frame), `enigma-hardcopy.svg` (master, 40 px and up),
  `enigma-hardcopy-small.svg` (16–32 px), `png/enigma-hardcopy-{16,20,24,32,40,48,64,96,128,256}.png`.
- Desktop: `src/Enigma.HardCopy.Desktop/Assets/app.ico` is the `ApplicationIcon` and `MainWindow`'s `Icon`;
  `Assets/**` is an `AvaloniaResource`. There is no PNG asset yet.
- Android (Recover): an adaptive icon — `mipmap-anydpi-v26/app_icon.xml` (background colour
  `@color/app_icon_background` = `#3574F0`, foreground and monochrome `drawable/app_icon_foreground.xml`, a white
  QR glyph scaled into the 66-unit safe zone) and `mipmap/app_icon.xml` (pre-API-26 round badge). Vector
  drawables only — no PNG mipmaps.
- Enigma.GitClient's FEATURE-F7B5 is the reference: `Assets/app.ico` + `Assets/app-256.png` (the 256 frame),
  the SVG masters kept under `packaging/icon/`.

## Steps

1. `Assets/app.ico` ← `enigma-hardcopy.ico`; new `Assets/app-256.png` ← `png/enigma-hardcopy-256.png` (the About
   dialog's image, FEATURE-7B95).
2. `packaging/icon/enigma-hardcopy.svg` and `enigma-hardcopy-small.svg`: the masters, verbatim.
3. Android: `app_icon_background` ← `#1E1F22`; `app_icon_foreground.xml` redrawn from `enigma-hardcopy.svg` —
   the modules' rounded rectangles as path data, in their two colours, scaled into the safe zone; the monochrome
   layer a single-colour copy (a separate `app_icon_monochrome.xml`) so themed icons keep working;
   `mipmap/app_icon.xml` the same artwork on its round badge.
4. Desktop test: `Assets/app.ico` is a valid ICO whose frames include 16, 32, 48 and 256 px; `app-256.png` is a
   256 × 256 PNG (read headers only — no platform needed).
5. Verify: the Android head builds (resources compile); the Desktop app is started once to see the window icon.

## Acceptance criteria

- `Assets/app.ico` is byte-for-byte the branding `.ico`; `app-256.png` byte-for-byte the 256 PNG.
- The Android head builds with zero warnings; its adaptive icon shows the new artwork's colours.
- Solution builds with zero warnings; whole suite green.

## Out of scope

- Linux hicolor PNGs (FEATURE-3D14 installs them); copying the icon to the output directory (FEATURE-C5C6).
- The QR glyph used on the navigation rail (a Phosphor icon, not the app icon).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which apps | Desktop and the Android Recover app | "App icons", plural, and one product brand; the Android launcher is the icon users see most | Desktop only |
| Android format | Vector drawables redrawn from the SVG | The head uses vectors only; one artwork for every density; matches the existing resources | PNG mipmaps per density (10 files, blurrier) |
| Android background | `#1E1F22`, the tile colour | The artwork's own background; the old `#3574F0` was the previous glyph's | Keeping the accent background |
| Monochrome layer | A separate single-colour drawable | A two-colour foreground would lose the corner module in a themed (tinted) icon | Reusing the coloured foreground |
| `app-256.png` | Added now | The About dialog shows it (GitClient: the PNG, because which ICO frame Skia picks is unspecified) | Adding it in FEATURE-7B95 |
| SVG masters | Kept under `packaging/icon/` | GitClient's layout; the source of every frame stays with the repo | Not keeping them |
