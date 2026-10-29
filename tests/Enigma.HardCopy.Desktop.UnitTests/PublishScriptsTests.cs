using System;
using System.IO;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Ties the three one-command publish scripts to the desktop project and to the command they promise: a Release,
/// ReadyToRun publish for one runtime identifier each.
/// </summary>
public sealed class PublishScriptsTests
{
    private static readonly string AssemblyName = typeof(App).Assembly.GetName().Name!;

    [Theory]
    [InlineData("packaging/windows/publish.ps1", "win-x64")]
    [InlineData("packaging/windows/publish.bat", "win-x64")]
    [InlineData("packaging/linux/publish.sh", "linux-x64")]
    public void EachScript_PublishesTheDesktopProject_InReleaseReadyToRun_ForItsRuntime(string script, string runtime)
    {
        string text = File.ReadAllText(RepositoryRoot.Combine(script)).Replace('\\', '/');

        Assert.Contains($"/src/{AssemblyName}", text, StringComparison.Ordinal);
        Assert.Contains($"-c Release -r {runtime} -p:PublishReadyToRun=true", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>cmd.exe</c> reads a batch file line by line and an LF-only one can misparse; <c>.gitattributes</c> keeps
    /// <c>*.bat</c> CRLF, and this catches a file that slipped past it.
    /// </summary>
    [Fact]
    public void TheBatchFile_HasWindowsLineEndings()
    {
        string text = File.ReadAllText(RepositoryRoot.Combine("packaging/windows/publish.bat"));

        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }
}
