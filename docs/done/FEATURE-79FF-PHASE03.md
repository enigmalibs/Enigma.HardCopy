# FEATURE-79FF-PHASE03 — QR generation & PDF composition — DONE

**Branch:** `feature/feature-79ff-phase03-qr-pdf` (cut from the PHASE02 branch, itself still unmerged)
**Date:** 2026-08-04

## Summary

An `EncodedBackup` now becomes a printable A4 PDF. Two seams were added on top of PHASE02's code strings: a
QR renderer that turns one code into a pixel-exact PNG, and a composer that lays those PNGs out on paged A4
with the furniture and the printed recovery instructions that make the sheets self-describing.

The grid is **derived, not tuned**. The only print constraint is a minimum module size (0.35 mm); `PageLayout`
packs the densest grid an A4 sheet allows while honouring it, then grows each cell into the space left over.
So the chunk-size presets need no per-preset layout: Small comes out 5×6, Medium and Large 3×4, and the
printed module size always lands at or above the floor. Computing the grid up front is also what makes the
page count knowable before anything is drawn, which the UI's estimate and the printed `Page n / m` both need.

Beyond the acceptance criteria, the generated PDF was **verified as paper**: composed, checked with
`pdfinfo` (A4, page count matching `PageLayout.PageCount`), rasterised with `pdftoppm -r 600`, and the page
images fed back through ZXing. All 21 codes of a 20 KB backup were found — 9 on page 1 and 12 on page 2,
exactly the layout's split — every CRC verified, and the reassembled bytes were identical to the original
with a matching SHA-256 and an intact `clé privée — 🔐 secrets.kdbx` file name. The seam the plan asks for is
code → PNG → decode; this closes the wider one, code → PNG → **PDF → rasterised page** → decode.

## Files/modules touched

**Created — `src/Enigma.HardCopy.Core/`**

| File | What it is |
|---|---|
| `QrErrorCorrectionLevel.cs` | Our own four-level enum, so QRCoder's types never reach the public API. |
| `QrRenderOptions.cs` | Error-correction level + pixels per module (validated 1–32, default 8). |
| `QrSymbol.cs` | PNG bytes + version + modules per side + pixels per module; a class, not a record, because it carries a `byte[]`. |
| `QrRenderer.cs` | `Render` / `GetVersion` / `GetVersionForLength` / `GetModulesPerSide`. The single call into QRCoder. |
| `PdfOptions.cs` | Composes `QrRenderOptions` plus the minimum module size, page margin and code spacing, all validated. |
| `PageLayout.cs` | All the geometry: cell size, module size, grid, rows on page 1, page count, `GetCodeCountOnPage`, plus `Estimate` for the UI. |
| `RecoveryInstructions.cs` | `Title` / `CodeShape` / `Paragraphs` — the app-less recovery spec printed on page 1. |
| `IPdfComposer.cs` / `PdfComposer.cs` | `Compose`: render every symbol, then draw the QuestPDF document the layout describes. |

**Created — `tests/Enigma.HardCopy.Core.UnitTests/`**
`QrRendererTests.cs` (64), `PageLayoutTests.cs` (38), `PdfComposerTests.cs` (30), `PdfOptionsTests.cs` (19),
`RecoveryInstructionsTests.cs` (19).

**Modified**
- `docs/plan/FEATURE-79FF.md` — PHASE03 → DONE, and the *Capacity check* paragraph corrected and expanded
  (see *Deviations* 1); `docs/roadmap.md` — PHASE03 → DONE.

**Unchanged:** no `.csproj` edits were needed. QuestPDF, QRCoder, ZXing and SkiaSharp were already referenced
by Core from PHASE01, and the test project sees them transitively through its `ProjectReference`.

## Decisions taken at build time

1. **The grid is computed from a module-size floor, not fixed per preset.** The plan targeted "~3×5 at
   default chunk" and left "exact grid/version tuning" to this phase. A derived grid was chosen over three
   tuned constants because it also handles the non-default combinations correctly on its own, and because it
   can guarantee the print constraint rather than approximate it — the module size is provably never below
   the floor (`PageLayoutTests.ForBackup_ModuleSizeNeverFallsBelowTheFloor`).
2. **`Compose` is synchronous, with a `CancellationToken`** — the plan's own contract sketch
   (`PdfComposer.Compose(...) -> byte[]`), rather than PHASE02's `EncodeAsync` precedent. Nothing here waits
   on I/O; QuestPDF's API is synchronous throughout. Wrapping that in a `Task` would only disguise which
   thread pays for it, and the house `dotnet-async` rule governs async APIs rather than requiring CPU-bound
   work to pretend to be one. The token is honoured between symbols, which is where the time goes, so
   PHASE05's `await Task.Run(() => composer.Compose(…), token)` cancels promptly.
3. **`QrRenderer` is a static class; `PdfComposer` is an injected service.** This follows the split PHASE02
   established: pure deterministic format machinery is static (`Base32`, `Crc32`, `HeaderCodec`), while the
   pipeline stages an application composes are interfaces (`IBackupEncoder`, now `IPdfComposer`) so a
   ViewModel test can substitute them instead of generating a real PDF. `PageLayout`'s factories are static
   for the same reason — the UI's page estimate should not need a service.
