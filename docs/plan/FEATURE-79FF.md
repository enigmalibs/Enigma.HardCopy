# FEATURE-79FF — Enigma.HardCopy v1 — paper backup & recovery

**Status:** IN PROGRESS
**Type:** multi-phase feature (6 phases)
**Branches (created by `/build`, one per phase):** `feature/feature-79ff-phaseNN-<slug>`

## Objective

A cross-platform desktop application (Windows + Linux) that backs up small critical files
(private keys, KeePass databases, …) to paper via a generated PDF of QR codes, and recovers
them later by importing scanned images or by manually typing the scanned code contents —
with end-to-end SHA-256 integrity verification.

## Validated decisions (interview 2026-07-30)

- **Barcode:** QR code; ECC level M by default (build-tunable constant).
- **Payload encoding:** Base32 (RFC 4648, **unpadded** — `=` is not in the QR alphanumeric set);
  the entire QR content stays within the QR *alphanumeric mode* charset (`0–9 A–Z $ % * + - . / :`)
  for ~10% overhead vs raw binary and easy manual typing.
- **Scope:** one file per PDF; no encryption in v1 (metadata flag reserved, always `0`).
- **Metadata:** dedicated barcode index 0; slim per-chunk headers.
- **Chunk integrity:** CRC-32 per chunk in every header.
- **Chunk size:** ~1 KB default, adjustable (small 512 B / medium 1024 B / large 1536 B).
- **Compression:** automatic — gzip the file, keep it only if smaller, flag in metadata.
- **Libraries:** QuestPDF 2026.7.2 (Community license — `QuestPDF.Settings.License = LicenseType.Community`
  must be set at startup), QRCoder 1.8.0 (generation), ZXing.Net 0.16.11 + SkiaSharp binding (decoding,
  multi-code per image), CommunityToolkit.Mvvm (explicit style), Avalonia 12.1.1.
- **Output:** A4 only; full page furniture + printed "recovery without this app" spec box on page 1.
- **UI:** plain Avalonia Fluent theme, stock controls only (user decision — no Carbon/Phosphor);
  English only, all user-facing strings routed through `.resx` from the start.
- **Packaging:** self-contained single-file publish for `win-x64` and `linux-x64`, zipped; no installer, no CI.
- **License:** MIT.

### Assumptions accepted at validation

Two test projects (`Core.Tests`, `Desktop.Tests`); metadata payload serialized as compact text and
Base32-encoded like any chunk (uniform CRC handling); index 0 = metadata, `total` = data-chunk count;
drag-&-drop images onto the Recover view; image formats PNG/JPEG/BMP; "save anyway" override (with
strong warning) on hash mismatch; settings session-only; Microsoft.Extensions.Logging console/debug
only; placeholder app icon (final `.ico` art supplied by the user); no hard file-size cap — page-count
estimate + warning for large inputs.

## Barcode format specification (v1 draft — the build contract)

Every QR code contains one ASCII string, all characters within the QR alphanumeric set:

```
EHC1:<BID>:<IDX>/<TOT>:<CRC>:<PAYLOAD>
```

| Field | Definition |
|---|---|
| `EHC1` | Magic `EHC` + format version `1`. Bump the digit on any breaking format change. |
| `BID` | Backup ID: 4 random Base32 chars (20 bits), generated per backup; prevents mixing scans from different backups. |
| `IDX` | Decimal, unpadded. `0` = metadata block; `1`..`TOT` = data chunks. |
| `TOT` | Decimal, unpadded: number of **data** chunks (metadata block excluded). |
| `CRC` | CRC-32 (IEEE, poly 0xEDB88320) of the **decoded chunk bytes**, as 8 uppercase hex chars. |
| `PAYLOAD` | Unpadded RFC 4648 Base32 of the chunk bytes. Decoder normalizes input: uppercase, strip whitespace. |

**Metadata block (IDX 0) payload** — decoded bytes are UTF-8 text, `|`-separated `key=value` pairs:

```
v=1|n=<filename>|s=<original size bytes>|h=<sha256 hex of ORIGINAL file>|c=<0|1 gzip>|e=0|z=<chunk size bytes>|d=<yyyy-MM-dd>
```

`c=1` means the data chunks reassemble to a gzip (RFC 1952) stream — gzip chosen over raw deflate so a
hand-recovery with standard tools (`gunzip`) works. `e` is the reserved encryption flag, always `0` in v1.
Unknown keys must be ignored by the decoder (forward compatibility).

**Capacity check (default chunk):** 1024 B → 1639 Base32 chars + ~27 header chars ≈ 1666 → fits QR
version 25 at ECC M (1708 alphanumeric). Rendered at ≥ 0.35 mm/module, ~44 mm per symbol → roughly
3×5 = 15 codes per A4 page ≈ 15 KB/page. Exact grid/version tuning happens in PHASE03.

