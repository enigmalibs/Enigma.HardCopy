using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Recover.Camera;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The scanner of a platform with no camera support: it never starts and never reports a code.
/// </summary>
/// <remarks>
/// The default registration, which a platform head replaces with its own. It keeps the application usable — and
/// honest about why nothing scans — wherever no camera implementation exists, the XAML previewer included.
/// </remarks>
public sealed class UnavailableCameraScanner : ICameraScanner
{
    /// <inheritdoc/>
    /// <remarks>Never raised, so subscriptions are not even kept.</remarks>
    public event EventHandler<CodesScannedEventArgs>? CodesScanned
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    /// <remarks>Never raised, so subscriptions are not even kept.</remarks>
    public event EventHandler<CameraFrameEventArgs>? FrameAvailable
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public Task<CameraStartResult> StartAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CameraStartResult.Unavailable);

    /// <inheritdoc/>
    public void Stop()
    {
    }
}