4. **The QuestPDF licence is declared in `PdfComposer`'s static constructor**, not at application startup as
   the plan suggested. A static constructor is guaranteed to have run before `Compose` can be called, from
   any host — the app, the test runner, a future CLI — so the declaration cannot be forgotten in one of them.
5. **`QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable` is turned off.** Left at its default, a file name
   holding an emoji or a CJK ideograph — which the bundled Lato font has no glyph for — makes QuestPDF throw,
   and the backup fails over page *furniture*. A fallback box in the header is the better trade: the file
   name that matters is the one inside the metadata code. Six exotic names are asserted to compose.
6. **`UseOriginalImage(true)` on every symbol.** QuestPDF re-encodes images as lossy JPEG by default, and a
   QR symbol whose module edges have been smeared by DCT ringing may simply not scan.
   `Compose_DoesNotReEncodeTheSymbolsAsJpeg` asserts no DCT-decoded image reaches the file.
7. **A fixed-height recovery-spec box (60 mm).** Letting it size itself would make page 1's capacity — and
   therefore the whole page count — unknowable in advance. It is written as wrapped paragraphs rather than
   pre-broken lines (proportional font, so hand-aligned columns would not align) and carries roughly five
   lines of slack; overflowing it would throw at generation, which the composer tests would catch.
8. **A 0.25 mm safety margin is shaved off both usable dimensions.** Found empirically: at ECC High with
   small chunks the grid came out filling the width to exactly 190.00 mm of 190 mm, and QuestPDF refused the
   document over single-precision error in the millimetre-to-point conversion. The margin is thousands of
   times the error being guarded against and costs no density at any supported chunk size — verified against
   all twelve preset × level combinations.
9. **The printed file name is truncated at 60 characters.** Keeps the header's height predictable whatever
   the name; the untruncated name is in the metadata code.
10. **`PdfOptions` composes `QrRenderOptions`** (as `Qr`) instead of repeating its two knobs, so the
    validation lives in one place.
11. **The document's creation date comes from the backup, not the clock**, so composing the same backup twice
    produces the same document.
12. **Flat namespace, files at the project root** — continuing PHASE02's decision 9. A `Rendering/` folder
    would have meant a second namespace for four files that the composer, the layout and the estimator all
    use together.

## Deviations & follow-ups

1. **The plan's *Capacity check* was wrong, and is corrected in the plan file** — not just recorded here,
   because PHASE04 and PHASE05 both reason from it. It claimed a default 1024 B chunk "fits QR version 25 at
   ECC M (1708 alphanumeric)". The character arithmetic was right (a real default code measures 1663–1668
   characters) but the capacity was not: version 25 at ECC M holds **1451** characters, so a default chunk
   needs **version 28** (1732). Both the delegated sub-agent and the primary implementation reached this
   independently, and QRCoder agrees with ISO/IEC 18004 to the character. PHASE02's ceiling figure (3391 at
   version 40, ECC M) was correct and is unaffected.
2. **The default grid is 3×4 = 12 codes per page, not the plan's "roughly 3×5 = 15"** — a direct consequence
   of correction 1: the symbol is 137 modules per side, so 56.7 mm at 0.414 mm/module rather than the assumed
   ~44 mm. Page 1 holds 9, the rest 12. A 100 KB incompressible file is 9 pages; Small chunks give 5×6 = 30
   codes per page and Large 3×4 = 12, both around 15–18 KB of payload per sheet.
3. **Error-correction levels Quartile and High cannot render a Large or maximum-size chunk at all.** Measured
   alphanumeric ceilings are L 4296, M 3391, Q 2420, H 1852, while a 1536 B chunk needs 2485 characters.
   `PageLayout` rejects the combination with an `ArgumentOutOfRangeException` rather than printing paper that
   omits data, and the plan now records the ceilings. Nothing in the app can reach this — ECC M is the
   validated default and the level is not user-facing — but PHASE05 must not expose the level as a free
   setting without pairing it against the chunk size.
4. **The PDF is large at the default 8 pixels per module**: 7.1 MB for a 100 KB backup (9 pages, ~790 KB per
   sheet); 20 KB gives 1.4 MB. Measured against the knob — 2 px/module 1.0 MB, 4 px 2.9 MB, 6 px 4.6 MB,
   8 px 7.1 MB, 12 px 12.6 MB. Kept at 8 because the file is transient (it gets printed; the *paper* is the
   archive) and a soft module edge is the one defect that cannot be fixed after printing. **Follow-up worth
   considering in a later phase:** draw the module matrix as PDF vector rectangles instead of an embedded
   raster. It would be crisper at any printer resolution and roughly two orders of magnitude smaller, but the
   plan specifies "QRCoder module matrix → pixel-exact bitmap", so changing it is out of PHASE03's scope.
5. **The spec box's presence in the PDF is proven structurally, not by text extraction.** Its content is
   asserted directly against the format constants in `RecoveryInstructionsTests`, and its presence follows
   from the document generating at all (a box taller than its fixed height throws) together with page 1
   holding fewer codes than later pages. Extracting text back out of the PDF would need a parser the project
   does not depend on.
