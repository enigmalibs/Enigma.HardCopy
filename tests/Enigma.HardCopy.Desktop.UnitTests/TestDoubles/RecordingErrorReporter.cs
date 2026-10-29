using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// An <see cref="IUnhandledErrorReporter"/> that records what reaches it, from any thread.
/// </summary>
internal sealed class RecordingErrorReporter : IUnhandledErrorReporter
{
    private readonly ConcurrentQueue<(Exception Exception, UnhandledErrorSource Source)> _reports = new();

    /// <summary>
    /// Gets what was reported, in order.
    /// </summary>
    public IReadOnlyList<(Exception Exception, UnhandledErrorSource Source)> Reports => [.. _reports];

    /// <summary>
    /// Gets a value indicating whether <see cref="ShutdownStarted"/> was called.
    /// </summary>
    public bool IsShuttingDown { get; private set; }

    /// <summary>
    /// Answers whether an exception was reported, and from where.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The source it was reported with, or <see langword="null"/> if it never was.</returns>
    public UnhandledErrorSource? SourceOf(Exception exception)
        => _reports.Where(report => ReferenceEquals(report.Exception, exception))
            .Select(report => (UnhandledErrorSource?)report.Source)
            .FirstOrDefault();

    /// <inheritdoc />
    public void Report(Exception exception, UnhandledErrorSource source) => _reports.Enqueue((exception, source));

    /// <inheritdoc />
    public void ShutdownStarted() => IsShuttingDown = true;
}
