using System;
using System.Collections.Generic;
using Enigma.HardCopy.Recover.Services;
using Enigma.HardCopy.Recover.ViewModels;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>Keeps every banner instead of showing it.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    /// <summary>Gets every message published, oldest first.</summary>
    internal List<StatusMessage> Published { get; } = [];

    /// <inheritdoc/>
    public void Publish(StatusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Published.Add(message);
    }
}
