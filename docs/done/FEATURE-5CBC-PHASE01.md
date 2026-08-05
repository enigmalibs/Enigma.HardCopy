# FEATURE-5CBC-PHASE01 — Package, theme & icon foundation — DONE

**Branch:** `feature/feature-5cbc-phase01-theme-foundation`
**Date:** 2026-08-05

## Summary

Put the app on the house control library's theme. `Enigma.Avalonia.Desktop` 1.0.0 and the three
`Enigma.Icons` packages are referenced through Central Package Management, the library's Fluent
dictionary is merged into `Application.Resources` as a `ResourceInclude`, and the nine `App*`-style
semantic brushes this app declared per theme variant are gone — every style class now points at the
library's `Enigma*` keys through `DynamicResource`. `MainWindow` paints itself with
`EnigmaBackgroundBrush`, because the theme paints controls and not windows.

No control was replaced and no page was restructured: the `TabControl` shell, the `ViewLocator`, the
section `Border`s, the inline outcome notices and the inline progress blocks are all still exactly
where PHASE02 and PHASE03 will find them. This phase changes what the app is *coloured by*, nothing
else. `Enigma.HardCopy.Core` is untouched, and so is every test — the suite runs green at 587 with no
test file edited.

Both routes into the Phosphor pack are proven before later phases lean on them: the *Generate PDF*
button's icon comes from `{ei:IconGeometry FilePdf}` (resolved at XAML load), and the *Cancel* button's
comes from the new `Resources/AppIcons.cs`, which resolves it in C# via
`PhosphorIconSet.Instance.GetGlyph(...).ToGeometry()`. Both were seen rendering in the running app.

## Files/modules touched

**Created**

- `src/Enigma.HardCopy.Desktop/Resources/AppIcons.cs` — the C#-resolved Phosphor outlines. One member
  for now (`Cancel`, `PhosphorIcon.XCircle`) plus the private `Resolve` helper that PHASE02's
  navigation items and PHASE04's settings card will reuse. Each outline is resolved once and held,
  because `ToGeometry()` re-parses the path data and hands out a fresh mutable `Geometry` per call.
- `docs/done/FEATURE-5CBC-PHASE01.md` — this file.

**Modified**

- `Directory.Packages.props` — four `PackageVersion` entries added to the Avalonia version-coupled
  group, with a comment recording *why* they belong there (the library pins Avalonia 12.1.1,
  Avalonia.Themes.Fluent 12.1.1 and CommunityToolkit.Mvvm 8.4.2 — the exact versions already in that
  group) and that `Enigma.Core` + `BouncyCastle.Cryptography` arrive transitively with no opt-out.
- `src/Enigma.HardCopy.Desktop/Enigma.HardCopy.Desktop.csproj` — the four version-less
  `PackageReference`s.
- `src/Enigma.HardCopy.Desktop/App.axaml` — `ThemeDictionaries` block (18 `SolidColorBrush`
  declarations across two variants) deleted; `ResourceInclude` of
  `avares://Enigma.Avalonia.Desktop/Themes/Fluent.axaml` merged into `Application.Resources`;
  `<FluentTheme />` kept in `Application.Styles`; all 17 brush references in the style classes
  repointed. The comments now record the three constraints that are easy to break later: why
  `ResourceInclude` and not `StyleInclude`, why `FluentTheme` stays, and why an override of an
  `Enigma*` key must be merged *after* the library's dictionary.
- `src/Enigma.HardCopy.Desktop/Views/MainWindow.axaml` — `Background="{DynamicResource
  EnigmaBackgroundBrush}"`.
- `src/Enigma.HardCopy.Desktop/Views/BackupView.axaml` — the `ei` namespace, and the two command
  buttons restructured from `Content="…"` into an icon + label `StackPanel`.
- `docs/roadmap.md`, `docs/plan/FEATURE-5CBC.md` — statuses.

**Deleted** — nothing.

**Untouched, as the plan requires** — all of `src/Enigma.HardCopy.Core/`, every test file, `App.axaml.cs`,
`Program.cs`, `ViewLocator.cs`, `ViewModels/`, `Services/`, `Views/RecoverView.axaml`,
`Views/*.axaml.cs`, `Resources/Strings.*`, `app.manifest`, `Directory.Build.props`.

## Brush mapping applied

