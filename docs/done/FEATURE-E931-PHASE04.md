# FEATURE-E931-PHASE04 — CameraX scanner & live preview — DONE

**Branch:** `feature/feature-e931-phase04-camera`
**Date:** 2026-10-19
**Run:** feature/2026-10-19-android-recover-app

## Summary

The phone's camera now feeds the scan session.

- **`CameraXScanner`** (Android head) is the real `ICameraScanner`:
  - It asks for the `CAMERA` permission when the user starts scanning. The request goes through
    `ActivityCompat.RequestPermissions`, and the answer comes back through Avalonia's
    `IActivityResultHandler.RequestPermissionsResult`.
  - It binds one CameraX `ImageAnalysis` use case to the activity's lifecycle: keep-only-latest, aiming for
    1920×1080, back camera preferred, front camera as a fallback.
  - On the analysis thread it copies each frame's YUV planes into reused buffers and raises `FrameAvailable` for
    the preview.
  - It hands the luminance to a **single decode worker** whenever that worker is idle. The worker calls Core's
    `ImageDecoder.ReadCodes` (PHASE01), so ZXing runs in-process; there is no Play Services and no barcode SDK.
  - `CodesScanned` is raised on the UI thread. `Stop` unbinds the camera, and codes still in flight are dropped.
    The camera pauses and resumes with the app by itself.
- **`CameraFrameConverter`** (shared, unit-tested) turns a YUV 4:2:0 frame into an upright BGRA preview at half
  resolution:
  - Each 2×2 luminance block lines up with exactly one chroma sample, so no interpolation is needed.
  - Rotation can be 0, 90, 180 or 270°.
  - Colour uses the full-range BT.601 transform in integer arithmetic.
  - It accepts any row padding, pixel stride 1 or 2, and an unpadded last row.
- **`CameraPreview`** (shared Avalonia control) draws the latest frame uniform-to-fill into a `WriteableBitmap`.
  At most one frame is in flight: frames arriving while the UI thread is busy are skipped before they are even
  converted. Because Avalonia draws it — not a native view — the info bar and the dialogs stay above it. `App`
  wires the preview to the scanner, the same way it wires the hosts to their services.
- **`ViewHapticFeedback`** plays the system's `CONFIRM` / `REJECT` ticks (Android 11+), or the closest standard
  ticks below that, on the activity's window. No permission is needed.
- **`ActivityTracker`** gives these services the current activity: the application registers it for every
  activity's lifecycle callbacks and hands the same instance to the container.
