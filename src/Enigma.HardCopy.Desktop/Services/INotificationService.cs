using Enigma.HardCopy.Desktop.ViewModels;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Where a page's outcome goes to be seen: one banner over the whole window, at the severity the outcome
/// carries.
/// </summary>
/// <remarks>
/// This replaces the inline outcome panels the two pages used to draw for themselves. It deliberately does
/// <b>not</b> replace anything else they show — the recovery activity log, the missing-index list and the two
/// inline warnings all stay on the page, because a one-line bar cannot carry a three-hundred-entry log.
/// </remarks>
public interface INotificationService
{
    /// <summary>Shows one outcome, replacing whatever banner was up.</summary>
    /// <param name="message">The outcome, already localized and formatted.</param>
    /// <remarks>
    /// Returns as soon as the banner has been asked for. It deliberately does not wait for the banner to be
    /// dismissed: the underlying control completes only on dismissal, and the operation that produced the
    /// outcome is over.
    /// </remarks>
    void Publish(StatusMessage message);
}
