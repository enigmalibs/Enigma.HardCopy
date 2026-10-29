using System;
using System.Linq;
using System.Threading;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests.Services;

/// <summary>
/// The safety net's reporter: every error logged, one dialog for a burst, a dialog that waits for its host, and
/// a dialog that fails falling back to the log — over a fake dialog, with the UI thread replaced by the calling one.
/// </summary>
/// <remarks>
/// Each test runs with no synchronization context, so the reporter's <c>await</c> on the dialog continues inline
/// when the test closes it, and what follows the close is asserted straight after it.
/// </remarks>
public sealed class UnhandledErrorReporterTests
{
    private readonly FakeErrorDialog _dialog = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly RecordingLogger<UnhandledErrorReporter> _logger = new();

    [Fact]
    public void AReport_ShowsOneDialog_WithTheError()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();
            Exception error = Thrown(new InvalidOperationException("the backup failed"));

            reporter.Report(error, UnhandledErrorSource.Dispatcher);

            UnhandledErrorBatch batch = Assert.Single(_dialog.Shown);
            Assert.Same(error, batch.First);
        });

    [Fact]
    public void AnErrorWhileTheDialogIsOpen_IsCountedIntoIt_NotShownAgain()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("first")), UnhandledErrorSource.Dispatcher);
            reporter.Report(Thrown(new ArgumentException("second")), UnhandledErrorSource.FireAndForget);

            UnhandledErrorBatch batch = Assert.Single(_dialog.Shown);
            Assert.Equal(1, batch.More);
        });

    [Fact]
    public void AnErrorBeforeTheHostIsFree_WaitsForIt_ThenShows()
        => WithoutContext(() =>
        {
            _dialog.Ready = false;
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("early")), UnhandledErrorSource.Startup);
            Assert.Empty(_dialog.Shown);

            _dialog.BecomeAvailable();

            Assert.Single(_dialog.Shown);
        });

    [Fact]
    public void EveryError_IsLoggedAtError_WithWhereItWasCaught()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("failed")), UnhandledErrorSource.UnobservedTask);

            (LogLevel level, string message) = Assert.Single(_logger.Entries);
            Assert.Equal(LogLevel.Error, level);
            Assert.Contains("UnobservedTask", message, StringComparison.Ordinal);
        });

    [Fact]
    public void AnAppDomainError_IsLogged_ButNoDialogComes()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("the process ends")), UnhandledErrorSource.AppDomain);

            Assert.Empty(_dialog.Shown);
            Assert.Equal(LogLevel.Error, Assert.Single(_logger.Entries).Level);
        });

    [Fact]
    public void ACancellationSomeoneAskedFor_ShowsNothing_AndIsNoError()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            reporter.Report(new OperationCanceledException(cancelled.Token), UnhandledErrorSource.FireAndForget);

            Assert.Empty(_dialog.Shown);
            Assert.DoesNotContain(_logger.Entries, entry => entry.Level >= LogLevel.Error);
        });

    [Fact]
    public void AnyCancellation_OnceShutdownHasStarted_ShowsNothing()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.ShutdownStarted();
            reporter.Report(new OperationCanceledException(), UnhandledErrorSource.FireAndForget);

            Assert.Empty(_dialog.Shown);
        });

    [Fact]
    public void ADialogThatFails_IsLogged_AndLaterErrorsAreOnlyLogged()
        => WithoutContext(() =>
        {
            _dialog.FailWith = new InvalidOperationException("no host");
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("first")), UnhandledErrorSource.Dispatcher);
            _dialog.FailWith = null;
            reporter.Report(Thrown(new InvalidOperationException("second")), UnhandledErrorSource.Dispatcher);

            Assert.Empty(_dialog.Shown);
            Assert.Contains(_logger.Entries, entry => entry.Message.Contains("could not be shown", StringComparison.Ordinal));
            Assert.Equal(3, _logger.Entries.Count(entry => entry.Level == LogLevel.Error));
        });

    [Fact]
    public void TheSameErrorAgain_IsNotShownForAMinuteAfterItsDialogClosed()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Repeated(), UnhandledErrorSource.Dispatcher);
            _dialog.Close();
            reporter.Report(Repeated(), UnhandledErrorSource.Dispatcher);

            Assert.Single(_dialog.Shown);

            _clock.Advance(UnhandledErrorPolicy.RepeatWindow + TimeSpan.FromSeconds(1));
            reporter.Report(Repeated(), UnhandledErrorSource.Dispatcher);

            Assert.Equal(2, _dialog.Shown.Count);
        });

    [Fact]
    public void ADifferentError_AfterTheDialogClosed_OpensANewOne()
        => WithoutContext(() =>
        {
            UnhandledErrorReporter reporter = Reporter();

            reporter.Report(Thrown(new InvalidOperationException("first")), UnhandledErrorSource.Dispatcher);
            _dialog.Close();
            reporter.Report(Thrown(new ArgumentException("another")), UnhandledErrorSource.Dispatcher);

            Assert.Equal(2, _dialog.Shown.Count);
        });

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(null!, _clock, _logger));
        Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_dialog, null!, _logger));
        Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_dialog, _clock, null!));
    }

    private UnhandledErrorReporter Reporter() => new(_dialog, _clock, _logger, static action => action());

    private static void WithoutContext(Action body)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        try
        {
            body();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>An exception with a stack trace, as one that was really thrown has.</summary>
    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    /// <summary>The same failure, thrown again from the same place: a new instance each time.</summary>
    private static Exception Repeated() => Thrown(new InvalidOperationException("the save failed"));
}
