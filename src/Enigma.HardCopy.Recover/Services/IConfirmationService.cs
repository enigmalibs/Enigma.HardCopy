using System.Threading.Tasks;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The two questions this application stops to ask before doing something it cannot take back — the same two the
/// desktop app asks.
/// </summary>
public interface IConfirmationService
{
    /// <summary>
    /// Asks whether to write bytes whose SHA-256 does not match the one the backup recorded.
    /// </summary>
    /// <param name="expectedSha256">The hash recorded in the backup, lower-case hexadecimal.</param>
    /// <param name="actualSha256">The hash the rebuilt bytes actually have, lower-case hexadecimal.</param>
    /// <returns><see langword="true"/> only if the user explicitly agreed.</returns>
    Task<bool> ConfirmUnverifiedSaveAsync(string expectedSha256, string actualSha256);

    /// <summary>Asks whether to throw away a session that already holds codes.</summary>
    /// <returns><see langword="true"/> only if the user explicitly agreed.</returns>
    Task<bool> ConfirmNewSessionAsync();
}
