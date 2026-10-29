using System;
using System.IO;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Finds files of the repository itself — the application's assets and its packaging scripts — from the test
/// output directory, for the tests that pin a file rather than a type.
/// </summary>
internal static class RepositoryRoot
{
    private const string SolutionFileName = "Enigma.HardCopy.slnx";

    /// <summary>Gets the directory holding the solution file.</summary>
    public static string Path { get; } = Find();

    /// <summary>Resolves a path relative to the repository root.</summary>
    /// <param name="relativePath">The path, with forward slashes, as it is written in the repository.</param>
    /// <returns>The full path.</returns>
    public static string Combine(string relativePath)
        => System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    private static string Find()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"No {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
