# FEATURE-79FF-PHASE04 — Core recovery pipeline — DONE

**Branch:** `feature/feature-79ff-phase04-recovery` (cut from the PHASE03 branch, itself still unmerged)
**Date:** 2026-08-04

## Summary

The recovery half of the format now exists, so the project closes its loop: a file encoded in PHASE02 and
printed in PHASE03 can be scanned or typed back in and comes out byte for byte identical, with its SHA-256
proving it.

Two seams and a state machine. `ImageDecoder` turns an imported image into the code strings printed on it —
every QR on the sheet, in one pass. `RecoverySession` accumulates codes from any source in any order, tells
the user what is still missing, and rebuilds the file only when nothing is. Between them sits the vocabulary
the UI will speak in PHASE05: six `AddCodeOutcome` values for what can become of one code, five
`AssemblyOutcome` values for what can become of the file.

The design principle throughout is that **a recovery is not a function, it is a process**. Pages get scanned
twice, one symbol refuses to read and gets typed by hand, a sheet from last year's backup turns up in the
pile. None of those is an error, so none of them throws: every one is a reported outcome carrying the barcode
index it concerns, because "re-scan code 7" is the only thing worth telling a user.

Beyond the acceptance criteria, the loop was closed **through real paper**: a composed PDF was rasterised by
poppler at five resolutions and fed back through the new pipeline. At 150, 200, 300, 400 and 600 dpi all 21
codes of a 20 KB backup were read — 9 on page 1 and 12 on page 2, the layout's exact split — and the file
came back verified with its emoji-bearing file name intact (see *Build/test evidence*).

## Files/modules touched

**Created — `src/Enigma.HardCopy.Core/`**

| File | What it is |
|---|---|
| `AddCodeOutcome.cs` | The six things that can become of one offered code. `Malformed` is the zero value, so an uninitialised outcome cannot read as success. |
| `AddCodeResult.cs` | Outcome + barcode index (when known) + the foreign backup ID (for `WrongBackup`). Private constructor and factory methods, so an "accepted without an index" result is not expressible. |
| `RecoveryStatus.cs` | The snapshot shown between scans: backup ID, metadata, received/total, missing indexes, `IsComplete`. |
| `AssemblyOutcome.cs` | `Incomplete` \| `EncryptionUnsupported` \| `DecompressionFailed` \| `HashMismatch` \| `Verified`. |
| `AssemblyResult.cs` | The rebuilt bytes, their hash, the metadata, and `HashVerified`. A class, not a record — value equality over a file-sized payload would be an expensive operation offered by accident. |
| `ImageScanResult.cs` | Per-image: was it a picture at all, and what became of each code found on it. |
| `ImageDecoder.cs` | The static seam over SkiaSharp + ZXing.Net. `TryReadCodes` — many codes per image, with a second pass for a single cropped symbol. |
| `RecoverySession.cs` | The state machine: `AddCode`, `AddImage`, `Status`, `TryAssemble`. |

**Created — `tests/Enigma.HardCopy.Core.UnitTests/`**
`RecoverySessionTests.cs` (50), `RecoveryRoundTripTests.cs` (20), `ImageDecoderTests.cs` (10), and
`RecoveryFixtures.cs` — the shared fixtures, including the fabricated backups whose metadata block says
something untrue (a wrong hash, a gzip stream that is not one, the encryption flag set). Those cannot come
from `BackupEncoder`, which by construction only ever tells the truth, so building them from the same
primitives is what lets those paths be tested at all.

**Modified**
- `docs/plan/FEATURE-79FF.md` — PHASE04 → DONE; the recovery contract sketch updated to the API as built; the
  format spec extended with the *Encryption flag handling* rule (see *Decisions* 4).
- `docs/roadmap.md` — PHASE04 → DONE.

**Unchanged:** no `.csproj` edits were needed. PHASE01 had already put ZXing.Net, the ZXing SkiaSharp binding
and SkiaSharp on Core, and `SkiaSharp.NativeAssets.Linux` on the test project with the comment "the test host
needs them to decode images (exercised from PHASE04 onward)". It does, and it did.

## Decisions taken at build time

1. **Integrity is checked twice, at two scales, and the code path is ordered around that.** A code's CRC-32
   covers its payload and *not* its header, so the checksum is the last thing that can be trusted absolutely:
   everything after it reasons about an unprotected header. Hence the order in `AddCode` — parse, then backup
   ID, then CRC, then the header-derived consistency checks — and hence the end-to-end SHA-256 in
   `TryAssemble`, which is the only check that can say a recovery is intact.
