using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.UnitTests.TestDoubles;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests.Services;

/// <summary>
/// The safety net's policy: which exceptions are not errors, how a burst becomes one dialog, what is not
/// shown twice, and what the details say — without a window or a platform.
/// </summary>
public sealed class UnhandledErrorPolicyTests
{
    private readonly ManualTimeProvider _clock = new();

    // ---------------------------------------------------------------- one dialog for a burst

    [Fact]
    public void TheFirstError_OpensABatch()
    {
        UnhandledErrorPolicy policy = Policy();
        Exception error = Thrown(new InvalidOperationException("first"));

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(error));
        Assert.Same(error, policy.Pending?.First);
        Assert.Equal(0, policy.Pending?.More);
    }

    [Fact]
    public void ABurstBeforeTheDialogShows_IsOneBatch_CountingTheRest()
    {
        UnhandledErrorPolicy policy = Policy();

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(Thrown(new InvalidOperationException("first"))));
        Assert.Equal(UnhandledErrorDecision.Merge, policy.Offer(Thrown(new InvalidOperationException("second"))));
        Assert.Equal(UnhandledErrorDecision.Merge, policy.Offer(Thrown(new ArgumentException("third"))));

        UnhandledErrorBatch batch = Assert.IsType<UnhandledErrorBatch>(policy.BeginShow());

        Assert.Equal("first", batch.Message);
        Assert.Equal(2, batch.More);
        Assert.Contains("Error 1 of 3", batch.Details, StringComparison.Ordinal);
        Assert.Contains("second", batch.Details, StringComparison.Ordinal);
        Assert.Contains("System.ArgumentException: third", batch.Details, StringComparison.Ordinal);
        Assert.Null(policy.Pending);
        Assert.Same(batch, policy.Showing);
    }

    [Fact]
    public void AnErrorWhileTheDialogIsOpen_JoinsIt_AndSaysSo()
    {
        UnhandledErrorPolicy policy = Policy();
        policy.Offer(Thrown(new InvalidOperationException("first")));
        UnhandledErrorBatch batch = policy.BeginShow()!;
        int changes = 0;
        batch.Changed += (_, _) => changes++;

        Assert.Equal(UnhandledErrorDecision.Merge, policy.Offer(Thrown(new InvalidOperationException("later"))));

        Assert.Equal(1, batch.More);
        Assert.Equal(1, changes);
        Assert.Null(policy.Pending);
    }

    [Fact]
    public void AnErrorAfterTheDialogClosed_OpensANewBatch()
    {
        UnhandledErrorPolicy policy = Policy();
        policy.Offer(Thrown(new InvalidOperationException("first")));
        policy.BeginShow();
        policy.EndShow();

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(Thrown(new ArgumentException("another"))));
        Assert.Equal("another", policy.Pending?.Message);
        Assert.Null(policy.Showing);
    }

    [Fact]
    public void TheSameExceptionTwice_CountsOnce()
    {
        UnhandledErrorPolicy policy = Policy();
        Exception error = Thrown(new InvalidOperationException("twice"));

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(error));
        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(error));
        Assert.Equal(0, policy.Pending?.More);
    }

    [Fact]
    public void BeginShow_WithNothingPending_IsNothing()
        => Assert.Null(Policy().BeginShow());

    // ---------------------------------------------------------------- repeats

    [Fact]
    public void AnErrorJustClosed_IsNotShownAgain_ForAMinute()
    {
        UnhandledErrorPolicy policy = Policy();

        policy.Offer(Repeated());
        policy.BeginShow();
        policy.EndShow();

        _clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(UnhandledErrorDecision.Suppress, policy.Offer(Repeated()));
        Assert.Null(policy.Pending);

        _clock.Advance(UnhandledErrorPolicy.RepeatWindow);

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(Repeated()));
    }

    [Fact]
    public void AnErrorCountedIntoAClosedDialog_IsNotShownAgainEither()
    {
        UnhandledErrorPolicy policy = Policy();

        policy.Offer(Thrown(new ArgumentException("first")));
        policy.BeginShow();
        policy.Offer(Repeated());
        policy.EndShow();

        Assert.Equal(UnhandledErrorDecision.Suppress, policy.Offer(Repeated()));
    }

    // ---------------------------------------------------------------- cancellations

    [Fact]
    public void ACancellationWhileClosing_IsIgnored()
    {
        UnhandledErrorPolicy policy = Policy();
        policy.IsShuttingDown = true;

        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(Thrown(new OperationCanceledException())));
        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(new AggregateException(new OperationCanceledException())));
        Assert.Null(policy.Pending);
    }

    [Fact]
    public void ACancellationSomeoneAskedFor_IsIgnored()
    {
        using CancellationTokenSource source = new();
        source.Cancel();

        Assert.Equal(UnhandledErrorDecision.Ignore, Policy().Offer(Thrown(new OperationCanceledException(source.Token))));
    }

    [Fact]
    public void ACancellationNobodyAskedFor_IsAnError()
    {
        // A timeout: cancelled, but not by a token anyone cancelled.
        Assert.Equal(UnhandledErrorDecision.Queue, Policy().Offer(Thrown(new OperationCanceledException("timed out"))));
    }

    [Fact]
    public void AnAggregateWithOneRealError_IsAnError()
    {
        UnhandledErrorPolicy policy = Policy();
        policy.IsShuttingDown = true;

        AggregateException mixed = new(new OperationCanceledException(), new InvalidOperationException("real"));

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(mixed));
    }

    // ---------------------------------------------------------------- what the dialog says

    [Fact]
    public void TheMessage_IsTheOneInsideTheWrappers()
    {
        AggregateException wrapped = new(new TargetInvocationException(new InvalidOperationException("what happened")));

        Assert.Equal("what happened", UnhandledErrorPolicy.Summarise(wrapped));
    }

    [Fact]
    public void AnEmptyMessage_IsTheTypesName()
        => Assert.Equal(nameof(EmptyMessageException), UnhandledErrorPolicy.Summarise(new EmptyMessageException()));

    [Fact]
    public void TheDetails_HaveTheType_TheMessage_TheInnerExceptions_AndTheStack()
    {
        Exception error = Thrown(new InvalidOperationException("outer", Thrown(new ArgumentException("inner"))));

        string details = UnhandledErrorPolicy.BuildDetails(error);

        Assert.StartsWith("System.InvalidOperationException: outer", details, StringComparison.Ordinal);
        Assert.Contains("--- Inner exception ---", details, StringComparison.Ordinal);
        Assert.Contains("System.ArgumentException: inner", details, StringComparison.Ordinal);
        Assert.Contains(nameof(Thrown), details, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDetails_ListEveryExceptionOfAnAggregate()
    {
        AggregateException error = new(new InvalidOperationException("one"), new ArgumentException("two"));

        string details = UnhandledErrorPolicy.BuildDetails(error);

        Assert.Contains("--- Inner exception 1 of 2 ---", details, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException: one", details, StringComparison.Ordinal);
        Assert.Contains("--- Inner exception 2 of 2 ---", details, StringComparison.Ordinal);
        Assert.Contains("System.ArgumentException: two", details, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDetails_NeverCarryTheExceptionsData()
    {
        Exception error = Thrown(new InvalidOperationException("failed"));
        error.Data["path"] = "hung-on-the-exception-not-for-the-report";

        string details = UnhandledErrorPolicy.BuildDetails(error);

        Assert.DoesNotContain("hung-on-the-exception-not-for-the-report", details, StringComparison.Ordinal);
    }

    [Fact]
    public void ADeepChain_StopsSomewhere()
    {
        Exception error = new InvalidOperationException("0");

        for (int depth = 1; depth < 50; depth++)
        {
            error = new InvalidOperationException(depth.ToString(CultureInfo.InvariantCulture), error);
        }

        List<string> inners = [.. UnhandledErrorPolicy.BuildDetails(error).Split("--- Inner exception ---")];

        Assert.InRange(inners.Count, 2, 10);
    }

    private UnhandledErrorPolicy Policy() => new(_clock);

    /// <summary>
    /// An exception with a stack trace, as one that was really thrown has.
    /// </summary>
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

    /// <summary>
    /// The same failure, thrown again from the same place: a new instance each time.
    /// </summary>
    private static Exception Repeated() => Thrown(new InvalidOperationException("the save failed"));

    private sealed class EmptyMessageException() : Exception(" ");
}
