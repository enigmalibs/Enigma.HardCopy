using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests.Services;

/// <summary>
/// The tests that attach a reporter to <see cref="Desktop.Services.UnhandledExceptionHooks"/>, whose state is
/// the process's. They run one at a time and alone, so no other test's report lands among the ones counted.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessHooksCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Process-wide unhandled-exception hooks";
}
