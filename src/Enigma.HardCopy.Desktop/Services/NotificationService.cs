using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.InfoBar;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="INotificationService"/> backed by the control library's info bar.
/// </summary>
/// <remarks>
/// <para>
/// The library's <c>ShowAsync</c> completes when the banner is <b>dismissed</b>, not when it appears, so
/// awaiting it would tie the caller's operation to how long the user takes to read. It is therefore started
/// and not awaited — and, because nothing observes the resulting task, every failure it can produce is caught
/// inside it.
/// </para>
/// <para>
/// The severity mapping is one-to-one with <see cref="MessageSeverity"/>; the title is the one thing this
/// layer adds, since the info bar wants a heading and a page's outcome text is a sentence rather than a label.
/// </para>
/// </remarks>
internal sealed class NotificationService : INotificationService
{
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>Initializes a new instance of the <see cref="NotificationService"/> class.</summary>
    /// <param name="infoBar">The library's info bar service, whose host the shell registered at startup.</param>
    /// <param name="logger">Records a banner that could not be shown; the user is not told twice.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public NotificationService(IInfoBarService infoBar, ILogger<NotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(infoBar);
        ArgumentNullException.ThrowIfNull(logger);

        _infoBar = infoBar;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Publish(StatusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        (string title, InfoBarSeverity severity) = message.Severity switch
        {
            MessageSeverity.Success => (Strings.NotificationSuccessTitle, InfoBarSeverity.Success),
            MessageSeverity.Warning => (Strings.NotificationWarningTitle, InfoBarSeverity.Warning),
            MessageSeverity.Error => (Strings.NotificationErrorTitle, InfoBarSeverity.Error),
            _ => (Strings.NotificationInformationTitle, InfoBarSeverity.Info),
        };

        _ = ShowAsync(title, message.Text, severity);
    }

    private async Task ShowAsync(string title, string text, InfoBarSeverity severity)
    {
        try
        {
            await _infoBar.ShowAsync(bar =>
            {
                bar.Title = title;
                bar.Message = text;
                bar.Severity = severity;
            });
        }
        catch (InvalidOperationException ex)
        {
            // The host was never handed over. Nothing is awaiting this task, so the failure has to end here.
            _logger.LogError(ex, "The notification could not be shown.");
        }
    }
}