## Architecture

```
Enigma.HardCopy.slnx            (.slnx format, .NET 10 SDK; global.json pins SDK + MTP test runner)
Directory.Build.props           (LangVersion 14, nullable, TreatWarningsAsErrors, no implicit usings, authors)
Directory.Packages.props        (Central Package Management; Avalonia ecosystem bumped as a unit)
.editorconfig / .gitignore / .gitattributes   (house dotnet-solution-config + git-repo-hygiene templates)
LICENSE (MIT) / README.md
src/Enigma.HardCopy.Core/       net10.0 library (TFM deviation from netstandard2.0 documented in csproj:
                                QuestPDF requires .NET 8+; only consumers are this app and a future CLI)
src/Enigma.HardCopy.Desktop/    net10.0 Avalonia 12 app, IHost bootstrap per house avalonia skill
tests/Enigma.HardCopy.Core.UnitTests/     xUnit v3, OutputType Exe
tests/Enigma.HardCopy.Desktop.UnitTests/  xUnit v3 — ViewModel tests
                                (renamed from *.Tests in PHASE01: the house xunit-v3 skill mandates
                                the <ProjectUnderTest>.UnitTests suffix — user decision at build)
docs/roadmap.md · docs/plan/ · docs/done/
```

### Core public API (contract sketch — refine, don't reshape, at build)

```csharp
// Encoding side
BackupEncoder.Encode(Stream file, string fileName, EncodeOptions options) -> EncodedBackup
//   EncodedBackup: Metadata (filename, size, sha256, compressed, chunkSize, backupId, date),
//                  IReadOnlyList<string> Codes  (index 0 = metadata code)
PdfComposer.Compose(EncodedBackup backup, PdfOptions options) -> byte[]   // A4 QuestPDF document

// Recovery side
RecoverySession                       // accepts codes in any order, mixable sources
  AddCode(string text) -> AddCodeResult        // Accepted | Duplicate | BadCrc(index) | WrongBackup | Malformed
  AddImage(Stream image) -> ImageScanResult    // per-image: codes found, each with its AddCodeResult
  Status -> RecoveryStatus                     // metadata present?, received/total, missing indexes
  TryAssemble() -> AssemblyResult              // file bytes + HashVerified flag + metadata; only when complete
```

Error strategy: Core returns result objects for expected recovery outcomes (bad scans are normal, not
exceptional) and throws typed exceptions for programming/IO errors; ViewModels translate to friendly
`.resx` messages. All long operations async with cancellation; UI updates marshaled per dotnet-async.

## Overall acceptance criteria

1. Round trip: a binary fixture (~100 KB random bytes) → `Encode` → codes → QR PNGs (QRCoder) →
   ZXing decode → `RecoverySession` → assembled bytes identical, SHA-256 verified.
2. Manual-entry round trip: feeding the code *strings* directly reproduces the file.
3. Out-of-order, duplicate, corrupted-chunk (CRC), wrong-backup-ID, and gzip-flag scenarios covered by tests.
4. Compressible fixture (text) produces `c=1` and fewer chunks; incompressible fixture stays `c=0`.
5. PDF: A4, page furniture (filename, SHA-256, date, backup ID, page n/m), per-code index captions,
   page-1 recovery-spec box; document generates without layout exceptions.
6. Desktop app: Backup and Recover flows fully operational on Windows and Linux.
7. Zero-warning build, full test suite green (Definition of Done per dev-workflow).

---

## PHASE01 — Repo & solution scaffolding — **DONE**

Goal: empty but fully wired solution; everything builds clean, one placeholder test runs green.

1. Hygiene files from house templates: `.gitignore`, `.gitattributes` (git-repo-hygiene),
   full `.editorconfig`, `Directory.Build.props` (fill authors: Josué Clément / 2026),
   `Directory.Packages.props` (dotnet-solution-config).
2. `global.json`: pin .NET 10 SDK, `"test": { "runner": "Microsoft.Testing.Platform" }`.
3. `Enigma.HardCopy.slnx` + 4 projects (Core, Desktop, Core.Tests, Desktop.Tests) per the
   architecture above; CPM entries for the full package set (Avalonia 12.1.1 ecosystem incl.
   `AvaloniaUI.DiagnosticsSupport`, CommunityToolkit.Mvvm, QuestPDF 2026.7.2, QRCoder 1.8.0,
   ZXing.Net 0.16.11 + SkiaSharp binding, xunit.v3 3.2.x — latest stables at build time).
4. Desktop bootstrapped per the avalonia skill: sync `Main`, `StartWithClassicDesktopLifetime`,
   IHost in `App.OnFrameworkInitializationCompleted`, empty `MainWindow` + `MainWindowViewModel` from DI.
