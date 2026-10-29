# FEATURE-555A — Catch unhandled exceptions

**Status:** DONE
**Type:** FEATURE (2 phases)
**Branch:** `feature/feature-555a-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every exception that nothing else handles in the desktop app reaches one safety net, which logs it and shows an
error content dialog saying the application is still running: the error, a count of further errors, collapsed
details and *Copy details*. It is Enigma.GitClient's net, as Enigma.TextEditor ported it (FEATURE-452D), so the
Enigma apps fail alike.

## Context & constraints

- **Reference** (`C:\Dev\EnigmaLibs\Enigma.GitClient\src\Enigma.GitClient.Desktop\`): `Services/UnhandledErrorPolicy.cs`
  (`UnhandledErrorSource`, `UnhandledErrorDecision`, `UnhandledErrorBatch`, `UnhandledErrorPolicy`),
  `Services/UnhandledExceptionHooks.cs`, `Services/UnhandledErrorReporter.cs`,
  `ViewModels/Dialogs/UnhandledErrorViewModel.cs`, `Views/Dialogs/UnhandledErrorView.axaml`,
  `Services/TaskExtensions.cs`. The single-window port is `C:\Dev\EnigmaLibs\Enigma.TextEditor\src\` (same names).
- **Does not carry over:** several windows (`IAppWindows`), the token reset, `ArgumentRedactor` — one `MainWindow`
  here, whose `HostDialog` every question shares, and no credentials.
- **Today nothing is caught:** `Program.Main` starts the lifetime; `App.OnFrameworkInitializationCompleted` builds
  and starts the host; one fire-and-forget discard (`_ = ShowAsync(…)` in `Services/NotificationService.cs`).
- **Logging:** console + debug sinks only, no log file — nothing to flush.
- **The secret rule:** `ConfirmationService` documents that no dialog may carry code text, because a
  `ContentDialog` keeps its content until the next one is shown.
- Conventions: flat `Services/`, `ViewModels/`, `Views/`; strings in `Strings.resx`; C# 14 `extension` blocks;
  tests without an Avalonia platform, fakes in `TestDoubles/`.

## PHASE01 — Error policy and process hooks

**Status:** DONE
**Branch:** `feature/feature-555a-phase01-policy-hooks`

### Steps

1. `Services/UnhandledErrorPolicy.cs`: GitClient's source/decision enums, `UnhandledErrorBatch` and
   `UnhandledErrorPolicy` — one batch at a time with later errors merged; the same error suppressed for 1 minute
   after its dialog closed (`TimeProvider`); a requested cancellation, or any while shutting down, ignored;
   `Summarise` (first meaningful message, unwrapping single-inner `AggregateException` / `TargetInvocationException`);
   `BuildDetails` (type, message, stack, inner exceptions to depth 8 — never `Data`, never `ToString()`).
2. `Services/IUnhandledErrorReporter.cs`: `Report(Exception, UnhandledErrorSource)`, `ShutdownStarted()`.
3. `Services/UnhandledExceptionHooks.cs` (static): `Install` (AppDomain + `TaskScheduler.UnobservedTaskException`,
   `SetObserved`), `InstallDispatcher` (`Dispatcher.UIThread.UnhandledException`, `Handled = true`), `Attach`
   (replays early reports, at most 32), `Detach`, `Report` (traces while nothing is attached); an `internal` reset
   seam for tests (`InternalsVisibleTo` the Desktop tests).
4. `Services/TaskExtensions.cs`: `Forget(this Task)` in a C# 14 `extension` block — observes the task; a fault
   that is not an ignored cancellation goes to `UnhandledExceptionHooks.Report(…, FireAndForget)`.
5. `Program.Main`: `UnhandledExceptionHooks.Install()` first.
6. Tests: policy (ignore, queue, merge, suppress then show again with a fake `TimeProvider`, the same exception
   offered twice, `Summarise` unwrapping, details with inner exceptions and the depth cap); batch (`More`,
   `Details`, `Changed`); hooks (early reports replayed and bounded, live reports, nothing after `Detach`);
   `Forget` (faulted → reported, completed / cancelled → nothing, faulting later → reported).

### Acceptance criteria

- `Program.Main` installs the process hooks before anything else.
- Policy, hooks and `Forget` covered by tests; build with zero warnings; whole suite green.

## PHASE02 — Error dialog and wiring

**Status:** DONE
**Branch:** `feature/feature-555a-phase02-error-dialog`

### Steps

1. `ViewModels/UnhandledErrorViewModel.cs` (`ObservableObject`): `Lead`, `Message`, `HasMore`, `MoreText`,
   `Details`, `CopyDetailsCommand` through an injected `Func<string, Task>`; follows `batch.Changed`.
2. `Views/UnhandledErrorView.axaml(.cs)`: GitClient's layout — lead, message (SemiBold), "N more errors" in the
   secondary brush, a collapsed *Details* expander with a read-only monospace `TextBox` (max 220 px), *Copy
   details*; `MinWidth` 480, `MaxWidth` 560 on the texts; the same Enigma brushes and automation names.
3. `Services/IErrorDialog.cs` + `Services/ErrorDialog.cs`: shows one batch on `IContentDialogService` — title from
   `Strings`, *Close* (default), GitClient's warning icon in `EnigmaWarningBrush`; clipboard through
   `TopLevel.GetTopLevel(host)?.Clipboard`; while the host already shows a question (`ContentDialog.IsOpen`) it
   waits for that question's `Closed`; when the dialog closes, its content is cleared.
4. `Services/UnhandledErrorReporter.cs` (`IUnhandledErrorReporter`): logs on the reporting thread at Error with
   the source; decides on the UI thread through the policy; shows through `IErrorDialog`; a failing dialog is
   logged and the reporter falls back to logging only.
5. DI (`AddEnigmaHardCopyDesktop`): `TimeProvider.System` already registered; `IErrorDialog`, the policy and
   `IUnhandledErrorReporter` as singletons.
6. `App.OnFrameworkInitializationCompleted`: `InstallDispatcher()` first; `Attach(reporter)` once the hosts are
   registered; `desktop.ShutdownRequested` and `Exit` → `ShutdownStarted()`, then `Detach()`.
7. `NotificationService`: `_ = ShowAsync(…)` → `ShowAsync(…).Forget()`.
8. Strings: the title, lead, "1 more error occurred." / "{0} more errors occurred.", *Details*, *Copy details*,
   *Close*.
9. Tests: reporter with a fake `IErrorDialog` and a synchronous UI-thread seam — a report shows one batch; a second
   while open merges (`More` = 1); a cancellation shows nothing; a failing dialog falls back to logging; logging at
   Error with the source; `UnhandledErrorViewModel` (texts, `HasMore`, copy delegate receives the details); DI
   resolves the reporter.
10. README: a sentence on what the app does when something unexpected fails.

### Acceptance criteria

- An exception thrown by a command or a posted callback on the UI thread shows the dialog and the app keeps
  running (checked by running the app with a temporary throw, removed before the commit).
- A burst of errors opens one dialog; the dialog never replaces a question on screen.
- `grep -rn "_ = .*Async(" src/Enigma.HardCopy.Desktop` finds nothing.
- Build with zero warnings; whole suite green.

## Out of scope

- A dialog for `AppDomain.UnhandledException`: .NET ends the process right after; it is logged only.
- The Android Recover app (its process is killed by Android; follow-up).
- Restart or *Send report* buttons; any change to existing outcome messages.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Design | Port GitClient's net (policy, hooks, reporter, dialog), as TextEditor did | "Uniform" Enigma apps; tested; handles bursts and open questions | A `try/catch` in `Main`; one dialog call per hook |
| Which app | Desktop only | The spec's baseline is the desktop one; Recover runs under Android's lifetime and the FLAG_SECURE rules | Both apps in this item |
| Phasing | Logic (with `Forget`) / dialog + wiring | Only one discard exists, so TextEditor's third phase folds into these two; each is one reviewable commit | 3 phases |
| Dialog seam | `IErrorDialog` between the reporter and the ContentDialog | No headless platform here: the seam makes the reporter testable, like `IConfirmationService` | Testing the reporter only by running the app |
| Code text in the dialog | Details are type/message/stack only, and the dialog content is cleared on close | The codes are the secret; a ContentDialog keeps its content until the next question | GitClient's behaviour unchanged |
| AppDomain errors | Logged only | The process ends right after; console/debug sinks need no flush | Trying to show a dialog |
| Copying details | A `Func<string, Task>` over `TopLevel.Clipboard`, injected into the VM | No clipboard service exists; the VM stays testable | A new `IClipboardService` |
| Strings | In `Strings.resx` | House rule of this app | GitClient's literals |
