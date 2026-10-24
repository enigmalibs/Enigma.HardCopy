# FEATURE-E931-PHASE01 — Core camera-frame decoding — DONE

**Branch:** `feature/feature-e931-phase01-frame-decoding`
**Date:** 2026-10-19
**Run:** feature/2026-10-19-android-recover-app

## Summary

Core's decoding seam gains its second entry point. `ImageDecoder.TryReadCodes(Stream)` reads an imported,
encoded image; the new `ImageDecoder.ReadCodes(byte[] luminance, int width, int height, int rowStride)` reads
one **live camera frame** given as 8-bit luminance — the Y plane of a YUV frame, exactly as a camera delivers
it. No colour conversion, no image codec, and no ZXing type in the public signature: the Android app will hand
its frames to Core and get code texts back, so ZXing stays behind the seam Core already defined for it.

The method searches the whole frame for every symbol first, then for a single one (a phone held close to one
code fills the frame with it, which the multi-symbol detector cannot locate). It accepts padded rows and a
final row that stops at its last pixel — the two shapes a camera plane's buffer commonly has — and rejects
impossible dimensions or a buffer too short for them.

## Files/modules touched

**Modified**

- `src/Enigma.HardCopy.Core/ImageDecoder.cs` — the `ReadCodes` overload, its frame reader, and the type's
  remarks (two kinds of input).
- `tests/Enigma.HardCopy.Core.UnitTests/RecoveryFixtures.cs` — `ToLuminanceFrame`: an encoded image turned
  into a camera-style Y plane (white border, padded stride, optionally unpadded last row).
- `docs/roadmap.md`, `docs/plan/FEATURE-E931.md` — statuses; an amendment note on PHASE01's step 1.

**Created**

- `tests/Enigma.HardCopy.Core.UnitTests/LuminanceFrame.cs` — the fixture's frame record.
- `tests/Enigma.HardCopy.Core.UnitTests/ImageDecoderFrameTests.cs` — 15 tests: a symbol in a larger padded
  frame, an unpadded last row, four symbols in one frame, a default-density (version > 20) symbol at 3 px per
  module, a blank frame, frames fed into a `RecoverySession` that verifies the rebuilt file, and argument
  validation (null, non-positive sizes, stride below width, short buffer, exact minimum length).
- `docs/done/FEATURE-E931-PHASE01.md` — this file.

## Deviations & follow-ups

- **`TryHarder` is on for camera frames, not off as planned.** The plan argued that a live frame is retried
  by the next one, so a cheaper pass would do. The first test run disproved it: with `TryHarder` off, ZXing's
  multi-symbol finder samples only every few rows, and a frame holding four codes read as **one**
  (`ReadCodes_SeveralSymbolsInView_ReadsEveryOne`, expected 4, actual 1). Turned on, all four are read. One
  fix cycle used. PHASE04's decode worker already drops frames while a read is running, so the extra cost
  lowers the attempt rate, not the preview's smoothness. The plan's PHASE01 step carries an amendment note;
  its decisions-table row is left as the record of the original reasoning.
- No user-facing documentation changed: nothing a user sees exists yet (documentation sweep found nothing
  stale).
- Line endings: `.gitattributes` enforces `eol=lf`; nothing to recommend.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Frame reader `TryHarder` | On | Measured: off misses symbols in multi-symbol frames | Off (plan) |
| Second pass for a frame | Single-symbol, **not** `PureBarcode` | A camera frame always has surroundings; `PureBarcode` assumes the image is nothing but the symbol | Reusing the image path's pure pass |
| Signature | `byte[]` + width/height/stride, returning `IReadOnlyList<string>` | Mirrors what a camera plane is; ZXing's luminance source takes a `byte[]`, so a span would force a copy | A frame record type in Core's public API (one more type for one call) |
| Short final row | Accepted (`rowStride × (height − 1) + width`) | Android camera planes commonly end at the last pixel | Requiring `rowStride × height` (would reject real frames) |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.HardCopy.slnx`: **697 passed, 0 failed, 0 skipped** (was 682; +15).
- Fix cycles used: **1 of 3** (see *Deviations*).
