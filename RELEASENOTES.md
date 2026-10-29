# Enigma.HardCopy Release Notes

## 1.3.0

A release that brings the desktop app in line with the other Enigma apps — the same About dialog, the same safety
net for unexpected failures, the same packaging — under a new icon, with every dependency on its latest coherent
version. **The format is untouched: every backup printed by 1.0, 1.1 or 1.2 recovers, on the desktop and on the
phone alike.**

### About Enigma HardCopy

- **A new About row on the Settings page** names the running version. Its *About Enigma HardCopy* button opens the
  About dialog every Enigma app shares: the icon, the name, the version, the build's short commit — selectable, to
  paste into a bug report, and absent for a build made outside the repository — and the copyright.

### When something unexpected fails

- **A safety net catches what nothing else handles** — an exception from a button, from work running in the
  background, or from work nobody waits for.
- **It says so in a *Something went wrong* dialog**, and the application keeps running. The dialog gives the error,
  counts any that followed it ("2 more errors occurred."), and keeps the details — type, message and stack, nothing
  else — collapsed, with *Copy details* for a bug report.
- **It never covers a question on screen**: it waits for the question to be answered. A burst of failures opens one
  dialog, and the same failure is not shown again for a minute after its dialog was closed.
- **Every such error is logged** in full. The failures the application already knows about — a file it cannot read
  or write, a code it rejects — are reported where they happen, exactly as before.

### A new icon

- **The new Enigma.HardCopy icon** — a QR code in soft blue with one green module, on a dark rounded tile — replaces
  the old one in the desktop app (window, taskbar, executable) and on the phone's launcher. The Android icon also
  has a themed (monochrome) layer for launchers that tint icons.
- **`app.ico` now sits beside the executable** in every build and publish folder, release archives included, for
  shortcuts and installers to point at.

### Installing and publishing

- **A Linux installer.** `packaging/linux/install.sh` installs the app for the current user into the XDG
  directories, with a launcher entry, the icon in eight sizes and an `enigma-hardcopy` command — no root, nothing
  outside your home. It builds from the repository (bundling the .NET runtime, or `--framework-dependent`) or
  installs an already-published folder (`--from`). `packaging/linux/uninstall.sh` removes exactly that and never
  touches your settings.
- **One-command publish scripts.** `packaging/windows/publish.ps1`, `packaging/windows/publish.bat` and
  `packaging/linux/publish.sh` publish the desktop app Release, ReadyToRun, for win-x64 or linux-x64, from any
  directory. The release archives are still produced by `scripts/publish.ps1` / `scripts/publish.sh`, unchanged.

### Compatibility — the paper is unchanged

- **The backup format is the same EHC1 format.** A PDF printed by 1.3 recovers with 1.0, 1.1 and 1.2, and the other
  way round; the recovery instructions printed on page 1 are unchanged.