| Retired (deleted from `App.axaml`) | Replacement | Where |
|---|---|---|
| `SubtleForegroundBrush` | `EnigmaForegroundSecondaryBrush` | `TextBlock.body`, `TextBlock.hint` |
| `CardBackgroundBrush` | `EnigmaSurfaceBrush` | `Border.card` |
| `CardBorderBrush` | `EnigmaBorderSubtleBrush` | `Border.card`, `Border.notice` |
| `SuccessBackgroundBrush` | `EnigmaSuccessBackgroundBrush` | `Border.notice.success` |
| `WarningBackgroundBrush` | `EnigmaWarningBackgroundBrush` | `Border.notice.warning` |
| `ErrorBackgroundBrush` | `EnigmaErrorBackgroundBrush` | `Border.notice.error` |
| `SuccessForegroundBrush` | `EnigmaSuccessBorderBrush` / `EnigmaSuccessBrush` | notice border / text + `:is(TextBlock).success` |
| `WarningForegroundBrush` | `EnigmaWarningBorderBrush` / `EnigmaWarningBrush` | notice border / text + `:is(TextBlock).warning` |
| `ErrorForegroundBrush` | `EnigmaErrorBorderBrush` / `EnigmaErrorBrush` | notice border / text + `:is(TextBlock).error` |

The single retired key became two replacements for the three severities because the library ships a
`…Background` / `…Border` pair per severity — the pair its own `InfoBar` uses — so the notice `Border`
takes `Enigma…BorderBrush` while the text inside it takes the `Enigma…Brush` accent, exactly as the
plan specified.

All 26 brush keys and 29 colour keys in the shipped dictionary were enumerated out of
`Enigma.Avalonia.Desktop.dll` before the edit; every key referenced above is in that set. `grep` over
the whole solution confirms no retired key survives anywhere.

## Deviations & follow-ups

1. **`BackupView.axaml` / `RecoverView.axaml` needed no brush repointing.** The plan's step 3 says to
   repoint "the style classes and `BackupView.axaml` / `RecoverView.axaml`", but neither view names a
   brush: every colour reaches them through the `App.axaml` style classes. That part of the step was a
   no-op, not a skipped one.
2. **`Resources/AppIcons.cs` is a file the plan did not list.** Plan step 5 asks for "one `Geometry`
   built from C#" without saying where it lives. A static outline catalogue is the natural home, and it
   is what PHASE02's `NavigationItem`s and PHASE04's settings card need anyway. It is consumed
   immediately — the *Cancel* button binds it by `x:Static` — so the C# route is proven at run time and
   not merely at compile time.
3. **`PhosphorIcon.XCircle` for *Cancel*** is not in the plan's pre-verified icon list, because the
   plan never assigned an icon to *Cancel*. It was checked against `Enigma.Icons.Phosphor.xml` before
   use, as the plan requires of any substitution.
4. **Secondary text is materially dimmer in Dark than it was.** Measured off the render, hint and body
   text on a card now sits around **2.5–3.4 : 1** against its surface, where the retired
   `SubtleForegroundBrush` (`#C5C5C5` on `#2C2C2C`) gave **8.1 : 1**; in Light the change is small
   (≈4.1–4.5 : 1 against the retired 6.3 : 1). The Dark figures are below WCAG AA for body text. This
   is the library's own `EnigmaForegroundSecondary` calibration used exactly as the library documents
   it ("descriptions, captions"), and the plan mandated this mapping — so nothing was changed here.
   Worth raising with the library rather than overriding the key downstream, which would put this app's
   palette back out of step with the house one. (Pixel-sampled figures are approximate: subpixel
   antialiasing makes small-text colour sampling imprecise. The surfaces themselves measured exactly —
   Dark `#1E1F22` background / `#2B2D30` surface, Light `#EBEDF0` / `#F7F8FA`.)
5. **`notice.warning`'s fill was not rendered.** Reaching it needs a real oversized input or a
   metadata-less session. Its two keys exist in the dictionary, its structure is identical to
   `notice.error` (which *was* rendered, through the real "that text holds no code" path), and PHASE03
   replaces these blocks with the library's `InfoBar` and covers the severities with its own fakes.
6. **Artifact size not re-measured.** The plan estimates ~9 MB per RID from the new assemblies and
   records it in PHASE05's release notes; PHASE01 publishes nothing, so nothing was measured here.
7. **Line endings:** nothing to report. Every touched file is LF and `.gitattributes` already declares
   `* text=auto eol=lf`; no action taken, per the workflow's recommendation-only rule.

## Build/test evidence

