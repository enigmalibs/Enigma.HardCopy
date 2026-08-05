using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Enigma.Avalonia.Desktop.Services;
using Enigma.HardCopy.Desktop.Views;
using Microsoft.Extensions.Logging;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The <see cref="IProgressOverlay"/> backed by the control library's overlay service and this application's
/// own <see cref="ProgressOverlayCard"/>.
/// </summary>
/// <remarks>
/// <para>
/// Showing and hiding are guarded <b>here</b> rather than at every call site: a stuck scrim leaves the whole
/// window unusable, so neither a host that was never registered nor a failure inside the library may be able
/// to produce one. <see cref="HideAsync"/> forgets the card in a <c>finally</c> and reports nothing to the
/// user; the operation behind it has its own outcome message either way.
/// </para>
/// <para>
/// The card is created per run and dropped on hide. It is an Avalonia control, so every method here belongs to
/// the UI thread — which is where the ViewModels' command handlers already run.
/// </para>
/// </remarks>
internal sealed class ProgressOverlay : IProgressOverlay
{
    private readonly IOverlayService _overlay;
    private readonly ILogger<ProgressOverlay> _logger;

    private ProgressOverlayCard? _card;

    /// <summary>Initializes a new instance of the <see cref="ProgressOverlay"/> class.</summary>
    /// <param name="overlay">The library's overlay service, whose host the shell registered at startup.</param>
    /// <param name="logger">Records a scrim that could not be raised; the user is not told.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ProgressOverlay(IOverlayService overlay, ILogger<ProgressOverlay> logger)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(logger);

        _overlay = overlay;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ShowAsync(string title, ICommand? cancelCommand = null)
    {
        ProgressOverlayCard card = new()
        {
            Title = title,
            CancelCommand = cancelCommand,
            IsIndeterminate = true,
        };

        _card = card;
        try
        {
            await _overlay.ShowAsync(card);
        }
        catch (InvalidOperationException ex)
        {
            // The host was never handed over — a startup wiring mistake, not something the user did. The
            // operation still runs; it just runs without a scrim.
            _card = null;
            _logger.LogError(ex, "The progress overlay could not be shown.");
        }
    }

    /// <inheritdoc/>
    public void Update(string? message, bool isIndeterminate = true, double percent = 0d)
    {
        if (_card is not ProgressOverlayCard card)
        {
            return;
        }

        card.Message = message;
        card.IsIndeterminate = isIndeterminate;
        card.Progress = percent;
    }

    /// <inheritdoc/>
    public async Task HideAsync()
    {
        try
        {
            await _overlay.HideAsync();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "The progress overlay could not be hidden.");
        }
        finally
        {
            _card = null;
        }
    }
}
