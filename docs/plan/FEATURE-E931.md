# FEATURE-E931 — Android recovery app (Enigma.HardCopy.Recover.Android)

**Status:** IN PROGRESS
**Type:** multi-phase feature (5 phases)
**Branches (created by the run, each from the run-branch tip):** one per phase, see *Phases*
**Run:** feature/2026-10-19-android-recover-app

## Objective

A new **Android** application, `Enigma.HardCopy.Recover.Android`, built with **Avalonia 12**, that recovers a
file from the QR codes printed by `Enigma.HardCopy.Desktop`:

- the user starts a **new scan session** and points the phone's camera at the printed pages;
- codes are accepted **in any order**; codes belonging to **a different backup are rejected** and the user is
  told so;
- the app shows the **progression** — data codes received out of the total, whether the metadata code is in,
  and which indexes are still missing;
- once everything is in, the file is rebuilt, checked against its SHA-256, and **saved where the user
  chooses** — phone storage, Google Drive, or any other document provider;
- the app **stores nothing**: no settings, no logs, no cache, no backup of app data, no screenshots.

The appearance is the one of `Enigma.Avalonia.Desktop` — the same theme dictionary, at the same version, as
the desktop app.

## Context & constraints

- **Recovery already exists in Core.** `RecoverySession` accepts codes in any order, adopts the backup ID and
  chunk count from the first verified code, rejects anything carrying another backup ID
  (`AddCodeOutcome.WrongBackup`) or another total (`Conflict`), reports progress through `RecoveryStatus`
  (`TotalChunks`, `ReceivedChunks`, `MissingIndexes`, `HasMetadata`, `IsComplete`) and verifies the SHA-256 in
  `TryAssemble`. The Android app reuses it unchanged — **the format is untouched**.
- The draft's recollection is right: every code is `EHC1:<BID>:<IDX>/<TOT>:<CRC>:<PAYLOAD>`; `IDX` 0 is the
  metadata code, `1..TOT` are the data codes.
- **`Enigma.Avalonia.Desktop` 1.0.0 is platform-neutral managed code** (dependencies: `Avalonia`,
  `Avalonia.Themes.Fluent`, `CommunityToolkit.Mvvm`, `Enigma.Core`), so it loads on `net10.0-android`. Its
  skill lists mobile as "not designed for"; the draft explicitly asks for its look, which wins. Only the theme,
  `InfoBar` and `ContentDialog` are used.
- **Build environment verified:** .NET 10 SDK, `android` workload 36.1, the Visual Studio Android SDK, and an
  AVD `Pixel6_API36`. `Avalonia.Android` 12.1.1 restores from nuget.org; CameraX bindings
  (`Xamarin.AndroidX.Camera.*`) likewise.
- **Avalonia.Android 12 surface verified:** `AvaloniaAndroidApplication<TApp>` (`CreateAppBuilder` /
  `CustomizeAppBuilder`), `AvaloniaMainActivity` (an `AppCompatActivity`, hence a lifecycle owner for CameraX),
  `IActivityResultHandler.RequestPermissionsResult`, `IActivityApplicationLifetime.MainViewFactory`.
- **Namespace shadowing.** Inside `namespace Enigma.HardCopy.Recover…`, an inline `Avalonia.X` resolves against
  `Enigma.Avalonia`; inside `namespace Enigma.HardCopy.Recover.Android`, an inline `Android.X` resolves against
  the project's own namespace. File-scope `using` directives (house rule) avoid both; `global::` where a using
  cannot.
- **Version-coupled Avalonia set.** `Avalonia.Android` joins the coupled group at **12.1.1**, the version the
  rest of the solution pins. Nothing in the group moves.
- **Zero warnings**, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, explicit usings, CPM — inherited from
  the root props.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Project shape | `src/Enigma.HardCopy.Recover` (net10.0: App, views, ViewModels, seams, frame conversion) + `src/Enigma.HardCopy.Recover.Android` (net10.0-android head: activity, CameraX, haptics) + `tests/Enigma.HardCopy.Recover.UnitTests` | Everything but the camera adapter is unit-testable on the desktop test runner; the same split as the house Avalonia Android reference project | A single Android project — its ViewModels could not be tested without a device |
