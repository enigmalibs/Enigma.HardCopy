using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The two questions this application stops to ask before doing something it cannot take back.
/// </summary>
/// <remarks>
/// Both are deliberately narrow methods rather than one general "ask the user" call: the wording, the icon and
/// the button labels of a confirmation are part of the decision being guarded, not a parameter of it, and
/// keeping them here is what lets a ViewModel ask the question without naming a dialog.
/// </remarks>
public interface IConfirmationService
{
    /// <summary>
    /// Asks whether to write bytes whose SHA-256 does not match the one the backup recorded — the one
    /// irreversible thing this application will do against its own advice.
    /// </summary>
    /// <param name="expectedSha256">The hash recorded in the backup, lower-case hexadecimal.</param>
    /// <param name="actualSha256">The hash the rebuilt bytes actually have, lower-case hexadecimal.</param>
    /// <returns><see langword="true"/> only if the user explicitly agreed.</returns>
    Task<bool> ConfirmUnverifiedSaveAsync(string expectedSha256, string actualSha256);

    /// <summary>Asks whether to throw away a recovery that already holds codes.</summary>
    /// <returns><see langword="true"/> only if the user explicitly agreed.</returns>
    Task<bool> ConfirmStartOverAsync();
}
