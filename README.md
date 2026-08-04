# Enigma.HardCopy

[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

Back up small critical files — private keys, KeePass databases, recovery seeds — **to paper**.

Enigma.HardCopy turns a file into a printable A4 PDF of QR codes, and recovers it later from scanned
images or from code contents typed in by hand, with end-to-end SHA-256 integrity verification. Each
printed backup also carries a plain-text description of its own format, so the data can be recovered
without this application.

Cross-platform desktop app (Windows + Linux), built with Avalonia.

> **What's new in 1.0** — the first release: both flows complete, self-contained artifacts for
> `win-x64` and `linux-x64`. See [RELEASENOTES.md](RELEASENOTES.md).

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
| Windows (x64) | `Enigma.HardCopy-1.0.0-win-x64.zip` | `Enigma.HardCopy.Desktop.exe` |
| Linux (x64) | `Enigma.HardCopy-1.0.0-linux-x64.zip` | `./Enigma.HardCopy.Desktop` |

On Linux, if the archive was produced on Windows the executable bit does not survive the zip — run
`chmod +x Enigma.HardCopy.Desktop` once after unpacking.

## Backing up a file

Open the **Backup** page.

1. **Choose the file.** Its size and SHA-256 appear immediately. Compare that hash against your own
   (`sha256sum <file>`, or `Get-FileHash <file>` on Windows) if you want to be sure the right bytes
   are going onto the paper.
2. **Pick the barcode density** (optional). *Medium* — 1024 bytes per code — is the default. *Small*
   makes each symbol sparser and easier to scan from a poor photograph at the cost of more pages;
   *Large* does the reverse. The setting applies to the current session only.
3. **Check the estimate.** The page shows how many codes and pages the backup will take. Past roughly
   twenty pages a warning appears — printing and re-scanning that many sheets is real work, and a
   larger density is usually the better answer.
4. **Choose the destination** and press **Generate PDF**. Long backups run in the background and can
   be cancelled; nothing is written if you do.

Then **print it, and check the print.** Every symbol must be sharp and complete — a smudged or clipped
code is a chunk you cannot recover, and no other code can reconstruct it. Store the sheets somewhere
you would store the original secret.

Each page carries the file name, the SHA-256, the date, the backup ID and its page number; page 1 also
carries the recovery instructions described below.

## Recovering a file

Open the **Recover** page. Both routes below feed **one** recovery session, so they can be mixed
freely: scan what scans, type in what does not. Codes may arrive in any order, and duplicates are
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
does not, **nothing is saved**: the app shows both hashes and requires an explicit **Save anyway**,
because bytes that fail their hash are not provably the file you backed up. Adding another code
withdraws that offer and re-checks from scratch.

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

Requires the .NET 10 SDK (pinned in `global.json`).

```
dotnet build Enigma.HardCopy.slnx
dotnet test --solution Enigma.HardCopy.slnx
dotnet run --project src/Enigma.HardCopy.Desktop
```

## Publishing the release artifacts

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

## Project layout

| Path | What it is |
|---|---|
| `src/Enigma.HardCopy.Core/` | The format, the encoder, QR rendering, PDF composition and the recovery pipeline. No UI. |
| `src/Enigma.HardCopy.Desktop/` | The Avalonia application. |
| `tests/` | The xUnit suites for both. |
| `docs/` | Roadmap, plans and per-phase completion records. The format specification lives in `docs/plan/FEATURE-79FF.md`. |

## Limitations

- One file per backup, and no encryption — if the file is a secret, the paper is the secret. Store it
  accordingly. (The format reserves an encryption flag; version 1 always writes it as `0` and refuses
  any backup that sets it.)
- A4 only.
- English only.
- Practical for kilobytes, not megabytes: roughly 12 KB per printed sheet at the default density.

## License

MIT — see [LICENSE.md](LICENSE.md).
