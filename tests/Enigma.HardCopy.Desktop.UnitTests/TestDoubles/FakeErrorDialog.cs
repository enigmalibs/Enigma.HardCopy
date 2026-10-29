using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.UnitTests.TestDoubles;

/// <summary>
/// Stands in for the error dialog. A shown dialog stays open until the test closes it, and the host can be made
/// unavailable — a question on screen, or no window yet — and available again.
/// </summary>
internal sealed class FakeErrorDialog : IErrorDialog
{
    private readonly List<UnhandledErrorBatch> _shown = [];

    private TaskCompletionSource? _open;

    /// <inheritdoc/>
    public event EventHandler? Available;

    /// <summary>Gets every batch shown, in order.</summary>
    internal IReadOnlyList<UnhandledErrorBatch> Shown => _shown;

    /// <summary>Gets or sets a value indicating whether the host could take a dialog, apart from one already open.</summary>
    internal bool Ready { get; set; } = true;

    /// <summary>Gets or sets what showing the dialog throws, or <see langword="null"/> to show it.</summary>
    internal Exception? FailWith { get; set; }

    /// <inheritdoc/>
    public bool IsAvailable() => Ready && _open is null;

    /// <inheritdoc/>
    public Task ShowAsync(UnhandledErrorBatch batch)
    {
        if (FailWith is { } failure)
        {
            return Task.FromException(failure);
        }

        _shown.Add(batch);
        _open = new TaskCompletionSource();

        return _open.Task;
    }

    /// <summary>Closes the dialog on screen, as the reader would.</summary>
    internal void Close()
    {
        TaskCompletionSource? open = _open;
        _open = null;
        open?.SetResult();
    }

    /// <summary>Makes the host available and says so, as the window handing its host over does.</summary>
    internal void BecomeAvailable()
    {
        Ready = true;
        Available?.Invoke(this, EventArgs.Empty);
    }
}
