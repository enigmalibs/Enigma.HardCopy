using System.Threading.Tasks;
using System.Windows.Input;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The modal scrim a long operation runs behind: raised once when the operation starts, told which stage it
/// has reached as it goes, and taken down when it ends however it ends.
/// </summary>
/// <remarks>
/// <para>
/// This is the application's own seam over the control library's overlay service, so that a ViewModel can put
/// a run on screen without naming an Avalonia type — the same reason
/// <see cref="IFileDialogService"/> exists. The only framework type in the contract is
/// <see cref="ICommand"/>, which the ViewModels already deal in.
/// </para>
/// <para>
/// An implementation must never let an exception leave the scrim up, and must treat an
/// <see cref="Update"/> arriving after the card is gone as a no-op rather than a failure.
/// </para>
/// </remarks>
public interface IProgressOverlay
{
    /// <summary>Raises the scrim with a card describing the operation about to run.</summary>
    /// <param name="title">What is being done — one line, already localized.</param>
    /// <param name="cancelCommand">
    /// The command that abandons the operation, or <see langword="null"/> when it cannot be abandoned, in
    /// which case the card shows no cancel button.
    /// </param>
    /// <returns>A task that completes once the card is on screen.</returns>
    Task ShowAsync(string title, ICommand? cancelCommand = null);

    /// <summary>Reports the stage the running operation has reached. A no-op when no card is showing.</summary>
    /// <param name="message">The stage, already localized, or <see langword="null"/> to show none.</param>
    /// <param name="isIndeterminate">
    /// Whether the progress bar should sweep rather than fill. It should, unless the caller has a real
    /// fraction to report.
    /// </param>
    /// <param name="percent">How far the operation has got, from 0 to 100. Ignored while indeterminate.</param>
    void Update(string? message, bool isIndeterminate = true, double percent = 0d);

    /// <summary>Takes the scrim down. Safe to call when none is up.</summary>
    /// <returns>A task that completes once the card is gone.</returns>
    Task HideAsync();
}
