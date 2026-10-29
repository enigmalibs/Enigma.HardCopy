# FEATURE-555A-PHASE01 — Error policy and process hooks

## Summary

This phase adds the logic half of the unhandled-exception safety net, ported from Enigma.TextEditor's port of
Enigma.GitClient's net (TextEditor FEATURE-452D) with only the namespaces changed. It needs no window.

- **Policy** (`UnhandledErrorPolicy`, `UnhandledErrorBatch`, `UnhandledErrorSource`, `UnhandledErrorDecision`):
  - it shows one batch at a time and merges later errors into it;
  - the same error is not shown again for one minute after it was closed;
  - a cancellation that was asked for, or any cancellation during shutdown, is ignored;
  - `Summarise` unwraps single-inner wrappers;
  - `BuildDetails` lists type, message and stack, down inner exceptions to depth 8, and never reads `Data` or
    calls `ToString()`.
- **Hooks** (`UnhandledExceptionHooks`, static):
  - `Install` hooks AppDomain and `UnobservedTaskException`;
  - `InstallDispatcher`;
  - `Attach` replays up to 32 early reports;
  - `Report` traces the type and message while no reporter is attached;
  - `Detach`;
  - an internal `ResetForTests`.
- **`Forget()`** (`TaskExtensions`, a C# 14 `extension` block) observes un-awaited work, and hands a fault to the
  hooks as `FireAndForget`, or `Startup` through the overload.
- **`Program.Main`** calls `UnhandledExceptionHooks.Install()` before anything else.

The dialog, the reporter and the wiring into `App` are PHASE02.

## Files/modules touched

- Created: `src/Enigma.HardCopy.Desktop/Services/UnhandledErrorSource.cs`, `UnhandledErrorDecision.cs`,
  `UnhandledErrorBatch.cs`, `UnhandledErrorPolicy.cs`, `IUnhandledErrorReporter.cs`,
  `UnhandledExceptionHooks.cs`, `TaskExtensions.cs`
- Modified: `src/Enigma.HardCopy.Desktop/Program.cs`, `src/Enigma.HardCopy.Desktop/Enigma.HardCopy.Desktop.csproj`
  (`InternalsVisibleTo` the Desktop tests)
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/Services/UnhandledErrorPolicyTests.cs`,
  `UnhandledExceptionHooksTests.cs`, `TaskExtensionsTests.cs`, `ProcessHooksCollection.cs`
- Created: `tests/Enigma.HardCopy.Desktop.UnitTests/TestDoubles/ManualTimeProvider.cs`,
  `RecordingErrorReporter.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-555A.md`

## Deviations & follow-ups

- **One type per file.** The plan put the policy types in one `UnhandledErrorPolicy.cs`. They are four files, as in
  TextEditor and as everywhere else in this codebase.
- **No dispatcher test.** TextEditor's *a callback that throws on the UI thread is reported* test runs under its
  headless Avalonia fixture, and this suite has no Avalonia platform. The dispatcher hook is verified in the running
  app in PHASE02, with a temporary throw that is removed before that commit.
- The hooks and `Forget` tests run in a `ProcessHooksCollection` with `DisableParallelization = true`, because the
  hooks' state belongs to the whole process.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Source of the port | TextEditor's files | They are GitClient's net already cut down to a single-window app, and tested |
| `ResetForTests` visibility | `internal`, with `InternalsVisibleTo` added to the Desktop csproj | Only the tests may reset process state. The app has no other internal seam yet |
| Test doubles | `ManualTimeProvider` and `RecordingErrorReporter` as `internal sealed` in `TestDoubles/` | The house shape of this suite's doubles |

## Build/test evidence

- `dotnet build Enigma.HardCopy.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.HardCopy.slnx --no-build` gave **845 passed**, 0 failed, up from 818. The 27 new
  tests cover:
  - the policy: the queue, merges, suppression and re-showing after the window on a manual clock, the same
    exception offered twice, cancellations, `Summarise` and the details;
  - the hooks: early reports replayed in order up to the limit, live reports, nothing after `Detach`;
  - `Forget`: faulted now, faulted later, several faults, `Startup`, success and cancellation silent.
- Doc sweep: nothing user-visible changes in this phase. No edit.
