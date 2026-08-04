using Enigma.HardCopy.Core;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Hands out one known backup ID, so a test can build a second, deliberately foreign backup and assert on the
/// ID the ViewModel reports.
/// </summary>
internal sealed class FixedBackupIdGenerator : IBackupIdGenerator
{
    private readonly string _backupId;

    internal FixedBackupIdGenerator(string backupId)
    {
        _backupId = backupId;
    }

    /// <inheritdoc/>
    public string NewBackupId() => _backupId;
}
