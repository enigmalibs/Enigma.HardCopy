# FEATURE-1148 — New application icons

## Summary

Both apps now use the new Enigma.HardCopy icon from `Branding/Apps/final/hardcopy`. It is a QR code in the Soft
palette (`#6A9BF4` modules and a `#63C77D` corner module) on the `#1E1F22` rounded tile.

- **Desktop:** `Assets/app.ico` is the branding `.ico`, with ten PNG frames from 16 to 256 px. A new
  `Assets/app-256.png` holds its 256 px frame, for the About dialog (FEATURE-7B95).
- **SVG masters:** `packaging/icon/enigma-hardcopy.svg` and `enigma-hardcopy-small.svg`, verbatim, as
  Enigma.GitClient keeps them.
- **Android (HardCopy Recover):**
  - The adaptive icon's background is the tile colour `#1E1F22`.
  - The foreground is the code redrawn from the master SVG as vector path data, in its two colours. The finder
    patterns are one even-odd path, so their dark rings show the background. The 136-unit code is scaled to 42
    units, inside the 66-unit safe zone.
  - A new single-colour `app_icon_monochrome` layer serves themed icons.
  - The pre-API-26 `mipmap/app_icon` draws the branding tile itself, a rounded square, where it used to draw a
    round badge.

## Files/modules touched

- Modified: `src/Enigma.HardCopy.Desktop/Assets/app.ico`
- Created: `src/Enigma.HardCopy.Desktop/Assets/app-256.png`
- Created: `packaging/icon/enigma-hardcopy.svg`, `packaging/icon/enigma-hardcopy-small.svg`
- Modified: `src/Enigma.HardCopy.Recover.Android/Resources/drawable/app_icon_foreground.xml`
- Created: `src/Enigma.HardCopy.Recover.Android/Resources/drawable/app_icon_monochrome.xml`
- Modified: `src/Enigma.HardCopy.Recover.Android/Resources/mipmap/app_icon.xml`
- Modified: `src/Enigma.HardCopy.Recover.Android/Resources/mipmap-anydpi-v26/app_icon.xml`
- Modified: `src/Enigma.HardCopy.Recover.Android/Resources/values/colors.xml`
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/AppIconTests.cs`, `tests/Enigma.HardCopy.Desktop.UnitTests/RepositoryRoot.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-1148.md`

## Deviations & follow-ups

- The branding SVGs are CRLF on disk. Git stores them LF, per `* text=auto eol=lf`, so the repository copy is
  identical except for line endings. Recommendation only: none needed.
- The legacy icon's shape changed from a round badge to the branding's rounded square. That is how the branding
  draws the icon, and a pre-26 launcher shows the icon as drawn.
- Plan step 5 asked to start the Desktop app once and look at the window icon. That was replaced by byte identity
  with the branding `.ico`, which carries every frame the OS picks from.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Foreground scale | 0.31 of the SVG, so the code is 42 units | The size the previous glyph had (54 × 0.78), well inside the 66-unit safe zone |
| How to check the Android artwork without a device | Rendered the three vector drawables with SkiaSharp from a scratch script, using their own path data and group transforms, under a circular mask with the safe-zone circle drawn | The resources compile in the build, but compiling says nothing about how they look |
| Where tests find repository files | A small `RepositoryRoot` helper, which walks up to `Enigma.HardCopy.slnx` | The assets are embedded resources, so they are absent from the test output. FEATURE-3D14 and FEATURE-92E2 pin packaging files the same way |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors. The Android head was included, and its
  `obj/…/res/drawable/` holds the compiled `app_icon_foreground.xml` and `app_icon_monochrome.xml`.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **798 passed**, 0 failed. That is the 796
  existing tests plus 2 new ones:
  - `AppIco_HoldsTheTenBrandingFrames_EachAPng`;
  - `AppPng_IsTheIconsLargestFrame_At256Pixels`.
- `cmp` shows that `app.ico`, `app-256.png` and `enigma-hardcopy.svg` are byte-identical to the branding files.
- Rendered check: the adaptive icon stays inside the safe zone, the themed layer keeps its finder-pattern holes,
  and the legacy icon matches the branding tile.
- Doc sweep: README and `docs/android.md` say nothing about the icon's look. No edit.