2. **A damaged code never gets to say what the session is recovering.** The backup ID and chunk total are
   adopted from the first code that *passes* its checksum, not the first code offered. A misread ID adopted
   from a damaged code would lock the session onto a backup that does not exist and reject every good code
   after it (`AddCode_ADamagedCode_DoesNotFixTheSessionIdentity`).
3. **`Conflict` covers both ways a code can contradict the session** — the same index carrying different
   bytes, or a different chunk total. Both are "one of these two codes is damaged in a way its checksum could
   not catch", both need the same remedy, and both are things the plan's five listed outcomes had no room
   for. The code already held wins: it has been through the same checks the newcomer just failed to
   contradict, and overwriting it could discard the good chunk.
4. **An encrypted backup is refused, not mis-reported.** The `e` flag is *reserved*, not forbidden, so a later
   version may set it without bumping the `EHC1` magic — meaning a v1 decoder must handle a backup it cannot
   undo. Returning presumable ciphertext as a `HashMismatch` would be true and misleading; a dedicated
   `EncryptionUnsupported` outcome says the real thing. The rule is now in the plan's format spec, because it
   is a decoder contract and not an implementation detail.
5. **A payload whose Base32 cannot decode is reported as `BadCrc`, not `Malformed`.** Base32 strictness about
   symbol counts and padding bits (PHASE02 decision 8) exists to catch a mistyped final character *before*
   the CRC; both mean the same thing to a user — that one code did not survive, re-scan it — and both know
   which index it was. `Malformed` is kept for text that is not a code of this format at all, which is the
   distinction the plan's corruption matrix asks for (flipped characters → `BadCrc` with the index; a code
   truncated out of its five fields → `Malformed`).
6. **The metadata block is decoded as strict UTF-8.** A tolerant decoder substitutes replacement characters,
   which would silently mangle the file name a recovery exists to restore. Invalid UTF-8 behind a correct
   checksum is damage no checksum could catch, so it is reported as `Malformed` at index 0.
7. **Decompression is bounded by the size the metadata block records.** A gzip stream that expands past it
   cannot be the file that block describes, and stopping there is also what keeps a damaged — or deliberately
   crafted — backup from being decompressed into memory without limit. The buffer grows with what gzip
   actually produces rather than being pre-allocated from the recorded size, so a metadata block claiming ten
   gigabytes allocates nothing. The opposite direction is deliberately *not* a decompression failure: a
   shorter stream is materialised and reported as the hash mismatch it is, which is the more precise thing to
   say about it.
8. **Every call on a session is atomic.** PHASE05 imports several images at once from background tasks, and a
   `Dictionary` mutated from two threads corrupts silently. A `System.Threading.Lock` around the mutating and
   reading paths costs nothing uncontended and removes the whole class of bug; image *decoding* happens
   outside the lock, so a slow scan does not block the UI's status reads. A sequence of calls is explicitly
   not atomic, and the XML docs say so.
9. **`ImageDecoder` is static, `RecoverySession` is constructed directly.** This continues the split PHASE02
   and PHASE03 established: a pure, deterministic library seam is static (`Base32`, `Crc32`, `QrRenderer`,
   now `ImageDecoder`), while a *service* the application composes is an interface. A session is neither — it
   is per-recovery state that a ViewModel owns, so it is a plain sealed class with no interface and no
   factory. PHASE05 can substitute it if it turns out to need to; nothing here forces the decision.
10. **A second reading pass for a single cropped symbol.** The multi-symbol detector works outwards from the
    page it expects around each symbol, and an image that is nothing but one code and its quiet zone gives it
    nothing to work from. Both shapes reach a recovery — a scanned sheet and a single exported symbol — so a
    `PureBarcode` pass runs when the multi-pass finds nothing (`TryReadCodes_ASingleCroppedSymbol_ReadsIt`).
11. **"Not an image" and "no codes on it" are different answers.** `TryReadCodes` fails only for bytes that
    are not a picture; a blank scan succeeds with an empty list. The remedies differ — import a different
    file, versus re-scan the page — so the UI must be able to tell them apart.
