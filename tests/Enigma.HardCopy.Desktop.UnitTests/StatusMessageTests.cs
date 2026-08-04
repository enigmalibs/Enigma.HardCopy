using System;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class StatusMessageTests
{
    [Fact]
    public void Information_IsNeitherSuccessNorProblem()
    {
        StatusMessage message = StatusMessage.Information("reading");

        Assert.Equal(MessageSeverity.Information, message.Severity);
        Assert.False(message.IsSuccess);
        Assert.False(message.IsWarning);
        Assert.False(message.IsError);
    }

    [Fact]
    public void Success_SetsOnlyItsOwnFlag()
    {
        StatusMessage message = StatusMessage.Success("done");

        Assert.True(message.IsSuccess);
        Assert.False(message.IsWarning);
        Assert.False(message.IsError);
    }

    [Fact]
    public void Warning_SetsOnlyItsOwnFlag()
    {
        StatusMessage message = StatusMessage.Warning("unverified");

        Assert.False(message.IsSuccess);
        Assert.True(message.IsWarning);
        Assert.False(message.IsError);
    }

    [Fact]
    public void Error_SetsOnlyItsOwnFlag()
    {
        StatusMessage message = StatusMessage.Error("failed");

        Assert.False(message.IsSuccess);
        Assert.False(message.IsWarning);
        Assert.True(message.IsError);
    }

    [Fact]
    public void Text_IsKeptVerbatim() => Assert.Equal("Saved keys.kdbx.", StatusMessage.Success("Saved keys.kdbx.").Text);

    /// <remarks>
    /// <see cref="Assert.ThrowsAny{T}(Func{object?})"/> rather than an exact match: <see langword="null"/> is
    /// rejected as an <see cref="ArgumentNullException"/> and white space as an <see cref="ArgumentException"/>,
    /// and which of the two arrives is the guard's business, not this test's.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankText_IsRejected(string? text)
        => Assert.ThrowsAny<ArgumentException>(() => new StatusMessage(text!, MessageSeverity.Information));
}