| QR decoding engine | **ZXing.Net**, already a Core dependency, in-process, behind Core's `ImageDecoder` seam via a new luminance-frame overload | The codes are the secret: decoding stays in the app's own process, open source, no new decoding dependency, and ZXing stays behind the seam Core already defines for it | ML Kit (proprietary, Google telemetry, new dependency); Google Code Scanner (Play Services UI, one code per launch, the secret transits GMS); ZXing.Net.MAUI (MAUI only) |
| Camera API and preview | CameraX `ImageAnalysis` **only**; each frame's Y plane feeds the decoder, and a downsampled, rotated BGRA copy is drawn by an Avalonia control | No native view in the visual tree, so the `InfoBar` and `ContentDialog` draw above the preview; one use case is the lightest camera configuration | `PreviewView` in a `NativeControlHost` — a native view always draws over Avalonia content (airspace), hiding the info bar and dialogs; Camera2 by hand — far more code; deprecated `android.hardware.Camera` — obsolete warnings |
| Look and feel | Reference `Enigma.Avalonia.Desktop` **1.0.0** (the desktop app's version) and merge `Themes/Fluent.axaml` exactly as the desktop app does; `InfoBar` for outcomes, `ContentDialog` for confirmations; the desktop app's style classes reused where they fit | "The same styles/themes" by construction, and kept in step by the package rather than by copying | Copying the colour tokens (drifts); `NavigationView` (a single-task app has nothing to navigate); bumping to 1.2.0 (moves the coupled set, out of scope) |
| Where the file is saved | Avalonia `StorageProvider.SaveFilePickerAsync` → Android Storage Access Framework, through the library's `IFileDialogService` | One picker covers phone storage, Google Drive and every other document provider; no storage permission | Google Drive REST API (OAuth, new dependencies, network); MediaStore Downloads only (no Drive) |
| "Must not store anything" | No settings file, no log file, no cache; `android:allowBackup="false"`; `FLAG_SECURE` on the window; the session lives in memory only and is lost with the process; theme follows the system | The draft forbids storage; Android persists a recents screenshot to disk unless `FLAG_SECURE` is set, and the preview shows the secret | Remembering a theme preference (storage); a session that survives process death (storage) |
| Wrong-file check | Rely on `RecoverySession`: the first accepted code fixes the backup; `WrongBackup` → warning InfoBar naming both IDs + reject haptic | Already correct and tested in Core | A second identity check in the app |
| Repeated detections | A per-session set of code texts already processed; a text seen again is ignored silently | The camera sees the same code ~30 times a second; without it the log and the info bar drown in duplicates and repeated warnings | Rate-limiting by time (still repeats) |
| Progress shown | "N of T data codes", metadata received yes/no, a bar over T+1 codes (data + metadata), missing indexes as compressed ranges capped at a readable length, the last code scanned, file name/size/backup ID once known | Mirrors the desktop page's progress panel in a phone-sized layout | A full activity log (unreadable on a phone) |
| Scan feedback | Haptic tick on an accepted code, reject pattern on a rejected one, via `View.PerformHapticFeedback` | The user is looking at paper, not at the screen; needs no permission and honours the system's touch-feedback setting | Vibrator API (permission); sound |
| Completion | When the session is complete the camera stops by itself and *Save recovered file* becomes the primary action | Saves battery, stops pointless decoding | Leaving the camera running |
| Hash mismatch | As in the desktop app: bytes held back, *Save anyway* behind a confirmation dialog naming both hashes | The one dangerous action gets the same guard on both platforms | Refusing outright; saving silently |
| New session | Confirmation only when the current session already holds a code | Same rule as the desktop *Start over* | Always confirming |
| Camera permission | Requested when the user starts a session, via `ActivityCompat.RequestPermissions` + Avalonia's `IActivityResultHandler.RequestPermissionsResult`; a refusal shows an explanation and *Try again* | Asks in context; no subclassed override of an obsolete callback | Asking at launch |
| Orientation | Portrait | A scanner is used one-handed in portrait; avoids re-deriving the frame rotation on every turn | Free rotation |
| Live decoding tuning | `TryHarder` off for camera frames; a single decode worker that drops frames while busy; 1920×1080 analysis target, back camera (any camera as fallback) | The next frame is the retry, so a cheap pass repeated beats an expensive pass on a stale frame; dense codes need the resolution | `TryHarder` on (halves the attempt rate); decoding on the analyzer thread (stalls the preview) |
| Identity | ApplicationId `io.github.enigmalibs.hardcopy.recover`, launcher label **HardCopy Recover**, `ApplicationDisplayVersion` = `$(Version)`, `ApplicationVersion` 1, minSdk 24 (Avalonia 12's floor) | Reverse-domain id for a GitHub-hosted project with no owned domain; the full assembly name is too long for a launcher | `ch.petitpierre.*` (not a Petitpierre product) |
| Solution membership | The two new projects and the test project are added to `Enigma.HardCopy.slnx`; the `android` workload becomes a prerequisite of a full solution build, documented | One solution, one build, one test command | A separate `.slnx` (two builds to keep green) |
| DI / hosting | `Host.CreateEmptyApplicationBuilder` (as the house Android reference), Debug logging provider only | Hosting is already pinned; the Debug provider writes nothing unless a debugger is attached — nothing stored | `Host.CreateApplicationBuilder` (console/config/file providers meaningless on Android) |
| Platform services from the head | The head passes a registration callback to `App` through `AvaloniaAndroidApplication.CreateAppBuilder` (`AppBuilder.Configure(() => new App(...))`) | The shared project stays free of Android types; the composition root stays in `App` | A static service locator |
| Localization | English only, `Strings.resx` with a hand-written accessor, as the desktop app | Consistency; the desktop app is English only | — |
| Release | No version bump, no release notes section, no signing setup, no store listing | Not asked for; the build produces a debug-signed APK to sideload, documented | Cutting 1.2.0 now |
| Tests | Unit tests for the Core frame decoder, the frame converter, the ViewModel, the DI wiring and the strings; an emulator launch smoke check per UI phase | No instrumentation or headless-UI infrastructure exists in the solution and none is introduced | Adding Avalonia.Headless (new test infrastructure) |

## Architecture

### Projects

```
src/Enigma.HardCopy.Core/                 (existing — PHASE01 adds one public overload)
src/Enigma.HardCopy.Recover/              net10.0      App, MainView, ScanSessionViewModel, seams,
                                                       CameraPreview control, frame conversion, Strings
src/Enigma.HardCopy.Recover.Android/      net10.0-android  AndroidApp, MainActivity, CameraX scanner,
                                                       haptics, manifest, launcher icon
tests/Enigma.HardCopy.Recover.UnitTests/  net10.0      xunit.v3
```

### Seams (shared project, implemented by the head)

| Seam | Purpose | Android implementation | Default / test |
|---|---|---|---|
| `ICameraScanner` | Start (asks permission) / stop the camera; raises `CodesScanned` on the UI thread and `FrameAvailable` (BGRA preview) on a camera thread | `CameraXScanner` | `UnavailableCameraScanner` (PHASE03 default) / fake |
| `IHapticFeedback` | Accept / reject tick | `ViewHapticFeedback` | no-op / recording fake |
| `IRecoveredFileSaver` | Ask where to save, return an `ISaveTarget` | library `IFileDialogService` save picker | fake |
| `INotificationService` | Outcome banner | library `IInfoBarService` | fake |
| `IConfirmationService` | *Save anyway* / *New session* questions | library `IContentDialogService` | fake |

### Camera pipeline (PHASE04)

```
CameraX ImageAnalysis (YUV_420_888, keep-only-latest, analyzer executor)
 ├─ Y plane ─► [decoder idle?] copy ─► worker: ImageDecoder.ReadCodes(luma, w, h, stride) ─► UI thread: CodesScanned
 └─ Y/U/V  ─► CameraFrameConverter.ToBgra(rotation, /2 downsample) ─► FrameAvailable ─► CameraPreview (WriteableBitmap)
```

### Window composition

```
MainView (UserControl, Background=EnigmaBackgroundBrush, safe-area padded)
└─ Panel
   ├─ Grid: header · camera preview / start card · progress card · actions
   ├─ contentDialog:ContentDialog  x:Name="HostDialog"
   └─ infoBar:InfoBar              x:Name="HostInfoBar"   (top)
```

The hosts are re-registered whenever a `MainView` is attached (the Android lifetime may recreate the view);
`RegisterHost` simply replaces the previous host.

## Phases

### PHASE01 — Core camera-frame decoding (DONE)

Branch: `feature/feature-e931-phase01-frame-decoding`

1. `ImageDecoder.ReadCodes(byte[] luminance, int width, int height, int rowStride)` — public, returns every QR
   code text found in an 8-bit luminance frame (a camera's Y plane), never `null`. Multi-symbol pass first, then
   a single-symbol pass; `TryHarder` **off** (documented: live frames are retried by the next frame).
   **Amended at build time:** `TryHarder` stays **on** — off, the multi-symbol pass misses symbols outright
   (see `docs/done/FEATURE-E931-PHASE01.md`). The decode worker's frame dropping absorbs the cost.
2. Argument validation: `ArgumentNullException`; `ArgumentOutOfRangeException` for non-positive sizes, a stride
   below the width, and a buffer shorter than `rowStride × (height − 1) + width` (a camera's last row may be
   unpadded).
3. Tests in `Enigma.HardCopy.Core.UnitTests`: a rendered code inside a larger frame with a padded stride is read;
   two codes in one frame are both read; a blank frame yields nothing; each invalid argument throws; a real
   `BackupEncoder` code round-trips through a frame into a `RecoverySession`.

**Acceptance:** solution builds with zero warnings; full suite green; ZXing types do not appear in the new
public signature.

### PHASE02 — Recover app & Android head scaffold (DONE)

Branch: `feature/feature-e931-phase02-scaffold`

1. `Directory.Packages.props`: `Avalonia.Android` 12.1.1 in the coupled group (comment updated).
2. `src/Enigma.HardCopy.Recover` (net10.0, compiled bindings): `App.axaml` merging
   `avares://Enigma.Avalonia.Desktop/Themes/Fluent.axaml` + `FluentTheme` + the shared style classes;
   `App.axaml.cs` building the host (`CreateEmptyApplicationBuilder`), accepting platform registrations,
   supporting `IActivityApplicationLifetime` and `ISingleViewApplicationLifetime`; `Views/MainView` with the
   header, an intro card, the `ContentDialog` and `InfoBar` hosts registered on attach, and safe-area padding;
   `Hosting/` registration extensions (library services in their own file, as in Desktop);
   `Resources/Strings.resx` + `Strings.cs`; `Resources/AppIcons.cs`.
3. `src/Enigma.HardCopy.Recover.Android` (net10.0-android, minSdk 24): `AndroidApp`
   (`AvaloniaAndroidApplication<App>`, Inter font), `MainActivity` (portrait, in-place config changes,
   `FLAG_SECURE`), `AndroidManifest.xml` (`allowBackup="false"`), launcher icon (adaptive vector), window
   background colours matching `EnigmaBackgroundColor` light/dark, AppCompat DayNight theme.
4. `tests/Enigma.HardCopy.Recover.UnitTests`: Strings test (every accessor resolves), DI registration test.
5. `Enigma.HardCopy.slnx`: the three projects.

**Acceptance:** `dotnet build Enigma.HardCopy.slnx` zero warnings (Android head included); full suite green; the
APK installs and launches on the `Pixel6_API36` emulator without a crash (logcat clean of fatal exceptions) and
its accessibility tree shows the app title.

### PHASE03 — Scan session page & save flow (TODO)

Branch: `feature/feature-e931-phase03-scan-session`

1. `ScanSessionViewModel` (CommunityToolkit.Mvvm, the desktop VM's style): state (`Idle`, `Starting`,
   `Scanning`, `PermissionDenied`, `CameraUnavailable`, `Complete`); `StartSessionCommand`,
   `NewSessionCommand` (confirms when codes are held), `SaveCommand` (assemble → verified → save picker → write),
   `SaveAnywayCommand` (confirmation naming both hashes, then save); ingestion of `CodesScanned` with the
   processed-text set, `RecoverySession.AddCode`, haptics, last-scan line, warnings/errors to the info bar;
   auto-stop of the scanner on completion; progress properties (`CodesText`, `MetadataText`,
   `ProgressPercent` over T+1, `MissingText` as compressed ranges with a cap, backup ID, file name and size).
2. Seams: `ICameraScanner` (+ `CameraStartResult`, `CodesScannedEventArgs`, `CameraFrame` /
   `FrameAvailable` contract), `IHapticFeedback`, `IRecoveredFileSaver` + `ISaveTarget`,
   `INotificationService`, `IConfirmationService`, with their library-backed implementations and the defaults
   `UnavailableCameraScanner` / `NoHapticFeedback`.
3. `OutcomeMessages` / `StatusMessage` / `MessageSeverity` for this app (phone wording; same severities as the
   desktop app).
4. `MainView` layout: start card (instructions + *Start a new scan session*), scanning area placeholder,
   progress card, action row (*Save recovered file*, *Save anyway*, *New session*); 48dp touch targets,
   automation names.
5. Tests: progress texts and ranges, any-order acceptance, wrong-backup warning raised once, duplicate texts
   ignored, completion stops the scanner, verified save writes the exact bytes, mismatch → no write until
   confirmed, declined confirmation writes nothing, write failure → error, new-session confirmation rules,
   permission-denied and unavailable states, empty-file backup (metadata only).

**Acceptance:** zero warnings; suite green; emulator launch shows the start card; pressing start reports the
camera as unavailable (no scanner yet) without crashing.

### PHASE04 — CameraX scanner & live preview (TODO)

Branch: `feature/feature-e931-phase04-camera`

1. `Directory.Packages.props`: `Xamarin.AndroidX.Camera.Camera2`, `.Lifecycle`, `.Core` (same release line);
   referenced by the Android head only.
2. Shared `Camera/CameraFrameConverter`: YUV_420_888 planes (row and pixel strides) → BGRA, 2× downsampled,
   rotated by 0/90/180/270 — pure, unit-tested.
3. Shared `Controls/CameraPreview`: draws the latest BGRA frame (uniform-to-fill) from `ICameraScanner`;
   one pending frame at most, copied on the UI thread.
4. Android `CameraXScanner`: permission request, `ProcessCameraProvider` bind to the activity lifecycle (back
   camera, any camera as fallback), `ImageAnalysis` 1920×1080 keep-only-latest, Y-plane decode worker through
   `ImageDecoder.ReadCodes`, `CodesScanned` raised on the UI thread, `FrameAvailable` per frame, `Stop` unbinds.
   `ViewHapticFeedback` (`CONFIRM` / `REJECT` on API 30+, fallbacks below).
5. Manifest: `CAMERA` permission, `android.hardware.camera.any` feature.
6. Tests: converter (orientation of a known pattern for each rotation, downsampling, colour of known YUV values,
   pixel-stride 1 and 2, padded row strides).

**Acceptance:** zero warnings; suite green; on the emulator, *Start* raises the permission prompt, granting it
starts the preview without a crash, and refusing it shows the explanation.

### PHASE05 — README & Android guide (TODO)

Branch: `feature/feature-e931-phase05-docs`

1. `README.md`: the Android app in the introduction, a *Recovering on Android* section (scan, progress, wrong
   backup, saving to the phone or Google Drive, what is never stored and why screenshots are blocked), the
   Android build prerequisite in *Build from source*, the project-layout table.
2. `docs/android.md`: prerequisites, building, running on the emulator and on a phone (USB / APK sideload),
   troubleshooting (camera permission, focus distance, dense codes).

**Acceptance:** every command in the guide was run on the development machine or is marked as not verified;
README facts match the app.

## Out of scope

- Typing codes by hand on the phone and importing photos from the gallery (the desktop app covers both).
- Any change to the `EHC1` format, the PDF, or the desktop app.
- Splitting Core so the APK does not carry QuestPDF (unused on Android) — follow-up.
- Release work: version bump, release notes, signing keys, Play Store listing, publish scripts.
- Colour preview tuning beyond a straightforward YUV→RGB conversion; torch / zoom / tap-to-focus controls.

## Risks

- **ZXing on dense codes from a live camera.** Large-preset codes reach QR version ~36. Mitigation: 1920×1080
  analysis, multi-symbol + single-symbol passes, user guidance in the docs (one code filling the frame).
- **Performance of managed per-frame work on Mono.** Mitigation: decode worker drops frames while busy; preview
  conversion downsampled 2×; at most one pending preview frame.
- **AndroidX version resolution** between Avalonia.Android's and CameraX's transitive AndroidX packages.
  Mitigation: resolved at build; pinned explicitly in CPM if NuGet reports a downgrade.
- **The desktop control library on a phone.** `ContentDialog` / `InfoBar` sizing on a 360dp-wide screen is
  verified on the emulator.
- **No real camera in CI/emulator.** The emulator's virtual camera proves the pipeline runs; real-code decoding is
  proven by the Core frame tests and must be confirmed on a physical phone (stated in the completion doc).
