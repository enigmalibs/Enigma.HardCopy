using System;
using System.Text.RegularExpressions;
using Enigma.HardCopy.Desktop.Diagnostics;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// What the About dialog shows, and the two lines it leaves out rather than leaving blank.
/// </summary>
public sealed class AboutViewModelTests
{
    [Fact]
    public void Values_AreShownAsGiven()
    {
        AboutViewModel viewModel = new("1.3.0", "9a1b2c3", "Copyright © 2026 Someone");

        Assert.Equal(ProductInformation.DisplayName, viewModel.DisplayName);
        Assert.Equal("1.3.0", viewModel.Version);
        Assert.Equal("9a1b2c3", viewModel.BuildSha);
        Assert.True(viewModel.HasBuildSha);
        Assert.Equal("Copyright © 2026 Someone", viewModel.Copyright);
        Assert.True(viewModel.HasCopyright);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ABuildWithNoRevision_ShowsNoBuildLine(string? buildSha)
        => Assert.False(new AboutViewModel("1.3.0", buildSha, "©").HasBuildSha);

    [Fact]
    public void AnAssemblyWithNoCopyright_ShowsNoCopyrightLine()
        => Assert.False(new AboutViewModel("1.3.0", null, string.Empty).HasCopyright);

    [Fact]
    public void TheRunningBuild_StatesTheSolutionVersionAndItsCopyright()
    {
        AboutViewModel viewModel = new();

        // Directory.Build.props stamps both on every assembly of the solution; a plain X.Y.Z, whatever it is.
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), viewModel.Version);
        Assert.StartsWith("Copyright ©", viewModel.Copyright, StringComparison.Ordinal);
        Assert.Equal("Enigma HardCopy", viewModel.DisplayName);
    }

    [Fact]
    public void Constructor_RejectsMissingValues()
    {
        Assert.Throws<ArgumentNullException>(() => new AboutViewModel(null!, null, string.Empty));
        Assert.Throws<ArgumentNullException>(() => new AboutViewModel("1.3.0", null, null!));
    }
}
