using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests.Services;

/// <summary>
/// The way into the safety net: what is reported before the reporter exists is kept and handed over, and
/// nothing reaches a reporter that was taken away. The process events are neither hooked nor raised here, and
/// neither is the dispatcher's — this suite runs with no Avalonia platform, and a dispatcher callback that
/// throws is checked in the running application instead. An exception that reaches
/// <see cref="AppDomain.UnhandledException"/> ends the test host too, and the test host's own unobserved
/// tasks would land among the reports under test.
/// </summary>
/// <remarks>
/// In the process-hooks collection, which runs alone: the hooks' state is the process's, so no other test may
/// report into it while one of these counts what arrived.
/// </remarks>
[Collection(ProcessHooksCollection.Name)]
public sealed class UnhandledExceptionHooksTests : IDisposable
{
    public UnhandledExceptionHooksTests() => UnhandledExceptionHooks.ResetForTests();

    public void Dispose() => UnhandledExceptionHooks.ResetForTests();

    [Fact]
    public void AReportBeforeTheReporter_IsHandedOverOnAttach_WithItsSource()
    {
        InvalidOperationException early = new("before the container");
        UnhandledExceptionHooks.Report(early, UnhandledErrorSource.Startup);
        RecordingErrorReporter reporter = new();

        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(UnhandledErrorSource.Startup, reporter.SourceOf(early));
    }

    [Fact]
    public void EarlyReports_AreKeptUpToTheLimit_InOrder()
    {
        List<Exception> early = [.. Enumerable.Range(0, UnhandledExceptionHooks.EarlyLimit + 5).Select(index => new InvalidOperationException($"early {index}"))];

        foreach (Exception exception in early)
        {
            UnhandledExceptionHooks.Report(exception, UnhandledErrorSource.UnobservedTask);
        }

        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(early.Take(UnhandledExceptionHooks.EarlyLimit), reporter.Reports.Select(report => report.Exception));
    }

    [Fact]
    public void AReportAfterAttach_GoesStraightToTheReporter_AndIsNotReplayedLater()
    {
        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        InvalidOperationException error = new("after the container");

        UnhandledExceptionHooks.Report(error, UnhandledErrorSource.Dispatcher);

        Assert.Equal(UnhandledErrorSource.Dispatcher, reporter.SourceOf(error));

        RecordingErrorReporter next = new();
        UnhandledExceptionHooks.Attach(next);
        Assert.Null(next.SourceOf(error));
    }

    [Fact]
    public void AfterDetach_NothingReachesTheReporter_AndReportsWaitForTheNextOne()
    {
        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        UnhandledExceptionHooks.Detach();
        InvalidOperationException late = new("while closing");

        UnhandledExceptionHooks.Report(late, UnhandledErrorSource.FireAndForget);

        Assert.Null(reporter.SourceOf(late));

        RecordingErrorReporter next = new();
        UnhandledExceptionHooks.Attach(next);
        Assert.Equal(UnhandledErrorSource.FireAndForget, next.SourceOf(late));
    }
}
