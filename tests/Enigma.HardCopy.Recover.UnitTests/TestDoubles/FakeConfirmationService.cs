using System.Threading.Tasks;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>Answers both questions the way the test said to, and remembers being asked.</summary>
internal sealed class FakeConfirmationService : IConfirmationService
{
    /// <summary>Gets or sets the answer to the unverified-save question.</summary>
    internal bool UnverifiedSaveAnswer { get; set; }

    /// <summary>Gets or sets the answer to the new-session question.</summary>
    internal bool NewSessionAnswer { get; set; }

    /// <summary>Gets how many times the unverified-save question was asked.</summary>
    internal int UnverifiedSaveAsked { get; private set; }

    /// <summary>Gets how many times the new-session question was asked.</summary>
    internal int NewSessionAsked { get; private set; }

    /// <summary>Gets the recorded hash the last unverified-save question named.</summary>
    internal string? LastExpectedSha256 { get; private set; }

    /// <summary>Gets the rebuilt hash the last unverified-save question named.</summary>
    internal string? LastActualSha256 { get; private set; }

    /// <inheritdoc/>
    public Task<bool> ConfirmUnverifiedSaveAsync(string expectedSha256, string actualSha256)
    {
        UnverifiedSaveAsked++;
        LastExpectedSha256 = expectedSha256;
        LastActualSha256 = actualSha256;

        return Task.FromResult(UnverifiedSaveAnswer);
    }

    /// <inheritdoc/>
    public Task<bool> ConfirmNewSessionAsync()
    {
        NewSessionAsked++;

        return Task.FromResult(NewSessionAnswer);
    }
}