```
dotnet clean && dotnet build -c Debug   --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet clean && dotnet build -c Release --no-incremental  → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --solution Enigma.HardCopy.slnx               → total: 587  failed: 0  succeeded: 587  skipped: 0
dotnet test --solution Enigma.HardCopy.slnx -c Release    → total: 587  failed: 0  succeeded: 587  skipped: 0
```

**`AVLN*` warnings: zero.** Because `TreatWarningsAsErrors` does not promote them, both full
`--no-incremental` build logs were searched for the string `AVLN` explicitly — 0 hits in each, on top
of the 0-warning summary. 587 is the identical count to the pre-phase baseline, and **no test file was
touched**: `BackupViewModelTests`, `RecoverViewModelTests`, `OutcomeMessagesTests`,
`StatusMessageTests`, `ChunkSizeOptionTests`, `StringsTests` and `MainWindowViewModelTests` all pass
unmodified.

`dotnet list package --include-transitive` confirms the resolution: `Enigma.Avalonia.Desktop` 1.0.0,
`Enigma.Icons` / `.Phosphor` / `.Avalonia` 1.0.0, and transitively `Enigma.Core` 1.0.0 +
`BouncyCastle.Cryptography` 2.6.2 — the dependency the plan expected and accepted.

### Verified in the running app (Linux, `DISPLAY=:1`)

The session is KDE Plasma on Wayland with a **rootless Xwayland**, so `x11grab` on the X root captures
an empty screen; the window has to be grabbed by its own X window id
(`ffmpeg -f x11grab -window_id …`). Recorded here because it will bite every later phase's screenshot
step.

Both theme variants, both pages:

- **Dark** (`RequestedThemeVariant="Default"` on a dark desktop) — window `#1E1F22`, cards `#2B2D30`
  with a subtle 1 px border, `h1` at full foreground, body/hint secondary, and the `ComboBox` and
  `TextBox` restyled by the library dictionary with no hand-styling. The activity log renders all four
  severities legibly on the card, and the `notice.error` block shows the correct red-tinted fill with a
  red border.
- **Light** (forced via a temporary `RequestedThemeVariant="Light"`) — the same layout on `#EBEDF0` /
  `#F7F8FA`, every severity and notice still reading correctly.
- **Both icons render.** The `FilePdf` glyph sits on *Generate PDF* and the `XCircle` glyph on
  *Cancel*, each inheriting its button's `Foreground` (both dimmed with their disabled buttons, which
  is the correct behaviour and confirms the icons are not painted with a frozen brush).

Two **temporary scaffolds** were used to reach states the UI cannot show at rest, and **both were
reverted before this commit**: a block in `App.axaml.cs` that seeded the activity log with one message
per severity, fed the manual-code box non-code text through the real `AddCodesCommand` to raise a real
outcome notice, and selected the page from an env var; and `IsVisible="True"` on the *Cancel* button so
its icon could be photographed. `git diff` confirms `App.axaml.cs` is byte-identical to `HEAD` and that
`RequestedThemeVariant` is back to `Default`; a `grep` for `scaffold`, `HC_SHOT_PAGE`, `DispatcherTimer`
and forced visibility over `src/` and `tests/` returns nothing. The final screenshot was taken from the
exact code being committed.

## Acceptance criteria

| # | Criterion | Status |
|---|---|---|
| 1 | Four packages in `Directory.Packages.props` (Desktop group, with the coupling comment) + version-less `PackageReference`s | ✅ |
| 2 | `<FluentTheme />` kept; library dictionary merged as `ResourceInclude`; `ThemeDictionaries` block deleted | ✅ |
| 3 | Every brush reference repointed to its `Enigma*` equivalent, all `DynamicResource`; every style class kept | ✅ (the views needed none — see *Deviations* 1) |
| 4 | `MainWindow` `Background="{DynamicResource EnigmaBackgroundBrush}"` | ✅ |
| 5 | Icon markup extension **and** the C# `ToGeometry()` route both resolve | ✅ (both rendering in the app) |
| 6 | Debug **and** Release build clean — zero warnings **and** zero `AVLN*` | ✅ (0 / 0, `AVLN` grep = 0 hits in both logs) |
| 7 | Full suite green with **no test changes at all** | ✅ (587/587 Debug + Release, 0 test files touched) |
| 8 | App launches; panels, lists, notices and status colours read correctly in both variants (screenshots) | ✅ (see above; one caveat recorded in *Deviations* 4–5) |
| 9 | No retired brush key remains anywhere in the solution (`grep`) | ✅ |
