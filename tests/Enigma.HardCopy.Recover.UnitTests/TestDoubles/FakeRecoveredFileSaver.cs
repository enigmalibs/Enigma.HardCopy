using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>
/// A save picker that "chooses" the destination the test set up — or backs out, when there is none.
/// </summary>
internal sealed class FakeRecoveredFileSaver : IRecoveredFileSaver
{
    /// <summary>Gets or sets the destination the next pick returns; <see langword="null"/> backs out.</summary>
    internal FakeSaveTarget? Destination { get; set; } = new();

    /// <summary>Gets the names offered to the picker, one per pick.</summary>
    internal List<string> SuggestedNames { get; } = [];

    /// <inheritdoc/>
    public Task<ISaveTarget?> ChooseDestinationAsync(string suggestedFileName)
    {
        SuggestedNames.Add(suggestedFileName);

        return Task.FromResult<ISaveTarget?>(Destination);
    }
}
