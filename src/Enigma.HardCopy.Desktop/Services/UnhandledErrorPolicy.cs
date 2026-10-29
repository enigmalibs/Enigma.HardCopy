using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Decides what the safety net shows: which exceptions are not errors at all, which wait for the dialog
/// already there, and what the details say. Nothing here touches a window, so it is tested without a
/// platform.
/// </summary>
/// <remarks>
/// <para>
/// Enigma.GitClient's policy, so the Enigma apps fail alike. One dialog at a time: an error that comes
/// while one waits for its host, or while one is on screen, is counted into it ("2 more errors occurred")
/// rather than queued behind it, so a loop of failures does not open a dialog per failure. An error
/// identical to one the reader closed less than <see cref="RepeatWindow"/> ago is logged and not shown
/// again.
/// </para>
/// <para>
/// Every member is called on the UI thread, except <see cref="IsIgnoredCancellation"/> and the static
/// helpers, which only read.
/// </para>
/// </remarks>
public sealed class UnhandledErrorPolicy
{
    /// <summary>
    /// How long an error the reader has seen is not shown again.
    /// </summary>
    public static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How deep the details follow inner exceptions before they stop.
    /// </summary>
    private const int MaximumDepth = 8;

    private static readonly object Seen = new();

    private readonly TimeProvider _time;
    private readonly ConditionalWeakTable<Exception, object> _offered = new();
    private readonly Dictionary<string, long> _closedAt = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorPolicy"/> class.
    /// </summary>
    /// <param name="time">Measures how long ago an error was seen.</param>
    public UnhandledErrorPolicy(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        _time = time;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application is closing, when no cancellation is worth
    /// a word.
    /// </summary>
    public bool IsShuttingDown { get; set; }

    /// <summary>
    /// Gets the batch waiting to be shown, or <see langword="null"/>.
    /// </summary>
    public UnhandledErrorBatch? Pending { get; private set; }

    /// <summary>
    /// Gets the batch on screen, or <see langword="null"/>.
    /// </summary>
    public UnhandledErrorBatch? Showing { get; private set; }

    /// <summary>
    /// Answers whether an exception is a cancellation that is no error: one while the application closes,
    /// or one whose own token was cancelled — a stop someone asked for. A cancellation nobody asked for,
    /// such as a timeout, is an error.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns><see langword="true"/> when it is not worth showing.</returns>
    public bool IsIgnoredCancellation(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OperationCanceledException cancelled => IsShuttingDown || cancelled.CancellationToken.IsCancellationRequested,
            AggregateException { InnerExceptions.Count: > 0 } aggregate => AllIgnored(aggregate),
            _ => false,
        };
    }

    /// <summary>
    /// Decides what an exception does.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The decision; on <see cref="UnhandledErrorDecision.Queue"/>, <see cref="Pending"/> holds it.</returns>
    public UnhandledErrorDecision Offer(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // The same exception can reach two hooks: an awaited fault rethrown on the dispatcher whose task the
        // finaliser also finds unobserved.
        if (_offered.TryGetValue(exception, out _))
        {
            return UnhandledErrorDecision.Ignore;
        }

        _offered.Add(exception, Seen);

        if (IsIgnoredCancellation(exception))
        {
            return UnhandledErrorDecision.Ignore;
        }

        if ((Showing ?? Pending) is { } open)
        {
            open.Add(exception);
            return UnhandledErrorDecision.Merge;
        }

        if (_closedAt.TryGetValue(SignatureOf(exception), out long closed) && _time.GetElapsedTime(closed) < RepeatWindow)
        {
            return UnhandledErrorDecision.Suppress;
        }

        Pending = new UnhandledErrorBatch(exception);
        return UnhandledErrorDecision.Queue;
    }

    /// <summary>
    /// Takes the pending batch on screen.
    /// </summary>
    /// <returns>The batch, or <see langword="null"/> when none was waiting.</returns>
    public UnhandledErrorBatch? BeginShow()
    {
        if (Pending is not { } batch)
        {
            return null;
        }

        Pending = null;
        Showing = batch;

        return batch;
    }

    /// <summary>
    /// The batch on screen was closed: each of its errors is not shown again for <see cref="RepeatWindow"/>.
    /// </summary>
    public void EndShow()
    {
        if (Showing is not { } batch)
        {
            return;
        }

        Showing = null;

        long now = _time.GetTimestamp();

        foreach (Exception error in batch.Errors)
        {
            _closedAt[SignatureOf(error)] = now;
        }
    }

    /// <summary>
    /// Says what went wrong in one line: the message of the exception that carries the meaning.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The sentence.</returns>
    public static string Summarise(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception meaningful = exception;

        // Wrappers say "one or more errors occurred" or "an invocation threw": the one inside says what.
        while (meaningful is AggregateException { InnerExceptions.Count: 1 } or TargetInvocationException { InnerException: not null })
        {
            meaningful = meaningful.InnerException!;
        }

        string message = meaningful.Message.Trim();

        return message.Length > 0 ? message : meaningful.GetType().Name;
    }

    /// <summary>
    /// Builds the details of an exception for a bug report: for it and every exception inside it, the
    /// type, the message and the stack trace — nothing else.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The text.</returns>
    /// <remarks>
    /// Never <see cref="Exception.Data"/>, never <see cref="Exception.ToString"/>, never a property of a
    /// derived type: what a library hangs there is not the reader's to copy into a bug report.
    /// </remarks>
    public static string BuildDetails(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        StringBuilder details = new();
        Append(details, exception, 0);

        return details.ToString().TrimEnd();
    }

    private bool AllIgnored(AggregateException aggregate)
    {
        foreach (Exception inner in aggregate.InnerExceptions)
        {
            if (!IsIgnoredCancellation(inner))
            {
                return false;
            }
        }

        return true;
    }

    private static string SignatureOf(Exception exception)
        => exception.GetType().FullName + "\n" + exception.Message + "\n" + exception.StackTrace;

    private static void Append(StringBuilder details, Exception exception, int depth)
    {
        details.Append(exception.GetType().FullName).Append(": ").AppendLine(exception.Message);

        if (exception.StackTrace is { Length: > 0 } stack)
        {
            details.AppendLine(stack);
        }

        if (depth >= MaximumDepth)
        {
            return;
        }

        if (exception is AggregateException aggregate)
        {
            for (int index = 0; index < aggregate.InnerExceptions.Count; index++)
            {
                details.Append("--- Inner exception ").Append(index + 1).Append(" of ")
                    .Append(aggregate.InnerExceptions.Count).AppendLine(" ---");
                Append(details, aggregate.InnerExceptions[index], depth + 1);
            }
        }
        else if (exception.InnerException is { } inner)
        {
            details.AppendLine("--- Inner exception ---");
            Append(details, inner, depth + 1);
        }
    }
}