5. `LICENSE` (MIT), `README.md` stub, `Assets/` placeholder icon wiring.
6. Acceptance: `dotnet build` zero warnings; `dotnet test` runs 1 placeholder test per test project.

## PHASE02 — Core encoding pipeline — **TODO**

Goal: file bytes → list of code strings, fully unit-tested (no QR, no PDF yet).

1. `Base32` codec (RFC 4648, unpadded, tolerant decode: uppercase + whitespace strip).
2. `Crc32` (IEEE) — no external package needed.
3. `Chunker` (split by chunk-size option), `HeaderCodec` (format/parse the header per the spec),
   `BackupMetadata` + `|`-separated serializer (unknown-key-tolerant parser).
4. gzip compress-if-smaller step; SHA-256 of original bytes.
5. `BackupEncoder.Encode` composing the above → `EncodedBackup`.
6. Tests: codec round-trips (property-style over random buffers), header golden strings, CRC vectors,
   metadata round-trip incl. exotic filenames, compress/no-compress branch, chunk-count math, charset
   invariant (every produced code string ⊆ QR alphanumeric set).
7. Acceptance: overall criteria 3–4 at the string level; zero-warning build, tests green.

## PHASE03 — QR generation & PDF composition — **TODO**

Goal: `EncodedBackup` → printable A4 PDF; QR seam proven by decode round-trip.

1. `QrRenderer`: QRCoder module matrix → pixel-exact bitmap (N px/module, quiet zone, ECC M),
   sized so modules ≥ ~0.35 mm at print size.
2. `PdfComposer` (QuestPDF, Community license set at startup): A4 grid (~3×5 target at default chunk),
   header/footer (filename, SHA-256 hex, date, backup ID, page n/m), per-code index caption,
   page-1 recovery-spec box (~10 lines describing the format for app-less recovery).
3. Page/code-count estimator exposed for the UI.
4. Tests: QR seam round-trip (code string → PNG → ZXing.Net decode → identical string) across all
   3 chunk sizes; PDF smoke test (non-empty, expected page count, no layout exception).
5. Acceptance: overall criteria 1 (generation half) and 5.

## PHASE04 — Core recovery pipeline — **TODO**

Goal: images and/or typed strings → verified original file.

1. `RecoverySession` state machine per the API contract: any order, dedupe by index, conflict
   detection (same index, different bytes), wrong-BID rejection, missing-index reporting.
2. `ImageDecoder`: SkiaSharp load (PNG/JPEG/BMP) → ZXing.Net multi-reader (all QR codes per image).
3. Assembly: concatenate chunks, gunzip if `c=1`, SHA-256 verify vs metadata → `AssemblyResult`.
4. Tests: full round-trip closing overall criteria 1–3 (generated PNGs from PHASE03 renderer as
   fixtures), corruption matrix (flipped chars → BadCrc with correct index; truncated code →
   Malformed; foreign BID → WrongBackup), assemble-before-complete rejected, hash-mismatch surfaced.
5. Acceptance: overall criteria 1–4 end-to-end in Core.

## PHASE05 — Avalonia desktop app — **TODO**

Goal: the full UX on stock Fluent controls, MVVM per house CommunityToolkit style (no source generators).

1. Shell: `MainWindow` with simple two-view navigation (Backup / Recover), `.resx` strings, ViewLocator.
2. Backup view: file picker (StorageProvider), computed SHA-256 display, chunk-size advanced setting
   (small/medium/large, session-only), page/code estimate with large-file warning, output path picker,
   async generation with progress + cancellation, success/error surface.
3. Recover view: image import (multi-select + drag-&-drop) and manual text entry feeding one shared
   `RecoverySession`; live status (received/total, missing indexes, metadata state, per-code results);
   save on success; hash-mismatch blocking warning with explicit "save anyway".
4. DI: Core services + ViewModels registered in the IHost; background work per dotnet-async.
5. Tests (Desktop.Tests): ViewModel logic — state transitions, command enablement, error mapping.
6. Acceptance: overall criterion 6; manual smoke on Windows + Linux (WSLg).

## PHASE06 — Release readiness — **TODO**

Goal: v1.0.0 shippable from a clean checkout.

1. Publish scripts/commands: self-contained single-file `dotnet publish` for `win-x64` and `linux-x64`,
   zipped artifacts; documented in README.
2. App icon: multi-resolution `.ico` wired via `<ApplicationIcon>` + window `Icon` (placeholder art;
   final art user-supplied — record as follow-up in the completion doc).
3. README user guide: purpose, backup walkthrough, both recovery walkthroughs, app-less recovery
   pointer to the printed spec, build/publish instructions.
4. Version 1.0.0 set solution-wide; final full-suite run on both publish targets.
5. Acceptance: overall criterion 7; a zipped artifact per RID starts and completes a backup+recovery
   round trip on its target OS.
