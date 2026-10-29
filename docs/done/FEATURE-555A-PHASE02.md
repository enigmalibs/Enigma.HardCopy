# FEATURE-555A-PHASE02 — Error dialog and wiring

## Summary

The safety net is now live in the desktop app. Every unhandled exception is logged at Error with where it was
caught. A UI-thread, unobserved-task or fire-and-forget failure is also shown in a *Something went wrong* dialog,
which uses Enigma.GitClient's layout:

- the warning icon in the theme's warning brush;
- the lead saying the application is still running;
- the message, in SemiBold;
- "N more errors occurred." for the errors that arrive while it is open;
- a collapsed *Details* with a monospace read-only box and *Copy details*;
- *Close*.

The pieces:

- **`UnhandledErrorReporter`**: logs on the reporting thread and decides on the UI thread through the policy. It
  shows each batch through `IErrorDialog`, and goes back to logging only if the dialog itself fails.
- **`IErrorDialog` / `ErrorDialog`**: the seam between the decisions and the window.
  - `ErrorDialog` shows on the window's `HostDialog` through the library's `IContentDialogService`.
  - It waits for a question on screen to be answered, and for the host to be handed over.
  - It drops its content when it closes.
  - It copies through `TopLevel.Clipboard`.
- **`UnhandledErrorViewModel` / `UnhandledErrorView`**: GitClient's, as TextEditor ported them, with every text in
  `Strings.resx`.
- **`App`**:
  - `InstallDispatcher()` comes first;
  - the error dialog gets the window's host;
  - the reporter is attached to the hooks;
  - `ShutdownRequested` → `ShutdownStarted()`;
  - on `Exit`, `ShutdownStarted()` and `Detach()` run before the host stops.
- **`NotificationService`**: its one `_ = ShowAsync(…)` discard is now `.Forget()`.

## Files/modules touched

- Created: `src/Enigma.HardCopy.Desktop/Services/IErrorDialog.cs`, `ErrorDialog.cs`, `UnhandledErrorReporter.cs`
- Created: `src/Enigma.HardCopy.Desktop/ViewModels/UnhandledErrorViewModel.cs`, `Views/UnhandledErrorView.axaml(.cs)`
- Modified: `src/Enigma.HardCopy.Desktop/App.axaml.cs`, `Hosting/ServiceCollectionExtensions.cs`,
  `Resources/AppIcons.cs`, `Resources/Strings.resx`, `Resources/Strings.cs`, `Services/NotificationService.cs`
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/Services/UnhandledErrorReporterTests.cs`,
  `UnhandledErrorViewModelTests.cs`, `TestDoubles/FakeErrorDialog.cs`
- Modified: `tests/Enigma.HardCopy.Desktop.UnitTests/Hosting/ServiceCollectionExtensionsTests.cs`
- Modified: `README.md` (*Getting around*)
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-555A.md`

## Deviations & follow-ups

- **The reporter is `internal`, with a second, internal constructor** that takes the UI-thread `post`. The
  container sees only the public constructor, which uses the dispatcher. That is what lets the reporter be tested
  without a platform.
- **AppDomain errors** are logged and nothing more, as planned. One caveat, recorded as a follow-up: the console
  sink writes through a background queue, so the last record may not reach a terminal before .NET ends the
  process. The Debug sink is synchronous. A packaged `WinExe` has no console anyway.
- **The Android Recover app has no safety net.** That is the follow-up the plan's *Out of scope* names.
- The in-app check used a temporary throw. A screenshot was not possible, because the desktop session showed
  another screen. The check was confirmed from the app's log instead, and the dialog's look by a headless render.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the dialog waits | `IErrorDialog.IsAvailable()` arms a wait. `Available` is raised, posted at Background, when the host arrives or the open question closes | TextEditor's `RegisterHost` / `Closed` logic, moved behind the seam so the reporter's half is testable |
| One `ErrorDialog` under two names | `AddSingleton<ErrorDialog>()` + `IErrorDialog` forwarded to it | `App` needs the concrete type to hand over the host. Two instances would wait for a host that never comes, and a test pins that |
| Clearing the content | `host.Content = null` after close, only if it is still this dialog's | The house rule that a ContentDialog's content must not outlive its question (`ConfirmationService`) |
| Icon colour | `EnigmaWarningBrush` (follows the variant) | This app's confirmations use it. GitClient's literal `#E8A33D` would ignore the variant |
| App's references to app services | Type aliases (`ErrorDialog`, `ExceptionHooks`, `IUnhandledErrorReporter`) | `App.axaml.cs` imports the library's `Services` namespace and avoids the app's (`IFileDialogService` clash), as its `AppTheming` alias already does |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors. This was after the temporary throw was
  removed.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **865 passed**, 0 failed, up from 845. The new
  tests are:
  - 11 reporter tests: one dialog shown; a second error merged; waiting for the host; logged at Error with the
    source; AppDomain logged but not shown; requested cancellations and post-shutdown cancellations silent; a
    failing dialog falls back to the log; a repeat silent for a minute, then shown; a different error after close
    opens a new dialog; null guards;
  - 5 ViewModel tests: texts, singular and plural count, property changes, the copy delegate, null guards;
  - DI: lifetimes, resolution, and one `ErrorDialog` instance under both names.
- In the running app, a temporary `Dispatcher.UIThread.Post(() => throw …)` and a `Task.Run(…).Forget()` that
  faults after 1.5 s were added. The log shows `An unhandled exception reached the safety net (Dispatcher)` and
  `(FireAndForget)`, and the process was still running 9 s later. The temporary code was then removed; `grep TEMP`
  finds nothing.
- A headless render of the dialog, through the real App theme, in Dark and Light, matches GitClient's layout.
- `grep -rn "_ = .*Async(" src/Enigma.HardCopy.Desktop` finds nothing.
- Doc sweep: README *Getting around* now says what happens on an unexpected failure.