12. **`MissingIndexes` lists data chunks only, and the metadata block has its own flag.** The metadata block
    is the one code that is not part of the file: every chunk can be present and the file still not
    recoverable, because there is no name to save under and nothing to verify against
    (`TryAssemble_WithEveryChunkButNoMetadataBlock_IsIncomplete`).
13. **Flat namespace, files at the project root** — continuing PHASE02 decision 9 and PHASE03 decision 12. The
    plan allowed a `Recovery/` folder; the recovery types are used together with the format types by both
    halves, and a second namespace would have bought nothing.

## Deviations & follow-ups

1. **Two outcome values the plan did not list** — `AddCodeOutcome.Conflict` and
   `AssemblyOutcome.EncryptionUnsupported`, for the reasons in decisions 3 and 4. The plan's contract sketch
   and format spec have both been updated, because PHASE05 maps every outcome to a `.resx` message and needs
   the full set.
2. **`TryAssemble` keeps the plan's name while returning a result rather than a `bool`.** That is what the
   plan's contract sketch specifies, and the `Try` still earns its keep: the one thing it promises is that it
   will not throw when the session is not ready.
3. **A mildly rotated photograph can lose a symbol.** Measured: a page rasterised at 300 dpi and rotated 1.5°
   yields 11 of 12 codes, at JPEG quality 80 *and* 100 — so it is the rotation, not the compression. Rotated
   0.5° or not at all, every code reads at JPEG 80. Nothing is wrong with this — the session reports exactly
   which index is missing and the user types that one code in, which is the mixed-source path
   `Recover_MixingAScannedPageWithACodeTypedByHand_RebuildsTheFile` covers. **Follow-ups worth considering:**
   PHASE06's README should tell users to scan flat and square, and a later phase could add a deskew or
   tile-and-retry pass to `ImageDecoder` to raise the hit rate on hand-held photographs.
4. **No per-chunk length validation.** A non-final chunk whose length disagrees with the metadata block's `z`
   could be rejected at `AddCode` time, and deliberately is not: the check would add a failure mode that
   turns a recoverable backup into a rejected one if any future encoder ever pads differently, while adding
   nothing the end-to-end SHA-256 does not already catch.
5. **No progress reporting or cancellation on the recovery path.** `AddImage` is one call into ZXing that
   cannot be interrupted, and assembly of a file this size is milliseconds. PHASE05 reports progress per
   image — which is the granularity a user thinks in anyway — and can cancel between images.
6. **`RecoveryStatus.Status` walks the received chunks on every read.** A few hundred comparisons for a
   backup of a few hundred kilobytes, in exchange for a snapshot immune to whatever arrives next. If PHASE05
   ever binds it in a tight loop over a very large backup, cache it in the ViewModel rather than making the
   session's state mutable from outside.
7. **The PDF → raster → recover verification is not part of the suite.** It shells out to poppler's
   `pdftoppm`, which the suite must not depend on; it was run by hand during this build and its output is
   recorded below. The suite's own images come from the real `QrRenderer`, composed into page grids by
   `PageLayout`'s own column count.
8. **Line endings:** nothing to report — every new file was authored LF and `.gitattributes`
   (`* text=auto eol=lf`) has been in place since the first commit. No action taken, per the workflow's
   recommendation-only rule.
9. **PHASE01 to PHASE03 are still unmerged.** This branch stacks on `feature/feature-79ff-phase03-qr-pdf`; the
   four phases will need merging in order.

## Build/test evidence

```
dotnet build Enigma.HardCopy.slnx              → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build Enigma.HardCopy.slnx -c Release   → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --solution Enigma.HardCopy.slnx   → Passed! total: 505  failed: 0  succeeded: 505  skipped: 0
```

Zero warnings holds under `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` + the full `.editorconfig`, and
under `GenerateDocumentationFile` on Core — every new public type and member is XML-documented.

**505 tests, up from 425** (80 new: Core 503, Desktop 2). The whole suite runs in about five seconds,
rendering and decoding real QR symbols throughout.

Independent verification, beyond the suite — a composed PDF, rasterised by poppler, read back through
`ImageDecoder` and `RecoverySession`:

