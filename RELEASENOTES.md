# Enigma.HardCopy Release Notes

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
