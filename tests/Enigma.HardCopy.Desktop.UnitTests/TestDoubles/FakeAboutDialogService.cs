using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>Stands in for the About dialog, counting how many times it was asked for.</summary>
internal sealed class FakeAboutDialogService : IAboutDialogService
{
    /// <summary>Gets how many times the dialog was shown.</summary>
    internal int ShowCalls { get; private set; }

    /// <inheritdoc/>
    public Task ShowAsync()
    {
        ShowCalls++;

        return Task.CompletedTask;
    }
}
