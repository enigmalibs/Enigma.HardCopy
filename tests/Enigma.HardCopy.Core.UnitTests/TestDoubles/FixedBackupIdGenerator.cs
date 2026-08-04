namespace Enigma.HardCopy.Core.UnitTests.TestDoubles;

/// <summary>An <see cref="IBackupIdGenerator"/> that always returns the same ID, so codes are assertable.</summary>
internal sealed class FixedBackupIdGenerator(string backupId) : IBackupIdGenerator
{
    public const string DefaultBackupId = "K7QA";

    public FixedBackupIdGenerator()
        : this(DefaultBackupId)
    {
    }

    public string NewBackupId() => backupId;
}
