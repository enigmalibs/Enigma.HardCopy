using System;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The safety net: every exception nothing else caught ends up here, is logged, and is shown in a dialog
/// that says the application is still running.
/// </summary>
/// <remarks>
/// A safety net, not the error handling: a failure the application knows about — a file it cannot read
/// or write — is said where it happens, in that place's own words, and never reaches this.
/// </remarks>
public interface IUnhandledErrorReporter
{
    /// <summary>
    /// Logs an exception and shows it, from any thread.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="source">Where it was caught.</param>
    void Report(Exception exception, UnhandledErrorSource source);

    /// <summary>
    /// Says the application is closing: from here on a cancellation is never worth a word.
    /// </summary>
    void ShutdownStarted();
}
