using System.Collections.Generic;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.ViewModels;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for the window's info bar, keeping every outcome that was published so a test can assert both
/// how many there were and what they said.
/// </summary>
internal sealed class FakeNotificationService : INotificationService
{
    private readonly List<StatusMessage> _published = [];

    /// <summary>Gets every outcome published, in order.</summary>
    internal IReadOnlyList<StatusMessage> Published => _published;

    /// <summary>Gets the outcome published last, or <see langword="null"/> when there was none.</summary>
    internal StatusMessage? Last => _published.Count == 0 ? null : _published[^1];

    /// <inheritdoc/>
    public void Publish(StatusMessage message) => _published.Add(message);
}
