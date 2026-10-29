namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Where an exception reached the safety net from.
/// </summary>
public enum UnhandledErrorSource
{
    /// <summary>A command, an event handler or a posted callback threw on the UI thread.</summary>
    Dispatcher,

    /// <summary>A task faulted and nothing ever looked at it.</summary>
    UnobservedTask,

    /// <summary>An exception escaped on a thread with nothing above it; the process ends.</summary>
    AppDomain,

    /// <summary>Work started and not awaited failed.</summary>
    FireAndForget,

    /// <summary>The start of the application failed in a way it does not handle itself.</summary>
    Startup,
}
