using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IUnhandledErrorReporter"/>: every exception logged in full, and shown in the error dialog.
/// </summary>
/// <remarks>
/// <para>
/// Enigma.GitClient's reporter, for this application's one window, as Enigma.TextEditor ported it. The log is
/// written at once, on whatever thread reported, because that thread may be about to end the process. What is
/// shown is decided on the UI thread, by <see cref="UnhandledErrorPolicy"/>.
/// </para>
/// <para>
/// The dialog is <see cref="IErrorDialog"/>, which waits for a question on screen to be answered and for the
/// window's host to exist. If the dialog itself fails, that is logged and no dialog is tried again in this
/// session: the net must not loop on itself.
/// </para>
/// </remarks>
internal sealed class UnhandledErrorReporter : IUnhandledErrorReporter
{
    private readonly IErrorDialog _dialog;
    private readonly ILogger<UnhandledErrorReporter> _logger;
    private readonly Action<Action> _post;
    private readonly UnhandledErrorPolicy _policy;

    private bool _presenting;
    private bool _broken;

    /// <summary>Initializes a new instance of the <see cref="UnhandledErrorReporter"/> class.</summary>
    /// <param name="dialog">Shows the errors.</param>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <param name="logger">Receives every exception, in full.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UnhandledErrorReporter(IErrorDialog dialog, TimeProvider time, ILogger<UnhandledErrorReporter> logger)
        : this(dialog, time, logger, static action => Dispatcher.UIThread.Post(action))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UnhandledErrorReporter"/> class.</summary>
    /// <param name="dialog">Shows the errors.</param>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <param name="logger">Receives every exception, in full.</param>
    /// <param name="post">
    /// Runs an action on the UI thread, later: the dispatcher in the application, the calling thread at once in a
    /// test. Internal, so the container only ever sees the public constructor.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    internal UnhandledErrorReporter(
        IErrorDialog dialog,
        TimeProvider time,
        ILogger<UnhandledErrorReporter> logger,
        Action<Action> post)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(post);

        _dialog = dialog;
        _logger = logger;
        _post = post;
        _policy = new UnhandledErrorPolicy(time);

        _dialog.Available += (_, _) => PresentAsync().Forget();
    }

    /// <inheritdoc/>
    public void Report(Exception exception, UnhandledErrorSource source)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_policy.IsIgnoredCancellation(exception))
        {
            _logger.LogDebug(exception, "A cancellation reached the safety net ({Source}); not an error.", source);
            return;
        }

        _logger.LogError(exception, "An unhandled exception reached the safety net ({Source}).", source);

        if (source == UnhandledErrorSource.AppDomain)
        {
            // .NET ends the process once the event returns: no dialog can come.
            return;
        }

        _post(() => Offer(exception));
    }

    /// <inheritdoc/>
    public void ShutdownStarted() => _policy.IsShuttingDown = true;

    private void Offer(Exception exception)
    {
        switch (_policy.Offer(exception))
        {
            case UnhandledErrorDecision.Queue:
                PresentAsync().Forget();
                break;
            case UnhandledErrorDecision.Suppress:
                _logger.LogDebug(
                    "The same error was shown less than {Window} ago; it is not shown again.",
                    UnhandledErrorPolicy.RepeatWindow);
                break;
            default:
                break;
        }
    }

    /// <summary>Shows the pending batch, once the dialog can be. Never throws: nothing awaits it.</summary>
    private async Task PresentAsync()
    {
        if (_broken || _presenting || _policy.Pending is null || !_dialog.IsAvailable())
        {
            // Not available: the dialog raises Available once it is, and this runs again.
            return;
        }

        if (_policy.BeginShow() is not { } batch)
        {
            return;
        }

        _presenting = true;

        try
        {
            await _dialog.ShowAsync(batch);
        }
        catch (Exception exception)
        {
            // Logged, never reported: a dialog that cannot be shown would otherwise report itself again and again.
            _broken = true;
            _logger.LogError(exception, "The safety net's dialog could not be shown; later errors are only logged.");
        }

        _presenting = false;
        _policy.EndShow();

        if (_policy.Pending is not null)
        {
            PresentAsync().Forget();
        }
    }
}
