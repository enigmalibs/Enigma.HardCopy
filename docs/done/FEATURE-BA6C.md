# FEATURE-BA6C — New icon set without the tile

**Item:** FEATURE-BA6C — New icon set without the tile
**Branch:** `feature/feature-ba6c-new-icon-set`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The desktop icon is now the new set from `Branding/Apps/final2/hardcopy` (`export.py final2`). It is the same QR
code in the *Soft* palette, **without the dark rounded tile**, so the background is transparent. Every copy is the
delivered file byte for byte:

- `Assets/app.ico`: the new ten-frame icon (16 to 256 px; 32-bit PNG frames). It is the executable's icon, the
  window's icon and the `app.ico` written beside the executable;
- `Assets/app-256.png`: the new 256 frame, shown by the About dialog;
- `packaging/icon/*.svg`: the two new masters, with their header comments as the export writes them;
- `packaging/linux/icons/`: the eight sizes `install.sh` installs.

**Android keeps its launcher artwork.** Its adaptive foreground was already this drawing. An adaptive icon's
background layer must be opaque, so the tile colour stays there, as the shape the launcher masks it to. Only the
comments changed: they now name the in-repository master and say why the tile remains on Android.

`AppIconTests` gained two facts: `app-256.png` and every Linux PNG must each be the `.ico`'s own frame of their size,
byte for byte. A half-done swap of the icon set now fails the suite.

## Files/modules touched

- Modified:
  - `src/Enigma.HardCopy.Desktop/Assets/app.ico`, `src/Enigma.HardCopy.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-hardcopy.svg`, `packaging/icon/enigma-hardcopy-small.svg`
  - `packaging/linux/icons/enigma-hardcopy-{16,24,32,48,64,96,128,256}.png`
  - `src/Enigma.HardCopy.Recover.Android/Resources/drawable/app_icon_foreground.xml` (comment)
  - `src/Enigma.HardCopy.Recover.Android/Resources/mipmap/app_icon.xml` (comment)
  - `src/Enigma.HardCopy.Recover.Android/Resources/values/colors.xml` (comment)
  - `tests/Enigma.HardCopy.Desktop.UnitTests/AppIconTests.cs`
  - `docs/roadmap.md`, `docs/plan/FEATURE-BA6C.md`: statuses.
- Created: `docs/done/FEATURE-BA6C.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Finding a frame in the test | An `IcoFrame(size)` helper that reads the directory entry's length and offset | The existing test already parses the directory; the PNG bytes are the frame itself |
| Prose about the tile | Nothing to change outside Android's comments | The README never describes the icon's look. The 1.3.0 release notes describe 1.3.0's icon, which is history |

## Deviations & follow-ups

- The source directory `Branding/Apps/final2/hardcopy` is outside this repository and was left alone.
- Line endings: the delivered SVGs are CRLF on disk. `.gitattributes` (`* text=auto eol=lf`) stores them as LF, so
  the committed SVGs differ from the source only in line endings. The binaries are byte for byte. Recommendation
  only: have `export.py` write LF.

## Build/test evidence

**The run's baseline**, taken once on `feature/2026-11-03-icons-msi-release` at `0d71ae8` (`develop` @ `f2254fa`
plus the planning commit), before this first dev. Later devs of the run cite it:

- `dotnet build Enigma.HardCopy.slnx -c Release`: succeeded, **0 warnings, 0 errors** (Android head included).
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **875 passed**, 0 failed, 0 skipped.

**This dev:**

- `dotnet build Enigma.HardCopy.slnx -c Release`: **0 warnings, 0 errors**. The Android head compiled its resources.
- `dotnet test --solution Enigma.HardCopy.slnx -c Release --no-build`: **877 passed** (875 + 2 new), 0 failed.
- `cmp` shows that `app.ico`, `app-256.png` and the eight Linux PNGs are identical to the branding files. The
  `app.ico` in the app's and the test project's Release output is the new one.
- Negative check: with the old 48 px PNG put back into `packaging/linux/icons/`,
  `EveryLinuxIcon_IsTheIcosFrameOfItsSize_ByteForByte` failed ("enigma-hardcopy-48.png is not the 48 px frame of
  app.ico."). The new file was restored afterwards and checked with `cmp`.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale: the README describes the icon's sizes and places, which did not change. The new look goes into the
  1.4.0 release notes (FEATURE-C1D1).
