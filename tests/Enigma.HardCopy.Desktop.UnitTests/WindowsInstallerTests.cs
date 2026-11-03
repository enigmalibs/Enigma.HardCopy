using System;
using System.IO;
using Enigma.HardCopy.Desktop.Diagnostics;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Ties the Windows installer build (<c>packaging/windows/buildmsi.ps1</c> and its <c>buildmsi.bat</c> twin) to the
/// product it packages: the upgrade code every MSI of the product line carries, the product's name, the assembly its
/// shortcuts start, and the publish it packages.
/// </summary>
/// <remarks>
/// The script builds only on Windows with the WiX CLI, and nothing in the solution's build reads it: a renamed
/// assembly or a changed constant would otherwise surface as an MSI whose shortcuts start nothing, or one that
/// installs beside the previous version instead of replacing it.
/// </remarks>
public sealed class WindowsInstallerTests
{
    /// <summary>
    /// The product line's identity in Windows Installer, generated once for the first MSI (FEATURE-4ED9): an MSI
    /// that carries the installed one's code replaces it, one that does not installs beside it. Never change it.
    /// </summary>
    private const string UpgradeCode = "4b3ca1c8-32bb-4c88-83fb-24c08dbc4f61";

    private static readonly string AssemblyName = typeof(App).Assembly.GetName().Name!;

    private static string[] Script { get; } = File.ReadAllLines(RepositoryRoot.Combine("packaging/windows/buildmsi.ps1"));

    private static string BatchFile { get; } = File.ReadAllText(RepositoryRoot.Combine("packaging/windows/buildmsi.bat"));

    [Fact]
    public void TheInstaller_KeepsTheProductLinesUpgradeCode()
        => Assert.Contains($"$UpgradeCode = '{UpgradeCode}'", Script);

    [Fact]
    public void TheInstaller_NamesTheProduct_AsTheAboutDialogDoes()
        => Assert.Contains($"$ProductName = '{ProductInformation.DisplayName}'", Script);

    [Fact]
    public void TheInstaller_PackagesAndStartsTheExecutableOfTheAssembly()
        => Assert.Contains($"$Assembly = '{AssemblyName}'", Script);

    [Fact]
    public void TheInstaller_PackagesWhatThePublishScriptPublishes()
        => Assert.Contains("& (Join-Path $PSScriptRoot 'publish.ps1')", Script);

    [Fact]
    public void TheVendoredWorker_SitsBesideTheScript()
    {
        Assert.Contains("$worker = Join-Path $PSScriptRoot 'Enigma.Msi.Worker\\Enigma.Msi.Worker.exe'", Script);
        Assert.True(File.Exists(RepositoryRoot.Combine("packaging/windows/Enigma.Msi.Worker/Enigma.Msi.Worker.exe")));
    }

    [Fact]
    public void TheBatchFile_RunsTheScriptBesideIt_PastTheExecutionPolicy()
        => Assert.Contains("powershell -NoProfile -ExecutionPolicy Bypass -File \"%~dp0buildmsi.ps1\"", BatchFile, StringComparison.Ordinal);

    /// <summary>
    /// <c>cmd.exe</c> reads a batch file line by line and an LF-only one can misparse; <c>.gitattributes</c> keeps
    /// <c>*.bat</c> CRLF, and this catches a file that slipped past it.
    /// </summary>
    [Fact]
    public void TheBatchFile_HasWindowsLineEndings()
        => Assert.DoesNotContain("\n", BatchFile.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
}
