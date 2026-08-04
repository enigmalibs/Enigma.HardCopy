using System;
using System.Collections.Generic;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Desktop.Resources;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// One entry of the barcode-density selector: a <see cref="ChunkSizePreset"/> and the sentence describing
/// what choosing it costs and buys.
/// </summary>
/// <remarks>
/// A wrapper rather than binding the enum directly, because the enum's names are not what a user needs to
/// read: the trade-off between pages to print and modules to scan is the actual decision, and it belongs in
/// the label.
/// </remarks>
public sealed class ChunkSizeOption
{
    private ChunkSizeOption(ChunkSizePreset preset, string label)
    {
        Preset = preset;
        Label = label;
    }

    /// <summary>Gets every offered preset, in increasing chunk size.</summary>
    public static IReadOnlyList<ChunkSizeOption> All { get; } =
    [
        new(ChunkSizePreset.Small, Strings.BackupChunkSizeSmall),
        new(ChunkSizePreset.Medium, Strings.BackupChunkSizeMedium),
        new(ChunkSizePreset.Large, Strings.BackupChunkSizeLarge),
    ];

    /// <summary>Gets the option the backup view starts on.</summary>
    public static ChunkSizeOption Default { get; } = For(ChunkSizePreset.Medium);

    /// <summary>Gets the preset this option selects.</summary>
    public ChunkSizePreset Preset { get; }

    /// <summary>Gets the sentence shown in the selector.</summary>
    public string Label { get; }

    /// <summary>Returns the option for <paramref name="preset"/>.</summary>
    /// <param name="preset">The preset to look up.</param>
    /// <returns>The matching option.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="preset"/> is not an offered preset.</exception>
    public static ChunkSizeOption For(ChunkSizePreset preset)
    {
        foreach (ChunkSizeOption option in All)
        {
            if (option.Preset == preset)
            {
                return option;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(preset));
    }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
