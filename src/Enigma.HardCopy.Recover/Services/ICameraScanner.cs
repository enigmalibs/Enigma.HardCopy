using System;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Recover.Services;

/// <summary>
/// The phone's camera, reduced to what a recovery needs from it: start, stop, and the codes it sees.
/// </summary>
/// <remarks>
/// <para>
/// The platform head implements this; the shared project only consumes it, so the whole scanning flow is tested
/// against a fake that raises <see cref="CodesScanned"/> on demand.
/// </para>
/// <para>
/// The same code is reported again on every frame in which it is in view — many times a second. Telling a repeat
/// from a new code is the consumer's job, because only the consumer knows what it has already done with each one.
/// </para>
/// </remarks>
public interface ICameraScanner
{
    /// <summary>
    /// Occurs when one or more QR codes were read from a camera frame. Always raised on the UI thread, so a handler
    /// may touch bound state directly.
    /// </summary>
    event EventHandler<CodesScannedEventArgs>? CodesScanned;

    /// <summary>
    /// Starts the camera, asking the user for the permission first when it has not been granted.
    /// </summary>
    /// <param name="cancellationToken">A token to stop waiting for the camera.</param>
    /// <returns>
    /// Whether the camera started, and if not, why. Expected failures — a refused permission, a missing or broken
    /// camera — are reported here rather than thrown.
    /// </returns>
    /// <remarks>
    /// Starting a camera that is already running changes nothing and reports <see cref="CameraStartResult.Started"/>.
    /// </remarks>
    Task<CameraStartResult> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the camera. No code is reported after this returns. Stopping a camera that is not running does nothing.
    /// </summary>
    void Stop();
}