| Scan | Page 1 | Page 2 | Result |
|---|---|---|---|
| 600 dpi PNG (4959 × 7017) | 9 / 9 | 12 / 12 | **Verified**, 20 000 bytes identical, file name intact |
| 400 dpi PNG | 9 / 9 | 12 / 12 | complete |
| 300 dpi PNG | 9 / 9 | 12 / 12 | complete |
| 200 dpi PNG | 9 / 9 | 12 / 12 | complete |
| 150 dpi PNG (1240 × 1755) | 9 / 9 | 12 / 12 | complete |
| 300 dpi, JPEG 80, unrotated | 9 / 9 | 12 / 12 | **Verified** |
| 300 dpi, 0.5° rotation, JPEG 90 | 9 / 9 | 12 / 12 | **Verified** |
| 300 dpi, 1.5° rotation, JPEG 80 and 100 | 9 / 9 | 11 / 12 | incomplete, missing index reported (deviation 3) |

The 9 / 12 split is `PageLayout`'s own arithmetic, arrived at independently by an outside rasteriser and
ZXing — and `pdfinfo` again reports `595 x 842 pts (A4)`, 2 pages. The recovered file name was
`clé privée — 🔐 secrets.kdbx`, which exercises the metadata escaping (`%`, `|`), non-ASCII and an
astral-plane character through the whole path.

Notable coverage beyond the plan's list:

- `RecoverySessionTests.AddCode_FromSeveralThreadsAtOnce_KeepsTheSessionConsistent` — every code offered four
  times from several threads; each ends up held exactly once and the file still verifies. This is the promise
  decision 8 makes to PHASE05.
- `AddCode_TheSameIndexWithDifferentBytes_IsAConflictAndTheHeldChunkSurvives` — the conflict is not merely
  reported, the good chunk is *proved* to have survived it: the backup still assembles and verifies afterwards.
- `RecoveryRoundTripTests.Recover_FromScannedPages_RebuildsTheFile` — the codes are split across simulated
  sheets by `PageLayout.GetCodeCountOnPage` and laid out in `layout.Columns`, so the images the reader is
  given have the shape of the real printed page rather than of a convenient fixture.
- `Recover_APageFromAnotherBackup_IsRejectedCodeByCode` — a whole foreign sheet scanned into the pile: every
  code refused, the foreign ID reported, and the recovery in progress untouched and still verifiable.
- `Recover_AnExoticFileName_ComesBackIntact` — six names through the full recovery, including one that
  contains the metadata separator and one that is pure emoji.
- `ImageDecoderTests.TryReadCodes_ABmpScan_ReadsTheCodes` — Skia decodes BMP but will not write it, so the
  fixture assembles a 24-bit BMP by hand rather than leaving the claimed format untested.
- `TryAssemble_WhenTheStreamExpandsPastTheRecordedSize_IsADecompressionFailure` and
  `TryAssemble_WhenTheStreamIsShorterThanRecorded_IsAHashMismatch` — both halves of decision 7's deliberate
  asymmetry, pinned so neither can drift into the other.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `RecoverySession` state machine: any order, dedupe by index, conflict detection, wrong-BID rejection, missing-index reporting | ✅ 50 tests; conflict covers both a differing payload and a differing total (decision 3) |
| 2 | `ImageDecoder`: SkiaSharp load (PNG/JPEG/BMP) → ZXing multi-reader, all QR codes per image | ✅ all three formats asserted; twelve symbols read off one sheet in a single pass |
| 3 | Assembly: concatenate, gunzip if `c=1`, SHA-256 verify vs metadata → `AssemblyResult` | ✅ plus the bounded-decompression and encryption refusals (decisions 4 and 7) |
| 4 | Tests: full round trip on PHASE03-rendered PNGs; corruption matrix (flipped → `BadCrc` with the index, truncated → `Malformed`, foreign BID → `WrongBackup`); assemble-before-complete rejected; hash mismatch surfaced | ✅ every item, and the round trip runs at all three chunk sizes from both single symbols and full pages |
| 5 | Overall criteria 1–4 end-to-end in Core | ✅ criterion 1 closed through a 150–600 dpi raster of the printed PDF; 2 by string entry; 3 by the corruption, duplicate, out-of-order and wrong-backup suites; 4 by the compressible fixture travelling in fewer codes and still verifying |

Overall criteria 5 (the PDF's own furniture) was closed by PHASE03. What remains is criterion 6 — the desktop
app — and criterion 7's final zero-warning full-suite run at release, which are PHASE05's and PHASE06's.
Everything Core owes the UI now exists: a session that reports rather than throws, a status shaped like the
screen it will be shown on, and an assembly result that hands back unverified bytes only with the flag that
says so.
