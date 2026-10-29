using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// Ties the Linux installer to the application it installs: both scripts name the desktop assembly and the same
/// icon sizes, every listed size has its PNG at that size, and the launcher entry starts and groups that assembly.
/// </summary>
/// <remarks>
/// A rename of the assembly or a change to the icon set would otherwise leave an installer that builds, runs and
/// installs a launcher that does nothing — none of which a Windows build would notice.
/// </remarks>
public sealed class LinuxPackagingTests
{
    private static readonly string AssemblyName = typeof(App).Assembly.GetName().Name!;

    private static string InstallScript { get; } = File.ReadAllText(RepositoryRoot.Combine("packaging/linux/install.sh"));

    private static string UninstallScript { get; } = File.ReadAllText(RepositoryRoot.Combine("packaging/linux/uninstall.sh"));

    private static string DesktopEntry { get; } = File.ReadAllText(RepositoryRoot.Combine("packaging/linux/enigma-hardcopy.desktop.in"));

    [Fact]
    public void BothScripts_NameTheDesktopAssembly_AsTheLauncher()
    {
        Assert.Equal(AssemblyName, ReadonlyValue(InstallScript, "APP_EXE"));
        Assert.Equal(AssemblyName, ReadonlyValue(UninstallScript, "APP_EXE"));
    }

    [Fact]
    public void Install_BuildsTheDesktopProject()
        => Assert.Contains($"dotnet publish \"${{repo_root}}/src/{AssemblyName}\"", InstallScript, StringComparison.Ordinal);

    [Fact]
    public void BothScripts_ShareOneApplicationId_AndOneIconSet()
    {
        Assert.Equal("enigma-hardcopy", ReadonlyValue(InstallScript, "APP_ID"));
        Assert.Equal("enigma-hardcopy", ReadonlyValue(UninstallScript, "APP_ID"));
        Assert.Equal(IconSizes(InstallScript), IconSizes(UninstallScript));
    }

    [Fact]
    public void EveryListedIconSize_HasItsPng_AtThatSize()
    {
        foreach (int size in IconSizes(InstallScript))
        {
            byte[] png = File.ReadAllBytes(RepositoryRoot.Combine($"packaging/linux/icons/enigma-hardcopy-{size}.png"));

            // IHDR is always the first chunk: width and height follow the 8-byte signature and the chunk header.
            Assert.Equal((uint)size, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16)));
            Assert.Equal((uint)size, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20)));
        }
    }

    [Fact]
    public void TheIconsFolder_HoldsOnlyTheListedSizes()
    {
        string[] files = [.. Directory.GetFiles(RepositoryRoot.Combine("packaging/linux/icons")).Select(Path.GetFileName).Order()!];

        Assert.Equal(IconSizes(InstallScript).Select(size => $"enigma-hardcopy-{size}.png").Order(), files);
    }

    [Fact]
    public void TheLauncherEntry_StartsThroughTheInstaller_AndGroupsTheRunningWindow()
    {
        Assert.Contains("Exec=@EXEC@", DesktopEntry, StringComparison.Ordinal);
        Assert.Contains("Icon=enigma-hardcopy", DesktopEntry, StringComparison.Ordinal);

        // Avalonia's X11 backend takes the window class from the entry assembly's name.
        Assert.Contains($"StartupWMClass={AssemblyName}", DesktopEntry, StringComparison.Ordinal);
    }

    private static string ReadonlyValue(string script, string name)
        => Regex.Match(script, $"^readonly {name}='([^']+)'$", RegexOptions.Multiline) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"No readonly {name} in the script.");

    private static int[] IconSizes(string script)
        => Regex.Match(script, @"^readonly ICON_SIZES=\(([0-9 ]+)\)$", RegexOptions.Multiline) is { Success: true } match
            ? [.. match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)]
            : throw new InvalidOperationException("No readonly ICON_SIZES in the script.");
}
