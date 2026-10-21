# FEATURE-E931-PHASE02 — Recover app & Android head scaffold — DONE

**Branch:** `feature/feature-e931-phase02-scaffold`
**Date:** 2026-10-19
**Run:** feature/2026-10-19-android-recover-app

## Summary

The recovery app exists and runs on Android. It is split the way the plan laid out:

- **`src/Enigma.HardCopy.Recover`** (net10.0) — the whole UI. `App` merges
  `avares://Enigma.Avalonia.Desktop/Themes/Fluent.axaml` exactly as the desktop app does, over `FluentTheme`,
  with the desktop app's style classes at phone scale and 48-dip buttons. It builds the container with
  `Host.CreateEmptyApplicationBuilder` and a Debug-only logging provider, takes platform registrations through an
  `App(Action<IServiceCollection>)` constructor, and supports both the Android activity lifetime (a factory that
  runs on every activity recreation) and the single-view lifetime. `MainView` is the app's only view: header
  (QR-code icon, name, subtitle), an introduction card with the privacy note, and the `ContentDialog` and
  `InfoBar` hosts — registered with the library's services each time a view is built, the storage provider once
  it is attached. The view pads itself by the display's safe area, since Android 15+ draws edge to edge.
- **`src/Enigma.HardCopy.Recover.Android`** (net10.0-android, minSdk 24) — `AndroidApp`
  (`AvaloniaAndroidApplication<App>`, Inter font), `MainActivity` (portrait, in-place configuration changes,
  **`FLAG_SECURE`** set before the first frame), a manifest with `allowBackup="false"` and no permissions, an
  adaptive launcher icon (a QR motif on the theme's accent), and a pre-first-frame window background matching
  `EnigmaBackgroundColor` in light and dark.
- **`tests/Enigma.HardCopy.Recover.UnitTests`** — the strings contract and the DI wiring.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Recover/Enigma.HardCopy.Recover.csproj`, `App.axaml`, `App.axaml.cs`
- `src/Enigma.HardCopy.Recover/Hosting/EnigmaAvaloniaServiceCollectionExtensions.cs` — the three library
  services this app uses (dialog, info bar, file picker), in their own file as in Desktop.
- `src/Enigma.HardCopy.Recover/Hosting/ServiceCollectionExtensions.cs` — `MainView`, transient.
- `src/Enigma.HardCopy.Recover/Views/MainView.axaml` + `.axaml.cs`
- `src/Enigma.HardCopy.Recover/Resources/Strings.resx` + `Strings.cs`, `Resources/AppIcons.cs`
- `src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj`, `AndroidApp.cs`,
  `MainActivity.cs`, `AndroidManifest.xml`, `Resources/values/{colors,styles}.xml`,
  `Resources/values-night/colors.xml`, `Resources/drawable/app_icon_foreground.xml`,
  `Resources/mipmap/app_icon.xml`, `Resources/mipmap-anydpi-v26/app_icon.xml`
- `tests/Enigma.HardCopy.Recover.UnitTests/Enigma.HardCopy.Recover.UnitTests.csproj`, `StringsTests.cs`,
  `Hosting/ServiceCollectionExtensionsTests.cs`
- `docs/done/FEATURE-E931-PHASE02.md` — this file.

**Modified**

- `Directory.Packages.props` — `Avalonia.Android` 12.1.1 in the version-coupled Avalonia group.
- `Enigma.HardCopy.slnx` — the three new projects.
- `README.md` — documentation sweep: *Build from source* now names the `android` workload (a full solution
  build fails without it), and the *Project layout* table lists the two new projects.
- `docs/roadmap.md`, `docs/plan/FEATURE-E931.md` — statuses.

## Deviations & follow-ups

- **QuestPDF's native libraries were being packed into the APK.** Android's RID graph falls back from
  `android-x64` / `android-arm64` to `linux-x64` / `linux-arm64`, so QuestPDF's desktop-Linux `.so` files
  (arriving with Core) matched and were packaged — caught by `XA0141` (16 KB page size). The Android head now
  references `QuestPDF` with `ExcludeAssets="native"`: the managed assembly Core links against stays, the
  unloadable natives go. Verified: the APK's `lib/` holds only SkiaSharp, HarfBuzzSharp and the runtime.
  Follow-up (out of scope): splitting the PDF composition out of Core would drop QuestPDF from the APK entirely.
- `App(Action<IServiceCollection>)` is not yet called by the head: nothing platform-specific is registered until
  PHASE03/PHASE04 add seams; the head uses Avalonia's default parameterless construction until then.
- **Visual verification is by accessibility tree, not screenshot**: `FLAG_SECURE` makes `screencap` return a
  black frame, which is the point. The tree and its bounds were checked instead (see evidence).
- **Deploying a Debug build:** a bare `adb install` of the Debug APK aborts at start (`No assemblies found …
  Assuming this is part of Fast Deployment`) — Debug builds keep their assemblies out of the APK. Deploy with
  `dotnet build src/Enigma.HardCopy.Recover.Android -t:Install` (or `-t:Run`). To be written into the Android
  guide in PHASE05.
- The emulator needs `ANDROID_SDK_ROOT` pointed at the Visual Studio SDK (`C:\Program Files (x86)\Android\android-sdk`),
  which holds the system image; the emulator binary lives under `%LOCALAPPDATA%\Android\Sdk`. Also for PHASE05.
- Line endings: `.gitattributes` enforces `eol=lf`; nothing to recommend.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Excluding QuestPDF's natives | Direct `PackageReference` with `ExcludeAssets="native"` in the head | Declarative, one line, keeps the managed assembly | A target pruning `.so` items (brittle, SDK-internal item names); `ExcludeAssets="all"` (removes the assembly Core references) |
| Library services registered | Dialog, info bar, file picker only | A one-screen phone app has no rail, overlay or folder picker; registering them invites calls with no host | All six, as in Desktop |
| Status of `App` host | Built, never started | No hosted services; Android kills the process instead of shutting it down | `_host.Start()` as in Desktop (no effect, nothing to stop) |
| Header icon | Phosphor `QrCode` via `AppIcons.App` | What the app reads; verified against the Phosphor enum | — |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` (Debug, Android head included): **0 warnings, 0 errors** (no `AVLN*`).
- `dotnet test --solution Enigma.HardCopy.slnx`: **706 passed, 0 failed, 0 skipped** (was 697; +9).
- Fix cycles used: **1 of 3** — `xUnit2031` in the new hosting test and `XA0141` from QuestPDF's natives, both fixed
  in the same cycle.
- Emulator `Pixel6_API36` (API 36, x86_64), deployed with `-t:Install`: the app starts and stays up (no fatal
  log line for its process); `dumpsys window` shows `fl=… SECURE …`; `uiautomator dump` shows the four texts
  (title, subtitle, introduction, privacy note), with the header at y = 170 px, below the 128 px status bar —
  the safe-area padding works.
