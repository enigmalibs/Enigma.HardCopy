# FEATURE-3642 — Icon set that fills the frame

**Item:** FEATURE-3642 — Icon set that fills the frame
**Branch:** `feature/feature-3642-fill-frame-icons`
**Run:** feature/2026-11-04-icons-release

## Summary

The desktop icon is now the new set from `Branding/Apps/final3/hardcopy` (`export.py final3`). It is FEATURE-BA6C's
tile-free QR code, scaled to fill its frame: the code spans 16 to 240 of 256 units instead of 60 to 196. The 16 to
32 px frames come from a new small master with heavier marks. Every copy is the delivered file, byte for byte:

- `Assets/app.ico`: the new ten-frame icon (16 to 256 px, 32-bit PNG frames). It is used as:
  - the executable's icon;
  - the window and taskbar icon;
  - the `app.ico` written beside the executable, which `buildmsi.ps1` uses for the MSI's shortcuts and its
    *Installed apps* entry.
- `Assets/app-256.png`: the new 256 frame, shown by the About dialog.
- `packaging/icon/*.svg`: the two new masters, with the header comments the export writes.
- `packaging/linux/icons/`: the eight sizes `install.sh` installs.

The installer scripts needed no change: both take the files above.

**Android keeps its launcher artwork.** A drawing that fills the frame would be clipped by the launcher's mask,
because the adaptive safe zone caps the code near its current 42 units. Only two comments changed. They described
the drawables as drawn "in the SVG's 256-unit space". They now say the drawables use the master's own coordinates,
before the transform that scales the code to fill the desktop icon's frame, and why Android does not fill it.

## Files/modules touched

- Modified:
  - `src/Enigma.HardCopy.Desktop/Assets/app.ico`, `src/Enigma.HardCopy.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-hardcopy.svg`, `packaging/icon/enigma-hardcopy-small.svg`
  - `packaging/linux/icons/enigma-hardcopy-{16,24,32,48,64,96,128,256}.png`
  - `src/Enigma.HardCopy.Recover.Android/Resources/drawable/app_icon_foreground.xml` (comment)
  - `src/Enigma.HardCopy.Recover.Android/Resources/mipmap/app_icon.xml` (comment)
  - `docs/roadmap.md`, `docs/plan/FEATURE-3642.md`: statuses.
- Created: `docs/done/FEATURE-3642.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `app_icon_monochrome.xml`'s comment | Unchanged | It says "app_icon_foreground in one colour" and makes no claim about coordinates |
| `values/colors.xml`'s comment | Unchanged | "The desktop icon has no tile" is still true |

## Deviations & follow-ups

- The plan listed `app_icon_monochrome.xml` among the comments to correct. It holds no claim the new master makes
  wrong, so it was left alone.
- The source directory `Branding/Apps/final3/hardcopy` is outside this repository and was left alone.
- Line endings: the delivered SVGs are CRLF on disk, as final2's were. `.gitattributes` (`* text=auto eol=lf`)
  stores them as LF, so the committed SVGs differ from the source only in line endings. The binaries match byte
  for byte. Recommendation only: have `export.py` write LF.

## Build/test evidence

**The run's baseline** was taken once on `feature/2026-11-04-icons-release` at `0cc724a` (`develop` @ `6b023bd`
plus the planning commit), before this first dev. Later devs of the run cite it:

- `dotnet build Enigma.HardCopy.slnx -c Release`: succeeded, **0 warnings, 0 errors** (Android head included).
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **884 passed**, 0 failed, 0 skipped.

**This dev:**

- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**. The Android head compiled its
  resources.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **884 passed**, 0 failed. This includes
  `AppIconTests`, which requires `app-256.png` and every Linux PNG to be the new `.ico`'s own frame, byte for byte.
- `cmp`: `app.ico`, `app-256.png` and the eight Linux PNGs are identical to the branding files. The SVGs are
  identical apart from line endings.
- The `app.ico` in the app's Release output and in the test project's Release output is the new one.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale. The README describes the icon's places and sizes, not its proportions, and neither changed. The
  new look goes into the 1.5.0 release notes (FEATURE-DA65).
