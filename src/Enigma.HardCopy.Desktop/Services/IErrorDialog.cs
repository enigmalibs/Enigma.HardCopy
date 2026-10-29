using System;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The safety net's dialog, as <see cref="IUnhandledErrorReporter"/> sees it: whether it can be shown now, and
/// showing it.
/// </summary>
/// <remarks>
/// The seam between what the net decides and the window it shows on, so the deciding is tested without a
/// windowing platform. Every member is used on the UI thread.
/// </remarks>
public interface IErrorDialog
{
    /// <summary>
    /// Raised on the UI thread once a dialog that could not be shown can be: the window handed its dialog host
    /// over, or the question on it was answered.
    /// </summary>
    event EventHandler? Available;

    /// <summary>
    /// Answers whether the dialog can be shown now. When it cannot — no host yet, or a question on it — it
    /// raises <see cref="Available"/> once it can.
    /// </summary>
    /// <returns><see langword="true"/> when <see cref="ShowAsync"/> may be called.</returns>
    bool IsAvailable();

    /// <summary>Shows a batch of errors and waits for the dialog to be closed.</summary>
    /// <param name="batch">The errors; the dialog follows errors counted into it while it is open.</param>
    /// <returns>A task that completes when the dialog is closed.</returns>
    Task ShowAsync(UnhandledErrorBatch batch);
}
