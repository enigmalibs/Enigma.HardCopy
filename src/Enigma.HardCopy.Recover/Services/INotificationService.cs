using Enigma.HardCopy.Recover.ViewModels;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// Where an outcome goes to be seen: one banner across the top of the screen, at the severity the outcome carries.
/// </summary>
/// <remarks>
/// Used for what the user must not miss — a code from another backup, a damaged code, a file saved or not. The
/// steady drip of accepted codes does <b>not</b> come here: it is shown on the page and felt in the hand, because a
/// banner per code would bury the one banner that matters.
/// </remarks>
public interface INotificationService
{
    /// <summary>Shows one outcome, replacing whatever banner was up.</summary>
    /// <param name="message">The outcome, already localized and formatted.</param>
    /// <remarks>
    /// Returns as soon as the banner has been asked for; it does not wait for the banner to be dismissed.
    /// </remarks>
    void Publish(StatusMessage message);
}
