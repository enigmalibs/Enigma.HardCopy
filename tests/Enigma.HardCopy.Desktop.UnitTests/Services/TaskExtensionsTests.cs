using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests.Services;

/// <summary>
/// Work started and not awaited: its failure reaches the safety net, whenever it comes; a success or a
/// cancellation says nothing.
/// </summary>
/// <remarks>
/// In the process-hooks collection, as every test that attaches a reporter to the process-wide hooks.
/// </remarks>
[Collection(ProcessHooksCollection.Name)]
public sealed class TaskExtensionsTests : IDisposable
{
    private readonly RecordingErrorReporter _reporter = new();

    public TaskExtensionsTests()
    {
        UnhandledExceptionHooks.ResetForTests();
        UnhandledExceptionHooks.Attach(_reporter);
    }

    public void Dispose() => UnhandledExceptionHooks.ResetForTests();

    [Fact]
    public void AFaultedTask_ReachesTheNet_AsFireAndForget()
    {
        InvalidOperationException error = new("the save failed");

        Task.FromException(error).Forget();

        Assert.Equal(UnhandledErrorSource.FireAndForget, _reporter.SourceOf(error));
    }

    [Fact]
    public void ATaskThatFaultsLater_StillReachesTheNet()
    {
        TaskCompletionSource running = new();
        InvalidOperationException error = new("failed after a while");

        running.Task.Forget();
        Assert.Null(_reporter.SourceOf(error));

        running.SetException(error);

        Assert.Equal(UnhandledErrorSource.FireAndForget, _reporter.SourceOf(error));
    }

    [Fact]
    public void EveryFailureOfTheTask_IsReported()
    {
        InvalidOperationException first = new("one");
        ArgumentException second = new("two");

        // Two failures of one task, as WhenAll gathers them.
        Task.WhenAll(Task.FromException(first), Task.FromException(second)).Forget();

        Assert.NotNull(_reporter.SourceOf(first));
        Assert.NotNull(_reporter.SourceOf(second));
    }

    [Fact]
    public void TheStartOfTheApplication_ReportsAsStartup()
    {
        InvalidOperationException error = new("the first file could not be opened");

        Task.FromException(error).Forget(UnhandledErrorSource.Startup);

        Assert.Equal(UnhandledErrorSource.Startup, _reporter.SourceOf(error));
    }

    [Fact]
    public void ASuccessOrACancellation_SaysNothing()
    {
        TaskCompletionSource later = new();

        Task.CompletedTask.Forget();
        Task.FromCanceled(new CancellationToken(canceled: true)).Forget();
        later.Task.Forget();
        later.SetResult();

        Assert.Empty(_reporter.Reports);
    }
}
