using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for the two confirmation dialogs. Each answer is set before the act; both default to
/// <see langword="true"/>, so a test that is not about the guard reads as though the user agreed.
/// </summary>
internal sealed class FakeConfirmationService : IConfirmationService
{
    private readonly List<(string Expected, string Actual)> _unverifiedSaveAsks = [];

    /// <summary>Gets the hash pair each unverified-save question was asked with, in order.</summary>
    internal IReadOnlyList<(string Expected, string Actual)> UnverifiedSaveAsks => _unverifiedSaveAsks;

    /// <summary>Gets or sets the answer the unverified-save question gets.</summary>
    internal bool AgreeToUnverifiedSave { get; set; } = true;

    /// <summary>Gets or sets the answer the start-over question gets.</summary>
    internal bool AgreeToStartOver { get; set; } = true;

    /// <summary>Gets how many times the unverified-save question was asked.</summary>
    internal int UnverifiedSaveCalls => _unverifiedSaveAsks.Count;

    /// <summary>Gets how many times the start-over question was asked.</summary>
    internal int StartOverCalls { get; private set; }

    /// <inheritdoc/>
    public Task<bool> ConfirmUnverifiedSaveAsync(string expectedSha256, string actualSha256)
    {
        _unverifiedSaveAsks.Add((expectedSha256, actualSha256));

        return Task.FromResult(AgreeToUnverifiedSave);
    }

    /// <inheritdoc/>
    public Task<bool> ConfirmStartOverAsync()
    {
        StartOverCalls++;

        return Task.FromResult(AgreeToStartOver);
    }
}
