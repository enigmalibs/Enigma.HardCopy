using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for the modal progress scrim, recording what was raised and — the point of the whole double —
/// whether it was ever left up.
/// </summary>
internal sealed class FakeProgressOverlay : IProgressOverlay
{
    private readonly List<string> _titles = [];
    private readonly List<string?> _stages = [];

    /// <summary>Gets the title of every card raised, in order.</summary>
    internal IReadOnlyList<string> Titles => _titles;

    /// <summary>Gets every stage the running card was told about, in order.</summary>
    internal IReadOnlyList<string?> Stages => _stages;

    /// <summary>Gets how many times the scrim was raised.</summary>
    internal int Shows { get; private set; }

    /// <summary>Gets how many times the scrim was taken down.</summary>
    internal int Hides { get; private set; }

    /// <summary>Gets a value indicating whether a card is on screen right now.</summary>
    internal bool IsShowing { get; private set; }

    /// <summary>Gets the cancel command the last card was raised with, if any.</summary>
    internal ICommand? LastCancelCommand { get; private set; }

    /// <summary>Gets a value indicating whether the last card offered a way to cancel.</summary>
    internal bool LastCardOfferedCancel { get; private set; }

    /// <inheritdoc/>
    public Task ShowAsync(string title, ICommand? cancelCommand = null)
    {
        Shows++;
        IsShowing = true;
        LastCancelCommand = cancelCommand;
        LastCardOfferedCancel = cancelCommand is not null;
        _titles.Add(title);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Update(string? message, bool isIndeterminate = true, double percent = 0d)
    {
        // Mirrors the real implementation: an update that arrives with no card up is dropped, not recorded.
        if (IsShowing)
        {
            _stages.Add(message);
        }
    }

    /// <inheritdoc/>
    public Task HideAsync()
    {
        Hides++;
        IsShowing = false;

        return Task.CompletedTask;
    }
}
