using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

/// <summary>
/// What the safety net's dialog says, and that it follows errors counted into its batch while it is open.
/// </summary>
public sealed class UnhandledErrorViewModelTests
{
    [Fact]
    public void TheDialog_SaysTheAppIsRunning_AndWhatWentWrong()
    {
        UnhandledErrorViewModel viewModel = new(new UnhandledErrorBatch(new InvalidOperationException("the PDF could not be composed")), NoCopy);

        Assert.Equal(Strings.UnhandledErrorLead, viewModel.Lead);
        Assert.Equal("the PDF could not be composed", viewModel.Message);
        Assert.False(viewModel.HasMore);
        Assert.Contains("System.InvalidOperationException: the PDF could not be composed", viewModel.Details, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreErrors_AreCounted_InTheSingularThenThePlural()
    {
        UnhandledErrorBatch batch = new(new InvalidOperationException("first"));
        UnhandledErrorViewModel viewModel = new(batch, NoCopy);

        batch.Add(new InvalidOperationException("second"));
        Assert.True(viewModel.HasMore);
        Assert.Equal("1 more error occurred.", viewModel.MoreText);

        batch.Add(new InvalidOperationException("third"));
        Assert.Equal("2 more errors occurred.", viewModel.MoreText);
    }

    [Fact]
    public void AnErrorCountedIn_RefreshesTheCount_AndTheDetails()
    {
        UnhandledErrorBatch batch = new(new InvalidOperationException("first"));
        UnhandledErrorViewModel viewModel = new(batch, NoCopy);
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        batch.Add(new InvalidOperationException("second"));

        Assert.Equal([nameof(viewModel.HasMore), nameof(viewModel.MoreText), nameof(viewModel.Details)], changed);
    }

    [Fact]
    public async Task CopyDetails_HandsTheDetailsToTheClipboard()
    {
        UnhandledErrorBatch batch = new(new InvalidOperationException("failed"));
        string? copied = null;
        UnhandledErrorViewModel viewModel = new(batch, text =>
        {
            copied = text;
            return Task.CompletedTask;
        });

        await viewModel.CopyDetailsCommand.ExecuteAsync(null);

        Assert.Equal(batch.Details, copied);
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new UnhandledErrorViewModel(null!, NoCopy));
        Assert.Throws<ArgumentNullException>(() => new UnhandledErrorViewModel(new UnhandledErrorBatch(new InvalidOperationException()), null!));
    }

    private static Task NoCopy(string text) => Task.CompletedTask;
}
