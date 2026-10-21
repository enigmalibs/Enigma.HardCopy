using System.Threading.Tasks;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// Asks the user where the recovered file goes.
/// </summary>
public interface IRecoveredFileSaver
{
    /// <summary>
    /// Shows the system's save picker — on Android, the document picker that offers the phone's storage, Google
    /// Drive and every other installed document provider.
    /// </summary>
    /// <param name="suggestedFileName">The name the backup recorded, offered as the default.</param>
    /// <returns>The chosen destination, or <see langword="null"/> when the user backed out.</returns>
    Task<ISaveTarget?> ChooseDestinationAsync(string suggestedFileName);
}
