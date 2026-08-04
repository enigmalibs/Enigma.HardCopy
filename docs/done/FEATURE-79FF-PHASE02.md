# FEATURE-79FF-PHASE02 — Core encoding pipeline — DONE

**Branch:** `feature/feature-79ff-phase02-core-encoding` (cut from the PHASE01 branch, which is not yet merged)
**Date:** 2026-08-04

## Summary

Implemented the whole encoding half of the barcode format in `Enigma.HardCopy.Core`: a file goes in as a
stream, a list of code strings comes out. No QR rendering and no PDF — PHASE03 owns those — but the format
itself is now real, tested, and pinned by golden strings.

Twelve new types, all in the flat `Enigma.HardCopy.Core` namespace: the primitives (`Base32`, `Crc32`,
`Chunker`), the format codecs (`CodeHeader` + `HeaderCodec`, `BackupMetadata` + `BackupMetadataCodec`), the
encoder surface (`IBackupEncoder`/`BackupEncoder`, `EncodeOptions`, `ChunkSizePreset`, `EncodedBackup`) and
the injected entropy source (`IBackupIdGenerator`/`RandomBackupIdGenerator`).

Beyond the acceptance criteria, the format was **verified against a third-party implementation**: the codes
produced by `BackupEncoder` were fed to a Python script using nothing but the standard library
(`base64.b32decode`, `zlib.crc32`, `gzip`, `hashlib`) and recovered the original file bit for bit — codes
shuffled into random order, escaped non-ASCII file name intact, and a single flipped character correctly
reported as a CRC failure on the right chunk index. That is the promise the printed recovery-spec box will
make in PHASE03, so it is worth knowing it holds before the box is written.

## Files/modules touched

**Created — `src/Enigma.HardCopy.Core/`**

| File | What it is |
|---|---|
| `Base32.cs` | RFC 4648 unpadded codec. Tolerant decode (case, whitespace); strict on bits — rejects impossible symbol counts and non-zero trailing bits. |
| `Crc32.cs` | Table-driven CRC-32/ISO-HDLC. Same checksum as gzip, so a hand-recovery can verify a chunk with ordinary tools. |
| `Chunker.cs` | `CountChunks` + `Split`; returns slices, not copies. |
| `CodeHeader.cs` | `readonly record struct` (BackupId, Index, Total, Crc) + `IsMetadata`. |
| `HeaderCodec.cs` | `FormatHeader` / `FormatCode` / `TryParseCode` / `Normalize` — the single gate every code passes through. |
| `BackupMetadata.cs` | The metadata block's fields, one for one. |
| `BackupMetadataCodec.cs` | `\|`-separated `key=value` text with percent-escaping, unknown-key tolerance, duplicate rejection. |
| `EncodeOptions.cs` | Validated chunk size (16–2048 B) + `FromPreset`. |
| `ChunkSizePreset.cs` | Small/Medium/Large — the enum value *is* the byte count. |
| `EncodedBackup.cs` | BackupId + Metadata + Codes (index 0 = metadata). |
| `IBackupEncoder.cs` / `BackupEncoder.cs` | `EncodeAsync`: read → SHA-256 → compress-if-smaller → chunk → format. |
| `IBackupIdGenerator.cs` / `RandomBackupIdGenerator.cs` | 4 Base32 characters from `RandomNumberGenerator`. |

**Modified**
- `src/Enigma.HardCopy.Core/HardCopyFormat.cs` — added `BackupIdLength` (4) and `CrcHexLength` (8).
- `docs/plan/FEATURE-79FF.md` — PHASE02 → DONE, plus the value-escaping rule added to the format spec
  (see *Deviations* 1); `docs/roadmap.md` — PHASE02 → DONE.

**Created — `tests/Enigma.HardCopy.Core.UnitTests/`**
- `Base32Tests.cs`, `Crc32Tests.cs`, `ChunkerTests.cs`, `HeaderCodecTests.cs`,
  `BackupMetadataCodecTests.cs`, `EncodeOptionsTests.cs`, `BackupEncoderTests.cs`
