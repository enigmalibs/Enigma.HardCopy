# Enigma.HardCopy

[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

Back up small critical files — private keys, KeePass databases, recovery seeds — **to paper**.

Enigma.HardCopy turns a file into a printable A4 PDF of QR codes, and recovers it later from scanned
images or from code contents typed in by hand, with end-to-end SHA-256 integrity verification. Each
printed backup also carries a plain-text description of its own format, so the data can be recovered
without this application.

Cross-platform desktop app (Windows + Linux), built with Avalonia and the `Enigma.Avalonia.Desktop`
control library, with icons from `Enigma.Icons.Phosphor` — plus **HardCopy Recover**, an Android app that
recovers a backup by scanning its codes with the phone's camera and stores nothing on the phone (see
[Recovering on Android](#recovering-on-android)).

> **What's new in 1.3.0** — an **About** dialog, a **safety net** that reports an unexpected failure in a dialog
> while the app keeps running, the **new icon** on the desktop and the phone, a **Linux installer** and one-command
> **publish scripts**, and every dependency on its latest coherent version. **The format is untouched — every backup
> printed by 1.0, 1.1 or 1.2 still recovers.** See [RELEASENOTES.md](RELEASENOTES.md).

## Why paper

A disk fails silently, a cloud account gets locked, a passphrase is forgotten, a format stops being
readable. Paper in a drawer does none of that, and a QR code is readable by any phone made in the last
fifteen years. The trade is capacity: this is for the few kilobytes that unlock everything else, not
for your photo library.

## Install

Download the archive for your platform, or build it yourself (see *Build from source*). Each archive
expands into its own folder holding a single self-contained executable — **no .NET installation is
required on the machine that runs it**.

| Platform | Archive | Run |
|---|---|---|
| Windows (x64) | `Enigma.HardCopy-1.3.0-win-x64.zip` | `Enigma.HardCopy.Desktop.exe` |
| Linux (x64) | `Enigma.HardCopy-1.3.0-linux-x64.zip` | `./Enigma.HardCopy.Desktop` |

On Linux, if the archive was produced on Windows the executable bit does not survive the zip — run
`chmod +x Enigma.HardCopy.Desktop` once after unpacking.

### Installing on Linux

`packaging/linux/install.sh` installs the app for the current user — no root, nothing outside your home — with an
entry in the desktop's application launcher and an `enigma-hardcopy` command:

```
packaging/linux/install.sh                      # build from this repository (.NET runtime bundled) and install
packaging/linux/install.sh --from <folder>      # install an already-published folder instead
packaging/linux/install.sh --help               # every option: --framework-dependent, --rid
```

It writes `~/.local/share/enigma-hardcopy` (the application), `~/.local/bin/enigma-hardcopy`,
`~/.local/share/applications/enigma-hardcopy.desktop` and the icon in eight sizes, honouring `XDG_DATA_HOME` and
`XDG_BIN_HOME`. Run it again to upgrade in place. `packaging/linux/uninstall.sh` removes exactly that, and never
your settings in `~/.config/Enigma.HardCopy`.

The Android app, **HardCopy Recover**, is a single APK: `Enigma.HardCopy.Recover-1.3.0.apk` (Android 7.0 or later,
64-bit). Open it on the phone and allow installing apps from that source, or `adb install` it. It is signed with
the project's release key: if a build made from source is already on the phone, uninstall that first, since
Android refuses an update with a different signature. See [Recovering on Android](#recovering-on-android).

## Getting around

The window has a navigation rail down its left edge: **Backup** and **Recover**, with **Settings** at the
foot of it. Switching pages never disturbs the one you left — a chosen file, a half-fed recovery session and
text you have typed are all still there when you come back.

Long operations dim the window behind a card naming what is running and how far along it is. Every outcome —
a PDF written, a recovery completed, a code rejected — is announced in a bar across the top of the window,
while the detail stays on the page that owns it.

Should something fail that the application does not expect, a *Something went wrong* dialog says so and the
application keeps running; its collapsed *Details* hold what to paste into a bug report (*Copy details*).

## Backing up a file

Open the **Backup** page from the rail.

1. **Choose the file.** Its size and SHA-256 appear immediately. Compare that hash against your own
   (`sha256sum <file>`, or `Get-FileHash <file>` on Windows) if you want to be sure the right bytes
   are going onto the paper.
2. **Pick the barcode density** (optional). *Medium* — 1024 bytes per code — is the default. *Small*
   makes each symbol sparser and easier to scan from a poor photograph at the cost of more pages;
   *Large* does the reverse. The setting applies to the current session only.
3. **Check the estimate.** The page shows how many codes and pages the backup will take. Past roughly
   twenty pages a warning appears — printing and re-scanning that many sheets is real work, and a
   larger density is usually the better answer.
4. **Choose the destination** and press **Generate PDF**. A long backup runs behind a progress card that
   names the stage it has reached and offers **Cancel**; nothing is written if you cancel.

Then **print it, and check the print.** Every symbol must be sharp and complete — a smudged or clipped
code is a chunk you cannot recover, and no other code can reconstruct it. Store the sheets somewhere
you would store the original secret.

Each page carries the file name, the SHA-256, the date, the backup ID and its page number; page 1 also
carries the recovery instructions described below.

## Recovering a file

Open the **Recover** page from the rail. Both routes below feed **one** recovery session, so they can be
mixed freely: scan what scans, type in what does not. Codes may arrive in any order, and duplicates are
harmless.

### From scans or photographs

Press **Import images…** and select the scanned pages, or drag the image files anywhere onto the page.
PNG, JPEG and BMP are read, and every QR code found on a page is picked up at once — so one image per
printed sheet is enough. Photographs work if the codes are in focus and reasonably square-on.

### By typing the codes

Every code is plain text, so a symbol that will not scan can always be typed in by hand. Paste or type
one or more codes into the text box and press **Add codes**. Line breaks and lower case are both fine.

### Finishing

The **Progress** panel shows the backup ID, the recorded file name and expected hash, how many data
codes are in out of how many are needed, and — the useful part when you are standing at a scanner —
exactly which indexes are **still missing**. When they are all in, press **Recover file** and choose
where to save it.

The file is only reported as recovered when its SHA-256 matches the one recorded at backup time. If it
does not, **nothing is saved**: the app shows both hashes and requires an explicit **Save anyway**, which
then asks you to confirm — the dialog names both hashes again and the button reads **Save unverified** —
because bytes that fail their hash are not provably the file you backed up. Adding another code withdraws
that offer and re-checks from scratch.

**Start over** discards the session and its log. It asks first, if any codes have been read; the printed
pages are of course unaffected, and can be fed in again.

## Recovering on Android

**HardCopy Recover** (`Enigma.HardCopy.Recover.Android`) does the same recovery with a phone: no scanner, no
computer, and nothing left behind on the device. It looks like the desktop app — same theme, same colours, light
or dark as the phone is set. Install the release's APK (see [Install](#install)), or build it from source as
[docs/android.md](docs/android.md) describes.

1. **Start scanning.** The first time, Android asks for the camera — the only permission the app requests (it has
   no network access at all). Refuse it and the app says so; if Android stops asking, allow the camera for
   *HardCopy Recover* in the system settings.
2. **Hold the phone over the printed codes**, a page at a time. Every code in view is read, in any order. A tick
   in your hand confirms each new code; a different tick means one was refused. A code held in view is read
   once, however long it stays there.
3. **Watch the progress card**: *N of T data codes*, whether the metadata code (index 0, the one that names the
   file and holds its hash) is in, a bar that counts it too, and the indexes **still missing**, as ranges
   (`3, 5–8, 12`). The file name, size and expected SHA-256 appear as soon as the metadata code is read.
4. When the last code arrives the camera stops by itself. Tap **Save the recovered file** and choose where it
   goes: the phone's storage, **Google Drive**, or any other place Android's save dialog offers.

**Codes from a different backup are refused.** The session belongs to the backup of the first code it reads; a
code from any other backup is ignored with a warning naming both backup IDs. If the first code you scanned was
the stray one, tap **New session**. A damaged code, or a QR code that is not an Enigma.HardCopy code, is refused
the same way.

As on the desktop, the file is only reported as recovered when its SHA-256 matches. Otherwise nothing is
written; **Save anyway** asks you to confirm, naming both hashes, before it writes the unverified bytes.

**What stays on the phone: nothing.**

- The codes and the rebuilt file live in the app's memory only, until you start a new session or the app is
  closed. Nothing is written except the file you choose to save, where you choose to save it.
- The app keeps no settings and no log, and opts out of Android's app-data backup.
- **Screenshots and screen recordings of the app are blocked**, and the app switcher shows a blank card. Android
  would otherwise save a picture of the screen — codes, file name, hash — to its own storage.

**Pause scanning** turns the camera off and keeps every code read so far. **New session** forgets everything —
it asks first if any code has been read — and turns the camera straight back on.

**Checking a printout.** To prove a freshly printed backup before you put it away, tap **Check a printed backup**
instead of *Start scanning* and scan it the same way. When the last code arrives the app rebuilds the file in
memory, compares it with the SHA-256 recorded in the backup and shows the verdict — *Check passed* or *Check
failed* — then lets the bytes go: nothing is saved, and no save button appears. A code that will not scan keeps
the check from finishing, and **Still missing** names it; reprint that page, or generate the backup again at the
*Small* density. The first code settles what a session is for: *Resume scanning* carries on a paused check, and
**New session** after a check starts another check.

## Settings

The **Settings** page, at the foot of the rail, holds one choice: **Appearance** — *Follow the system*,
*Light* or *Dark*. It takes effect the moment you pick it and is remembered between runs. New installations
follow the system.

Below it, the **About** row names the running version; its *About Enigma HardCopy* button opens the About
dialog — the version, the build's short commit (selectable, for a bug report) and the copyright.

The choice is kept in a small JSON file, written only once you make one:

| Platform | Path |
|---|---|
| Windows | `%APPDATA%\Enigma.HardCopy\settings.json` |
| Linux | `~/.config/Enigma.HardCopy/settings.json` |

Delete it and the app goes back to following the system. Nothing in it can stop the app starting: a file
that is missing, damaged or written by a later version is reported in the log and replaced by the defaults.

The barcode density is deliberately *not* remembered — it is a per-backup decision, and a remembered *Large*
would quietly change how many sheets your next backup takes.

## Recovering without this application

The format is documented on the paper itself. Page 1 of every backup prints *"Recovering this backup
without Enigma.HardCopy"* — a complete description of how to rebuild the file with a Base32 decoder, a
CRC-32 routine and `gunzip`. Nothing in it depends on this program still existing, which is the whole
reason for printing to paper rather than into a proprietary container.

In short, each code is one line of ASCII:

```
EHC1:<BID>:<IDX>/<TOT>:<CRC>:<PAYLOAD>
```

`BID` is a four-character backup ID shared by every code of one backup; `IDX` 0 is the metadata code
and `IDX` 1..`TOT` are the data chunks; `CRC` is the CRC-32/ISO-HDLC of that chunk's decoded bytes; and
`PAYLOAD` is unpadded RFC 4648 Base32. Base32-decode each payload, concatenate the data chunks in index
order, `gunzip` if the metadata says so, and check the SHA-256. The printed page states the rest —
including the metadata key/value grammar and its two escapes.

## Build from source

Requires the .NET 10 SDK (pinned in `global.json`) and, because the solution includes the Android recovery app,
the .NET `android` workload (`dotnet workload install android`) with an Android SDK and a JDK.

```
dotnet build Enigma.HardCopy.slnx
dotnet test --solution Enigma.HardCopy.slnx
dotnet run --project src/Enigma.HardCopy.Desktop
```

Building, running and installing the Android app — on an emulator, on a phone over USB, or as a standalone APK —
is covered step by step in [docs/android.md](docs/android.md).

## Publishing the release artifacts

For a local build of the desktop app, one command per platform publishes it Release, ReadyToRun, for an installed
.NET 10 runtime, from any directory:

```
packaging/windows/publish.ps1         # Windows — or publish.bat from a cmd prompt (no execution policy)
packaging/linux/publish.sh            # Linux
```

Each runs `dotnet publish src/Enigma.HardCopy.Desktop -c Release -r <win-x64|linux-x64> -p:PublishReadyToRun=true`
and leaves its output in `src/Enigma.HardCopy.Desktop/bin/Release/net10.0/<rid>/publish/`. To install on Linux,
`packaging/linux/install.sh` (see *Installing on Linux*) builds and installs in one step.

The release archives are produced by a different pair of scripts:

```
scripts/publish.sh                    # Linux/macOS — every RID
scripts/publish.ps1                   # Windows — every RID
scripts/publish.sh linux-x64          # or just one
```

Either script produces identical results, so a release can be cut from either OS. Each RID is published
self-contained and single-file, then zipped into `artifacts/` along with this README and the licence.
The version comes from `<Version>` in `Directory.Build.props` — bump it there and the artifact names
follow.

`scripts/publish.sh` needs `zip`; `scripts/publish.ps1` uses PowerShell's built-in `Compress-Archive`.

The Android app has its own pair:

```
scripts/publish-android.sh            # Linux/macOS
scripts/publish-android.ps1           # Windows
```

Each writes `artifacts/Enigma.HardCopy.Recover-<version>.apk`, signed with the project's release key. The scripts
read the key from environment variables and refuse to build without it, so a debug-signed APK cannot be shipped
by mistake. Creating the key, the variables, and checking the result are covered in
[docs/android.md](docs/android.md), *Release APK*.

## Project layout

| Path | What it is |
|---|---|
| `src/Enigma.HardCopy.Core/` | The format, the encoder, QR rendering, PDF composition and the recovery pipeline. No UI. |
| `src/Enigma.HardCopy.Desktop/` | The Avalonia application. |
| `src/Enigma.HardCopy.Recover/` | The Android recovery app's platform-neutral half: its UI, ViewModels and seams. |
| `src/Enigma.HardCopy.Recover.Android/` | The Android recovery app's head: the activity, the manifest and the platform services. |
| `packaging/` | The icon's SVG masters; the one-command publish scripts (`windows/publish.ps1`, `windows/publish.bat`, `linux/publish.sh`); the Linux installer: `install.sh`, `uninstall.sh`, the launcher entry and its icons. |
| `tests/` | The xUnit suites for Core, the desktop app and the recovery app. |
| `docs/` | Roadmap, plans and per-phase completion records. The format specification lives in `docs/plan/FEATURE-79FF.md`; the Android build guide in `docs/android.md`. |

## Limitations

- One file per backup, and no encryption — if the file is a secret, the paper is the secret. Store it
  accordingly. (The format reserves an encryption flag; version 1 always writes it as `0` and refuses
  any backup that sets it.)
- A4 only.
- English only.
- Practical for kilobytes, not megabytes: roughly 12 KB per printed sheet at the default density.
- The Android app reads codes with the camera only: a code that will not scan cannot be typed in on the phone,
  and photos cannot be imported from the gallery — the desktop app does both. It runs on 64-bit phones
  (Android 7.0 or later) and is not published to any store.

## License

MIT — see [LICENSE.md](LICENSE.md).

The interface is built on `Enigma.Avalonia.Desktop` and the `Enigma.Icons` family; the icons themselves are
[Phosphor Icons](https://phosphoricons.com), by Phosphor Icons, used under the MIT licence. The Android app's
camera is driven by AndroidX CameraX, used under the Apache License 2.0.
