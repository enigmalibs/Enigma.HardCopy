# Enigma.HardCopy v1.0.0 Release Notes

The first release of **Enigma.HardCopy** — a desktop application that backs up a small critical file to
paper as a PDF of QR codes, and recovers it later from scans or from typed-in code text, with SHA-256
verification at both ends. The format is deliberately self-describing: every backup prints a complete
account of its own encoding, so the file can be rebuilt with a Base32 decoder, a CRC-32 routine and
`gunzip` if this program is ever unavailable.

## Feature overview

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

## Compatibility

- Targets **net10.0**.
- Ships self-contained for **win-x64** and **linux-x64** — no .NET installation is needed on the
  machine that runs an artifact.
- Built on **Avalonia 12.1.1**, **QuestPDF 2026.7.2**, **QRCoder 1.8.0**, **ZXing.Net 0.16.11** and
  **SkiaSharp 3.119.4**.

## Known limitations

- One file per backup; no encryption in version 1 (the format reserves the flag and refuses any backup
  that sets it).
- A4 output only, English UI only.
- Roughly 12 KB per printed sheet at the default density — intended for the few kilobytes that unlock
  everything else.

## Version

- Initial release: **1.0.0**.