- `TestDoubles/FixedBackupIdGenerator.cs`, `TestDoubles/FixedTimeProvider.cs`
- `HardCopyFormatTests.cs` — extended from 2 to 8 tests (the QR-charset subset relationships).

## Decisions taken at build time

1. **Metadata value escaping — asked, and answered by the user.** The plan's format spec defined no escaping
   rule, yet the acceptance criteria require exotic file names to round-trip, and a file name may legally
   contain the `|` separator. Chosen: percent-escaping, `%`→`%25` and `|`→`%7C`, decoder accepting any ASCII
   `%XX`. The rule is now recorded in the plan's *Barcode format specification* section, with a note that
   PHASE03's printed spec box must state it.
2. **`BackupMetadata` excludes the backup ID** (the plan's contract sketch listed it under `Metadata`). The ID
   is in every code's header, including the metadata block's own; keeping it in the payload too would have
   made `BackupMetadataCodec` asymmetric — `Format` would drop a field `TryParse` could never restore. It now
   lives on `EncodedBackup.BackupId`, and the codec round-trips exactly what it serializes.
3. **`EncodeAsync`, not `Encode`.** The plan's error strategy says all long operations are async with
   cancellation. Options are optional (`null` → `EncodeOptions.Default`).
4. **The encoder is an injected service, not a static class.** It needs a substitutable clock and entropy
   source for deterministic tests, so it takes `IBackupIdGenerator` + `TimeProvider` per the house DI rules.
   No `AddHardCopyCore` extension yet — PHASE05 owns registration, and Core stays free of a DI dependency.
5. **SHA-256 recorded in lower-case hex**, matching `sha256sum` output so it can be compared by eye during a
   hand-recovery. The parser accepts either case and normalizes.
6. **Compression uses `CompressionLevel.SmallestSize`** and is kept only when *strictly* smaller. Fewer pages
   is worth more than encode speed for files this size.
7. **An empty file is encodable**: `TOT=0`, one metadata code, `c=0`. Core stays mechanical; warning the user
   is the UI's job. PHASE04's `RecoverySession` must therefore treat "metadata present, 0 data chunks" as a
   complete backup.
8. **Base32 decoding is strict about padding bits.** `MZ` and `MY` both decode to `f`, but only `MY` leaves
   the trailing bits zero — so a mistyped final character is caught by the codec, before the CRC.
9. **Flat namespace, files at the project root.** The format types are shared by the encode and recovery
   sides, so a `Backup/`/`Recovery/` split would have cut across them; and a folder named `Encoding` would
   have collided with `System.Text.Encoding` under this solution's no-implicit-usings rule. PHASE03/PHASE04
   can still add folders for genuinely separable pieces (`Rendering/`, `Recovery/`).
10. **Chunk size is an `int` (16–2048 B) with the three presets as an enum**, rather than presets only. The
    ceiling is the alphanumeric capacity of a version-40 QR symbol at ECC M (3391 characters); a test asserts
    the longest possible code still fits it, so the encoder cannot produce a chunk PHASE03 can't render. The
    floor lets tests exercise multi-chunk paths with tiny fixtures.

## Deviations & follow-ups

1. **The plan file's format spec was extended, not just consumed** — the escaping rule of decision 1 was
   written into `docs/plan/FEATURE-79FF.md` because PHASE03 and PHASE04 both depend on it and a completion
   doc is the wrong place for a format contract.
2. **`BackupEncoder` does not move work off the calling thread.** Choosing a thread is the caller's business,
   so PHASE05 must invoke `EncodeAsync` from a background task rather than directly in a command handler —
   past a few hundred kilobytes the CPU-bound part (gzip, Base32) would otherwise stall the UI thread.
3. **`fileName` is not validated as a leaf name.** Core stores what it is given; `Path.GetFileName` is
   PHASE05's responsibility at the file-picker boundary. Validating here would have meant
   platform-dependent behaviour (`\` is legal in a Linux file name and a separator on Windows).
4. **`HeaderCodec.TryParseCode` reports only "structurally valid or not".** It deliberately does not decode
   the payload or check the CRC, because PHASE04 must distinguish `Malformed` from `BadCrc`. The CRC check is
   one call away (`Crc32.Compute(Base32.Decode(payload))`), as `BackupEncoderTests.Reassemble` demonstrates.
5. **`RandomBackupIdGenerator` has no test of its own** beyond being exercised indirectly — asserting
   properties of a cryptographic RNG would be testing the BCL. Its output shape is covered by
   `HeaderCodec`'s backup-ID validation, which every code passes through.
6. **Line endings:** nothing to report — every new file was authored LF, and `.gitattributes`
   (`* text=auto eol=lf`) has been in place since the first commit. No action taken, per the workflow's
   recommendation-only rule.
7. **PHASE01 is still unmerged.** This branch stacks on `feature/feature-79ff-phase01-scaffolding`, which is
   one commit ahead of `main`/`develop`. Nothing is wrong with that — the skill branches from `HEAD` — but
   the two phases will need merging in order.

## Build/test evidence

```
dotnet build Enigma.HardCopy.slnx              → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build Enigma.HardCopy.slnx -c Release   → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --solution Enigma.HardCopy.slnx   → Passed! total: 255  failed: 0  succeeded: 255  skipped: 0
```

Zero warnings holds under `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` + the full `.editorconfig`,
and under `GenerateDocumentationFile` on Core — every new public type and member is XML-documented.

**255 tests, up from 4** (Core 253, Desktop 2). Every test vector was verified against an independent source
before being asserted: the RFC 4648 section 10 Base32 vectors and the published CRC-32/ISO-HDLC check values
(including `0xCBF43926` for `"123456789"`) were cross-checked with Python's `base64`/`zlib` first, so a
failing test would indict the implementation rather than the expectation.

Notable coverage beyond the plan's list:
- `Crc32Tests.Compute_MatchesTheCrcGzipStoresInItsTrailer` — proves the checksum really is gzip's, which is
  what makes the app-less recovery instructions honest.
- `EncodeOptionsTests.MaxChunkSizeInBytes_FitsTheLargestQrSymbolAtEccM` — the encoder cannot emit a chunk
  that PHASE03 would be unable to render.
- `EncodeAsync_CodesReassembleToTheOriginalFile` — 6 cases (3 presets × compressible/incompressible, 100 KB
  random and 88 KB text) walking the exact path a recovery will: parse → Base32-decode → verify CRC →
  concatenate → gunzip → compare bytes and SHA-256.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `Base32` codec (RFC 4648, unpadded, tolerant decode: uppercase + whitespace strip) | ✅ 24 tests, RFC vectors + round-trips over 45 buffer lengths |
| 2 | `Crc32` (IEEE), no external package | ✅ 11 tests, published check values + gzip-trailer agreement |
| 3 | `Chunker`, `HeaderCodec`, `BackupMetadata` + `\|`-separated serializer (unknown-key-tolerant parser) | ✅ |
| 4 | gzip compress-if-smaller; SHA-256 of original bytes | ✅ both branches asserted, hash compared against `SHA256.HashData` |
| 5 | `BackupEncoder.Encode` composing the above → `EncodedBackup` | ✅ as `EncodeAsync` (deviation 3 above) |
| 6 | Tests: codec round-trips, header golden strings, CRC vectors, metadata round-trip incl. exotic filenames, compress/no-compress branch, chunk-count math, charset invariant | ✅ all seven, plus the reassembly round trip |
| 7 | Overall criteria 3–4 at the string level; zero-warning build, tests green | ✅ out-of-order/duplicate-free indexing, CRC-detected corruption, wrong-prefix rejection, gzip flag both ways; 0 warnings, 255/255 |

Overall criterion 3's *wrong-backup-ID* and *duplicate* scenarios are covered here only as far as the
string layer reaches (a foreign ID parses into a `CodeHeader` whose `BackupId` differs; duplicate detection
is a session concern). The session-level behaviour belongs to PHASE04's `RecoverySession`, as the plan
specifies.