- The manifest declares `CAMERA` and `android.hardware.camera.any`.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Recover/Camera/YuvFrame.cs`, `CameraFrameConverter.cs`, `CameraFrameEventArgs.cs`
- `src/Enigma.HardCopy.Recover/Controls/CameraPreview.cs`
- `src/Enigma.HardCopy.Recover.Android/CameraXScanner.cs`, `ViewHapticFeedback.cs`, `ActivityTracker.cs`
- `tests/Enigma.HardCopy.Recover.UnitTests/CameraFrameConverterTests.cs` (22 tests)
- `docs/done/FEATURE-E931-PHASE04.md` — this file.

**Modified**

- `Directory.Packages.props` — CameraX bindings 1.6.1 (camera2, core, lifecycle), with the reason for the pin.
- `src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj` — the three CameraX references.
- `src/Enigma.HardCopy.Recover.Android/AndroidApp.cs` — registers the tracker; builds `App` with the camera, the
  haptics and the tracker as platform services.
- `src/Enigma.HardCopy.Recover.Android/AndroidManifest.xml` — `CAMERA` permission, camera feature.
- `src/Enigma.HardCopy.Recover/Services/ICameraScanner.cs` — `FrameAvailable` (moved here from PHASE03, see
  PHASE03's deviations); `UnavailableCameraScanner` and the test fake implement it as never-raised.
- `src/Enigma.HardCopy.Recover/App.axaml.cs` — the preview gets the scanner.
- `src/Enigma.HardCopy.Recover/Views/MainView.axaml` — the preview (320 dip) and the guidance under it.
- `src/Enigma.HardCopy.Recover/Views/MainView.axaml.cs` — safe-area fix (see *Deviations*).
- `src/Enigma.HardCopy.Recover/Resources/Strings.resx` + `Strings.cs` — the preview's accessible name.
- `docs/done/FEATURE-E931-PHASE03.md` — the ViewModel test count corrected from 22 to 23 (the totals were right).
- `docs/roadmap.md`, `docs/plan/FEATURE-E931.md` — statuses.

## Deviations & follow-ups

- **CameraX 1.6.1, not the latest 1.6.2.** 1.6.1.1 and later move AndroidX Lifecycle to 2.11. Avalonia.Android
  12.1.1 pins its Lifecycle packages to `[2.10.0.2, 2.10.1)`, and restore failed with `NU1608` (fix cycle 1).
  1.6.1 is the newest binding on the 2.10 line. Overriding Avalonia's transitive AndroidX set was rejected: it
  would put Avalonia on a Lifecycle version it was never built against. Bump CameraX with the Avalonia group once
  Avalonia's AndroidX set moves; the CPM comment says so.
- **Safe-area padding bug, found on the emulator and fixed (fix cycle 3).**
  - Symptom: after a cold start (permission revoked → process killed → relaunch), the page started 336 px down
    instead of 170 px.
  - Cause: decompiling Avalonia.Android 12.1.1 shows `SafeAreaPadding` is the insets divided by
    `RenderScaling`, and `SafeAreaChanged` is raised only when the insets change. When the insets arrive before
    the density is known, the scaling is 1, the value is in pixels, and no later event corrects it.
  - Fix: `MainView` now re-reads the property on `SafeAreaChanged` **and** on `TopLevel.ScalingChanged`.
  - Verified across repeated cold starts: header at y = 170 px every time. The PHASE02 scaffold had the latent
    form of this bug; it did not show then because the timing differed.
- Fix cycle 2: the CameraX bindings' nullable annotations (`Build()`, `Buffer`, `ImageInfo`, the default camera
  selectors, `Analyze(IImageProxy?)`) and a `Buffer` clash with `Java.Nio`. The fluent builder chains became
  statements on the builder object.
- **Not verified on a real phone with real codes.** The emulator's camera is a virtual scene, so decoding real
  printed codes through this pipeline has not been observed. That path is proven piecewise: Core's frame tests
  decode dense symbols from camera-style planes (PHASE01), and the converter and the session are unit-tested. A
  physical-phone test with a printed backup is the first thing to do with this build.
- **CPU while scanning:** about 2.3 cores on the emulator's software CPU. The decode worker re-runs as soon as it
  finishes (`TryHarder` on 1856×1392), and the preview converts every frame the UI is ready for. Pausing drops it
  to 0 %. Follow-up after measuring on a real phone: throttle the decode worker to a few reads per second, or
  decode a central crop.
- **The emulator's first CameraX start took about a minute** (`CameraX: The device might underreport the amount
  of the cameras…`): the AVD has no front camera and CameraX retries its enumeration. This is emulator-specific
  and is noted for the Android guide (PHASE05).
- A permission refused twice is auto-denied by Android without a prompt (`USER_FIXED`). The app's refusal
  message already says to allow the camera in the system settings when Android stops asking, and the emulator
  showed exactly that.
- Documentation sweep: nothing stale. The Android usage documentation is PHASE05.
- Line endings: `.gitattributes` enforces `eol=lf`; nothing to recommend.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where YUV→BGRA runs | In the preview control's handler, only when the previous frame has been presented | A busy UI thread costs no conversion work at all | Converting every frame in the scanner |
| How the head reaches the activity | `ActivityTracker` (lifecycle callbacks), one instance owned by `AndroidApp` and registered in DI | Activities are created by Android, not the container; no static state | A static `MainActivity.Current` |
| Permission result plumbing | Avalonia's `IActivityResultHandler.RequestPermissionsResult` | Already on `AvaloniaActivity`; no override of the deprecated callback needed | AndroidX activity-result launcher (must be registered in `OnCreate`) |
| Frame buffer copies | `GetDirectBufferAddress` + `Marshal.Copy` into reused arrays | One memcpy per plane, no `unsafe`, no per-frame allocation | `ByteBuffer.Get(byte[])` (JNI array copy) |
| Exceptions on the analysis thread | Caught and logged per frame | An exception escaping a Java callback ends the process; one bad frame is not worth that | Letting it propagate |
| Camera choice | Back camera, else front | Tablets and kiosk devices may only have a front camera | Back camera only |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` (Debug, Android head included): **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.HardCopy.slnx`: **785 passed, 0 failed, 0 skipped** (was 763; +22).
- Fix cycles used: **3 of 3** — `NU1608` (CameraX version), nullable annotations, the safe-area padding. Green
  after the third.
- Emulator `Pixel6_API36` (virtual back camera), deployed with `-t:Install`:
  - *Start scanning* raises the system prompt ("Allow HardCopy Recover to take pictures and record video?").
  - Allowing it starts the camera: CameraX attaches the `ImageAnalysis` stream at 1856×1392, and the page shows
    the 990×840 px preview, the guidance and *Pause scanning*.
  - Refusing it shows the refusal explanation with *Start scanning* still offered.
  - *Pause scanning* stops the camera (process CPU back to 0 %).
  - No error-level log line from the app's process, other than the platform's own `hiddenapi` and CameraX
    enumeration messages.
