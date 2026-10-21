# Building and running HardCopy Recover (Android)

This guide gets the Android recovery app from the repository onto an Android screen: an emulator, your own phone
over USB, or a standalone APK. What the app does is described in the README's
[Recovering on Android](../README.md#recovering-on-android).

All commands run from the repository root, in PowerShell or Git Bash. The paths are the ones of the development
machine (an Android SDK installed by Visual Studio); [section 6](#6-what-was-verified) lists what was actually run.

## 1. Prerequisites

| What | Version | Check |
|---|---|---|
| .NET SDK | 10.0 (`global.json` rolls forward within 10.0) | `dotnet --version` |
| `android` workload | matching the SDK (36.x) | `dotnet workload list` |
| Android SDK | platform API 36, build-tools, platform-tools | comes with Visual Studio's *.NET MAUI* workload or Android Studio |
| JDK | 17 or later | comes with the same |

Install the workload once with `dotnet workload install android`. The whole solution needs it — `dotnet build
Enigma.HardCopy.slnx` builds the Android head too.

## 2. Build

```
dotnet build src/Enigma.HardCopy.Recover.Android -f net10.0-android
```

The app's own logic lives in `src/Enigma.HardCopy.Recover` and is tested on the desktop:
`dotnet test --solution Enigma.HardCopy.slnx` runs its suite with everything else, no device needed.

## 3. Run on an emulator

Create an x86_64 virtual device with **API 36** and a *Google APIs* image (Visual Studio: *Tools → Android → Android
Device Manager*; Android Studio: *Device Manager*). Set its **back camera to "Emulated"** or "VirtualScene" so the
app has a camera to open.

Start it. If the emulator reports *Cannot find AVD system path*, point it at the SDK that holds the system image —
with Visual Studio's SDK that is:

```powershell
$env:ANDROID_SDK_ROOT = "C:\Program Files (x86)\Android\android-sdk"
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd Pixel6_API36
```

Then build, install and keep the debugger-friendly Debug build in sync with:

```
dotnet build src/Enigma.HardCopy.Recover.Android -t:Install -f net10.0-android
```

and open **HardCopy Recover** from the emulator's launcher. An emulator cannot read printed codes; it proves the
app starts, asks for the camera and shows the preview.

## 4. Install on your phone

### Over USB (development)

1. On the phone: *Settings → About phone*, tap *Build number* seven times; then *Settings → System → Developer
   options → USB debugging* on.
2. Connect it, accept the debugging prompt, and check it is listed: `adb devices` (adb is in the SDK's
   `platform-tools` folder).
3. Run the same `-t:Install` command as above. With both an emulator and a phone connected, add
   `-p:AdbTarget=-d` to pick the phone.

### As a standalone APK

```
dotnet publish src/Enigma.HardCopy.Recover.Android -c Release -f net10.0-android -p:AndroidPackageFormat=apk
```

The APK is `src/Enigma.HardCopy.Recover.Android/bin/Release/net10.0-android/publish/io.github.enigmalibs.hardcopy.recover-Signed.apk`
(about 42 MB, for 64-bit ARM phones and x86_64 emulators). Install it with
`adb install <path-to-apk>`, or copy it to the phone and open it there — Android asks you to allow installing apps
from that source.

It is signed with the .NET SDK's local debug key: fine for your own phones, not for publishing. A store release
needs a real signing key (`-p:AndroidKeyStore=true` and the `AndroidSigning*` properties) and is out of scope for
this repository today.

## 5. Troubleshooting

| Symptom | Cause and fix |
|---|---|
| The app closes instantly after `adb install` of a **Debug** APK; logcat says *No assemblies found … Assuming this is part of Fast Deployment* | Debug builds keep their assemblies out of the APK and push them separately. Install Debug builds with `-t:Install`, or use the Release APK. |
| A screenshot or screen recording of the app is black | Deliberate: the app blocks screen capture, because the screen shows the secret being recovered. |
| *The camera permission was refused* and no prompt appears any more | Android stops asking after the permission was refused twice. Allow the camera in *Settings → Apps → HardCopy Recover → Permissions*. |
| On an emulator, the first *Start scanning* takes up to a minute | CameraX waits for every camera the device claims, and an AVD without a front camera makes it retry its enumeration. Emulator-specific. |
| A code does not read | Fill most of the preview with that one code, in even light, without glare from glossy paper. Very dense codes (the *Large* density) need the phone closer; a backup printed at *Small* density reads most easily. |
| *Code N belongs to backup ABCD, but this session is recovering backup WXYZ* | A page from another backup is in the pile — or the first code scanned was the stray one. Tap **New session** and start with a page of the right backup. |

## 6. What was verified

On the development machine (Windows 11, .NET SDK 10.0.402, `android` workload 36.1.118, Visual Studio's Android SDK,
emulator 36.5 with a `Pixel6_API36` x86_64 AVD whose back camera is *Emulated*):

- **Run:** `dotnet build src/Enigma.HardCopy.Recover.Android` (0 warnings);
  `dotnet build src/Enigma.HardCopy.Recover.Android -t:Install -f net10.0-android`; starting the emulator
  with `ANDROID_SDK_ROOT` set as above (headless, from Git Bash); `adb devices`; `adb install` of the Release APK;
  `dotnet publish … -c Release … -p:AndroidPackageFormat=apk` (0 warnings).
- **Observed:** the app starts on the emulator, asks for the camera on *Start scanning*, shows the preview once it
  is allowed and the refusal message when it is not; the Release APK requests the camera permission only.
- **Not run here:** `dotnet workload install android` (already installed), the emulator with a window, USB
  deployment to a physical phone (`-p:AdbTarget=-d`), and installing the APK by opening it on a phone. **Decoding
  printed codes with a real phone camera has not been observed yet** — the decoder is covered by unit tests on
  camera-style frames, but a first real-phone recovery of a printed backup is the test that matters.
