using System;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.ViewModels;
using Xunit;

namespace Enigma.HardCopy.Desktop.UnitTests;

public sealed class ChunkSizeOptionTests
{
    [Fact]
    public void All_OffersTheThreePresets_InAscendingSize()
        => Assert.Collection(
            ChunkSizeOption.All,
            option => Assert.Equal(ChunkSizePreset.Small, option.Preset),
            option => Assert.Equal(ChunkSizePreset.Medium, option.Preset),
            option => Assert.Equal(ChunkSizePreset.Large, option.Preset));

    [Fact]
    public void Default_IsMedium() => Assert.Equal(ChunkSizePreset.Medium, ChunkSizeOption.Default.Preset);

    [Fact]
    public void Default_IsTheEntryFromAll() => Assert.Same(ChunkSizeOption.For(ChunkSizePreset.Medium), ChunkSizeOption.Default);

    [Theory]
    [InlineData(ChunkSizePreset.Small)]
    [InlineData(ChunkSizePreset.Medium)]
    [InlineData(ChunkSizePreset.Large)]
    public void For_ReturnsTheMatchingOption(ChunkSizePreset preset)
        => Assert.Equal(preset, ChunkSizeOption.For(preset).Preset);

    [Fact]
    public void For_RejectsAnUndefinedPreset()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ChunkSizeOption.For((ChunkSizePreset)999));

    [Fact]
    public void Labels_ComeFromTheResources()
    {
        Assert.Equal(Strings.BackupChunkSizeSmall, ChunkSizeOption.For(ChunkSizePreset.Small).Label);
        Assert.Equal(Strings.BackupChunkSizeMedium, ChunkSizeOption.For(ChunkSizePreset.Medium).Label);
        Assert.Equal(Strings.BackupChunkSizeLarge, ChunkSizeOption.For(ChunkSizePreset.Large).Label);
    }

    [Fact]
    public void ToString_IsTheLabel_SoAnUntemplatedSelectorStillReads()
        => Assert.Equal(ChunkSizeOption.Default.Label, ChunkSizeOption.Default.ToString());
}