6. **No progress reporting yet.** `Compose` takes no `IProgress<T>`; PHASE05's "async generation with
   progress" can add an optional parameter without breaking the interface. Rendering the symbols is the
   reportable part — it is already a per-code loop.
7. **`RecoveryInstructions` is a new public type the plan did not name.** The plan put the spec box inside
   `PdfComposer`; extracting it lets its content be asserted against the format constants it describes, and
   PHASE06's README user guide can quote it verbatim instead of paraphrasing it.
8. **Line endings:** nothing to report — every new file was authored LF and `.gitattributes`
   (`* text=auto eol=lf`) has been in place since the first commit. No action taken, per the workflow's
   recommendation-only rule.
9. **PHASE01 and PHASE02 are still unmerged.** This branch stacks on
   `feature/feature-79ff-phase02-core-encoding`; the three phases will need merging in order.

## Build/test evidence

```
dotnet build Enigma.HardCopy.slnx              → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet build Enigma.HardCopy.slnx -c Release   → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --solution Enigma.HardCopy.slnx   → Passed! total: 425  failed: 0  succeeded: 425  skipped: 0
```

Zero warnings holds under `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` + the full `.editorconfig`, and
under `GenerateDocumentationFile` on Core — every new public type and member is XML-documented.

**425 tests, up from 255** (170 new: Core 423, Desktop 2).

Independent verification, beyond the suite:

- **The document really is A4 and really is that long.** `pdfinfo` reports `Page size: 595 x 842 pts (A4)`
  and a page count equal to `PageLayout.PageCount` — an outside tool agreeing with the arithmetic, not the
  arithmetic agreeing with itself.
- **The paper scans.** `pdftoppm -r 600 -png` rasterised both pages of a 20 KB backup to 4959 × 7017 px;
  ZXing's multi-reader recovered 9 codes from page 1 and 12 from page 2 — the layout's exact split — every
  CRC verified, the chunks reassembled to bytes identical to the original, and the SHA-256 matched the
  metadata block. Useful signal for PHASE04: at 600 dpi the multi-reader found every symbol on a full page in
  one pass, with `TryHarder` and no `PureBarcode` hint.
- **The layout was checked across all twelve preset × error-correction combinations** for grid fit, module
  size and page-1 capacity, which is how the width-margin defect of decision 8 was found rather than shipped.

Notable coverage beyond the plan's list:

- `PdfComposerTests.Compose_ProducesExactlyThePageCountTheLayoutPromised` and
  `Compose_ThePageCountHoldsAcrossPageBoundaries` — the promise `PageLayout` makes to the UI and to the
  printed footer, checked against the real document at six sizes straddling page boundaries.
- `PageLayoutTests.GetCodeCountOnPage_AccountsForEveryCodeExactlyOnce` — no code can be dropped or printed
  twice by the paging arithmetic, which for a backup is the difference between recoverable and not.
- `PageLayoutTests.Estimate_NeverPromisesFewerPagesThanTheRealBackupNeeds` — the UI's estimate is allowed to
  be pessimistic and never optimistic, across both compressible and incompressible fixtures.
- `QrRendererTests` asserts the **alphanumeric-mode invariant** (`GetVersion(code)` equals
  `GetVersionForLength(code.Length)`): if QRCoder ever fell back to byte mode the whole capacity budget the
  page layout rests on would be wrong, and this is what would catch it.
- `Compose_DoesNotReEncodeTheSymbolsAsJpeg` — see decision 6.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | `QrRenderer`: QRCoder module matrix → pixel-exact bitmap (N px/module, quiet zone, ECC M), sized so modules ≥ ~0.35 mm at print size | ✅ PNG dimensions asserted against the IHDR, not the arithmetic; module size proven ≥ 0.35 mm for every preset (0.414 mm at the default) |
| 2 | `PdfComposer`: A4 grid, header/footer (filename, SHA-256, date, backup ID, page n/m), per-code index caption, page-1 recovery-spec box | ✅ A4 confirmed by `pdfinfo`; captions are `META` and `n / total`; spec box is five paragraphs in a fixed 60 mm frame |
| 3 | Page/code-count estimator exposed for the UI | ✅ `PageLayout.Estimate(fileSizeInBytes, …)`, worst-case by construction and asserted never to under-promise |
| 4 | Tests: QR seam round-trip across all 3 chunk sizes; PDF smoke test (non-empty, expected page count, no layout exception) | ✅ ZXing round trip per preset for the metadata, first and last codes; page count asserted against the document at 3 presets, 6 sizes, 3 module floors and 4 ECC levels |
| 5 | Overall criteria 1 (generation half) and 5 | ✅ and beyond — the generation half was closed all the way through a 600 dpi raster of the printed page (see *Build/test evidence*) |

Overall criterion 1's recovery half — a `RecoverySession` consuming these symbols — remains PHASE04's, as
the plan specifies. What PHASE03 can hand it is a renderer whose output ZXing reads back off a rasterised
page, and the observation that a full page decodes in a single multi-reader pass.