- **The settings file is unchanged** (`settings.json`, the appearance choice).
- **HardCopy Recover's only visible change is its icon.** It is built and signed exactly as 1.2.0
  (`scripts/publish-android.*`, the project's release key), so it installs as an update over a release-signed 1.2.0;
  its `versionCode` is now `10300`.

### For developers

- **The tests run on xunit.v3 4** (Microsoft Testing Platform v2), and coverage comes from
  `Microsoft.Testing.Extensions.CodeCoverage`: `dotnet test --solution Enigma.HardCopy.slnx --coverage`.
  `coverlet.collector` is gone.
- **New `packaging/` folder:** the icon's SVG masters, the publish scripts and the Linux installer.
- **The suite grows** from 796 to 875 tests.

### Dependencies

**Updated:**

- The Avalonia set (`Avalonia`, `Avalonia.Android`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`,
  `Avalonia.Fonts.Inter`) 12.1.1 → 12.1.4, with `Enigma.Avalonia.Desktop` 1.0.0 → 1.3.0
- `QuestPDF` 2026.7.2 → 2026.9.1 — the PDF composer uses the renamed glyph setting; a printed backup is laid out
  exactly as before
- `Microsoft.Extensions.Hosting`, `.Logging.Console`, `.Logging.Debug` 10.0.13 → 10.0.14
- `xunit.v3` 3.2.2 → 4.0.2 (tests only)
- `coverlet.collector` 10.1.0 → replaced by `Microsoft.Testing.Extensions.CodeCoverage` 18.12.0 (tests only)

**Deliberately held back:**

- `SkiaSharp` / `SkiaSharp.NativeAssets.Linux` stay at 3.119.4 (4.154.0 is out), and with them
  `ZXing.Net.Bindings.SkiaSharp` at 0.16.22 (0.16.25 needs SkiaSharp 4). Avalonia 12.1.4, the newest, is built on
  SkiaSharp 3.119.4, and the app needs one Skia runtime.
- CameraX (`Xamarin.AndroidX.Camera.Camera2` / `.Core` / `.Lifecycle`) stays at 1.6.1 (1.6.2 is out): 1.6.2 moves
  AndroidX Lifecycle to 2.11, while `Avalonia.Android` 12.1.4 still pins 2.10.

No package in the solution has a known vulnerability, direct or transitive.

### Known limitations

- The About dialog and the safety net are the desktop app's; HardCopy Recover has neither yet.
- The Linux installer installs for the current user only; there is no distribution package (`.deb`, `.rpm`,
  Flatpak, AppImage).

---

## 1.2.0

The first release with **HardCopy Recover**, an Android app that recovers a backup with the phone's camera —
and that can also check a freshly printed backup before it is filed away. **The format is untouched: every
backup printed by 1.0 or 1.1 recovers, on the phone and on the desktop alike.** The desktop app works exactly
as it did in 1.1.0.

### HardCopy Recover for Android

- **Recover with the camera.** Point the phone at the printed pages: every code in view is read, in any order,
  a code held in view is read once, and a tick in your hand confirms each new one.
- **Codes from another backup are refused**, with a warning naming both backup IDs. Damaged codes and QR codes
  that are not Enigma.HardCopy codes are refused too, each with a different tick.
- **Progress as on the desktop:** *N of T data codes*, whether the metadata code is in, a bar that counts it,
  the indexes still missing as ranges, and the file name, size and expected SHA-256 once known. The camera
  stops by itself when the last code arrives.
- **Save anywhere Android can.** *Save the recovered file* opens Android's own save dialog — the phone's
  storage, Google Drive, or any other document provider. The file is reported as recovered only when its
  SHA-256 matches. On a mismatch nothing is written, and *Save anyway* asks first, naming both hashes.
- **Check a printed backup.** A second way in: scan a printout exactly as for a recovery. Once every code is in,
  the app rebuilds the file in memory, compares it with the recorded SHA-256 and shows *Check passed* or *Check
  failed*. Then it lets the bytes go: nothing is saved. A code that will not scan keeps the check from
  finishing, and the missing list names it.
- **Nothing stays on the phone.**
  - The codes and the rebuilt file live in memory only, until a new session or the app is closed.
  - There are no settings, no log and no cache, and the app opts out of Android's app-data backup.
  - Screenshots and screen recordings of the app are blocked.
  - Its only permission is the camera; it has no network access.
- **It looks like the desktop app** — the same theme, light or dark as the phone is set.

### Installing it

- **Android 7.0 or later, 64-bit.** The APK carries `arm64-v8a` for phones and `x86_64` for emulators and
  Chromebooks.
- **It is not in any store.** Download `Enigma.HardCopy.Recover-1.2.0.apk` from the release, open it on the
  phone, and allow installing apps from that source — or `adb install` it.
- **It is signed with the project's release key.** The release page gives the signing certificate's SHA-256
  digest, which `apksigner verify --print-certs` shows for the downloaded file. Later releases are signed with
  the same key and install over this one as updates.
- **A phone with a build made from source before this release must uninstall it first.** That build carries a
  different signature, and Android refuses the update (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`). Nothing is lost:
  the app stores nothing.

### Compatibility — the paper is unchanged

- The `EHC1` code grammar, the metadata grammar, the recovery instructions printed on page 1, the A4 layout and
  the SHA-256 verification are exactly those of 1.0 and 1.1.
- **A backup printed by any of 1.0, 1.1 and 1.2 recovers in all three**, and on the phone.
- The desktop app's source is untouched since 1.1.0; it moves to 1.2.0 so that every artifact of the release
  carries one number, and with the dependency patches below.
- `Enigma.HardCopy.Core` gains one public method, nothing else:
  `ImageDecoder.ReadCodes(byte[] luminance, int width, int height, int rowStride)`. It reads every QR code in
  an 8-bit luminance frame, such as a camera's Y plane.

### For developers

- **Three new projects:**
  - `src/Enigma.HardCopy.Recover` (net10.0) — the app's UI, ViewModels and seams, tested on the desktop;
  - `src/Enigma.HardCopy.Recover.Android` (net10.0-android) — the Android head;
  - `tests/Enigma.HardCopy.Recover.UnitTests`.
- **A full solution build now needs** the .NET `android` workload, an Android SDK and a JDK.
- **The release APK is built by** `scripts/publish-android.sh` or `scripts/publish-android.ps1`.
  - They sign it with the release key, read from environment variables, and refuse to build without it.
  - The APK's `versionCode` derives from `<Version>` (major × 10000 + minor × 100 + patch — `10200` here).
  - `docs/android.md` covers the key, the variables and the checks.
- **The suite grows** from 682 to 796 tests.

### Dependencies

**New, Android app only:**

| Package | Version | What it is |
|---|---|---|
| `Avalonia.Android` | 12.1.1 | Avalonia's Android platform, in the same coupled set as the rest of Avalonia (MIT) |
| `Xamarin.AndroidX.Camera.Camera2` / `.Core` / `.Lifecycle` | 1.6.1 | AndroidX CameraX — frames only, decoded in-process by ZXing; no barcode SDK and no Play Services (MIT bindings over Apache-2.0) |

**Updated:**

- `QRCoder` 1.8.0 → 1.8.1
- `Microsoft.Extensions.Hosting`, `.Logging.Console`, `.Logging.Debug` 10.0.10 → 10.0.13
- `coverlet.collector` 10.0.1 → 10.1.0 (tests only)

**Deliberately held back:**

- `QuestPDF` stays at 2026.7.2 (2026.9.1 is out). 2026.9 renames a setting the PDF composer relies on, and the
  engine that lays out the paper gets its own release and its own look at the printed PDF.
- `ZXing.Net.Bindings.SkiaSharp` stays at 0.16.22. 0.16.24 needs SkiaSharp 4, which is coupled to Avalonia.
- The Avalonia set stays at 12.1.1 (12.1.4 is out). It moves as a unit, with `Enigma.Avalonia.Desktop`
  (1.0.0; 1.2.0 is out), SkiaSharp (3.119.4; 4.153.1 is out) and CameraX (1.6.1; 1.6.2 moves AndroidX
  Lifecycle past what `Avalonia.Android` 12.1.1 pins).
- `xunit.v3` stays at 3.2.2. 4.0.2 is a migration, not a bump.

No package in the solution has a known vulnerability, direct or transitive.

### What the APK weighs and what it ships

- **The APK is 42.2 MB** (42,231,208 bytes) for both ABIs.
- **License audit.** Its runtime dependency graph holds 135 packages, all redistributable:
  - MIT — Avalonia, Microsoft.Extensions, QRCoder, the house `Enigma.*` packages and others;
  - Apache-2.0 — ZXing.Net and Google's AndroidX/CameraX libraries;
  - one BSD-3-Clause component, inside CameraX Core;
  - QuestPDF's managed assembly, under the Community license the desktop app has used since 1.0. The phone never
    composes a PDF, so it is dead weight; its native libraries are already excluded, and dropping the assembly
    too is a known follow-up.

### Known limitations

- The phone reads codes with the camera only. A code cannot be typed in and photos cannot be imported — the
  desktop app does both.
- Very dense codes (the *Large* density) need the phone closer; a backup printed at *Small* density reads most
  easily.
- English only; portrait only; sideloaded, not in a store.

---

## 1.1.0

A redrawn application on the same format. **Every backup printed by 1.0 still recovers, byte for byte** —
nothing about the paper, the codes or the PDF changed. What changed is the program you feed them into.

### The application is now built on Enigma.Avalonia.Desktop

The user interface is rebuilt on the house control library and its Fluent theme, so this application and the
rest of the family look and behave alike, and so the app inherits the library's controls and services instead
of maintaining private equivalents of them.

- **A navigation rail instead of tabs.** *Backup* and *Recover* sit on a vertical rail down the left edge,
  each with an icon; *Settings* sits in the rail's footer. Leaving a page and coming back does not disturb it:
  a chosen file, a half-fed recovery session and typed-in text are all still there.
- **A new Settings page**, reached from the rail's footer.
- **An appearance preference — *Follow the system* / *Light* / *Dark*.** It applies the moment it is chosen,
  repainting the whole window, and it is remembered between runs. The default is to follow the operating
  system, which is what every 1.0 build did. The barcode density is deliberately *not* remembered: it stays a
  per-session choice, so every backup starts from the density the page layout is tuned for.
- **Long operations now run behind a progress overlay** carrying the operation's name and its live stage text
  — generating a backup, importing scanned pages, rebuilding a file and saving one alike. Generating a backup
  can still be cancelled from the card; the other three have no cancel button, because they never had
  cancellation to offer.
- **Outcomes are announced in an information bar** at the top of the window, at the severity that fits.
  The detail stays on the page that owns it: the recovery activity log, the list of still-missing indexes,
  the large-input warning and the metadata-missing warning are all exactly where they were.
- **Two in-window confirmations.**
  - *Save bytes that failed their check?* — asked before writing a recovered file whose SHA-256 does not
    match the one the backup recorded. It shows both hashes and requires **Save unverified**. In 1.0 this
    irreversible action was guarded only by a button standing next to a warning.
  - *Discard this recovery?* — asked when starting over on a session that already holds codes.

  Neither dialog carries any code text: hashes and prose only.

### Compatibility — the paper is unchanged

This release touches no part of the format. Specifically, all of the following are byte-for-byte identical
to 1.0:

- the `EHC1:<BID>:<IDX>/<TOT>:<CRC>:<PAYLOAD>` code grammar and its Base32 payload encoding;
- the metadata code's key/value grammar and its two escapes;
- the recovery instructions printed on page 1 of every backup, which therefore remain true;
- the A4 page layout, the page furniture and the QR rendering;
- the SHA-256 verification at both ends.

**A backup printed by 1.0 recovers in 1.1.0, and a backup printed by 1.1.0 recovers in 1.0.** No re-printing
is required or useful. `Enigma.HardCopy.Core` — the encoder, the PDF composer and the recovery pipeline — is
unchanged in this release.

### Settings file

The appearance preference is stored in a small JSON file, written only when a choice is made:

| Platform | Path |
|---|---|
| Windows | `%APPDATA%\Enigma.HardCopy\settings.json` |
| Linux | `~/.config/Enigma.HardCopy/settings.json` |

It is purely additive — **its absence is normal** and is what a first run looks like. A file that is missing,
unreadable, malformed, naming an appearance this version does not have, or written by a later version all
resolve to the same thing: the defaults, a line in the log, and an application that opens. Nothing in this
file can prevent the application from starting, and deleting it returns the application to following the
operating system.

### New dependencies, and what they cost

| Package | Version | What it is |
|---|---|---|
| `Enigma.Avalonia.Desktop` | 1.0.0 | the control library: theme, navigation rail, dialogs, overlay, information bar, settings cards, editors |
| `Enigma.Icons` | 1.0.0 | icon core |
| `Enigma.Icons.Phosphor` | 1.0.0 | the Phosphor glyph set ([Phosphor Icons](https://phosphoricons.com), MIT) |
| `Enigma.Icons.Avalonia` | 1.0.0 | the Avalonia `Icon` control and its XAML markup extensions |

They bring `Enigma.Core` and `BouncyCastle.Cryptography` in transitively. Nothing in this application calls
either — they back the control library's binary editors — and there is no way to opt out short of not
referencing the library.

**The artifacts grow by about 9 MB per platform, roughly 7 %:** the linux-x64 stage measures 129 MB unpacked
against 1.0.0's 120 MB, and 55 MB zipped against 52 MB. That is Phosphor's glyph data (~3.9 MB), BouncyCastle
(~4.7 MB) and the library plus the two icon assemblies (~0.3 MB).

The Avalonia set does **not** move in this release: the control library pins Avalonia 12.1.1, the Fluent theme
12.1.1 and CommunityToolkit.Mvvm 8.4.2 — exactly the versions 1.0.0 already carried. No package in the
solution has a known vulnerability, direct or transitive.

### Unchanged

Everything the application is *for* works as it did: hashing and estimating before generating, cancelling a
long backup, mixing scanned images with hand-typed codes in one session, rejecting duplicates and codes from a
foreign backup, catching damaged chunks by CRC and naming the indexes to re-scan, and refusing to write a file
whose hash does not match. One file per backup, no encryption, A4 only, English only.

---

## 1.0.0

The first release of **Enigma.HardCopy** — a desktop application that backs up a small critical file to
paper as a PDF of QR codes, and recovers it later from scans or from typed-in code text, with SHA-256
verification at both ends. The format is deliberately self-describing: every backup prints a complete
account of its own encoding, so the file can be rebuilt with a Base32 decoder, a CRC-32 routine and
`gunzip` if this program is ever unavailable.

### Feature overview

- **Backup** — any file becomes a printable A4 PDF: gzip applied only when it actually helps, chunked
  at a chosen density (512 / 1024 / 1536 bytes per code), each chunk carrying its own CRC-32, plus a
  metadata code holding the file name, size, SHA-256, compression flag and date. Page furniture on
  every sheet (file name, hash, date, backup ID, page *n* of *m*) and an index caption under every
  symbol. Page and code estimates before you commit, with a warning on large inputs.
- **Recovery** — scanned images and hand-typed codes feed one session, in any order, mixed freely.
  Every QR code on a page is read at once; duplicates are ignored, chunks from a different backup are
  rejected by backup ID, damaged ones are caught by CRC and reported with the index to re-scan. Live
  progress names exactly which indexes are still missing.
- **Verification** — a recovery is only reported as successful when the rebuilt file's SHA-256 matches
  the one recorded at backup time. On a mismatch nothing is written: both hashes are shown and an
  explicit *Save anyway* is required.
- **Self-describing paper** — page 1 of every backup prints the full format specification, including
  the metadata grammar and its escapes.
- **Cross-cutting** — long operations run in the background and can be cancelled; every user-facing
  string is read from a `.resx`.

### Compatibility

- Targets **net10.0**.
- Ships self-contained for **win-x64** and **linux-x64** — no .NET installation is needed on the
  machine that runs an artifact.
- Built on **Avalonia 12.1.1**, **QuestPDF 2026.7.2**, **QRCoder 1.8.0**, **ZXing.Net 0.16.11** and
  **SkiaSharp 3.119.4**.

### Known limitations

- One file per backup; no encryption in version 1 (the format reserves the flag and refuses any backup
  that sets it).
- A4 output only, English UI only.
- Roughly 12 KB per printed sheet at the default density — intended for the few kilobytes that unlock
  everything else.
