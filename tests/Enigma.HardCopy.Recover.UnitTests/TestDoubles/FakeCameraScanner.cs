using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.UnitTests.TestDoubles;

/// <summary>
/// A camera that starts with whatever result the test chose, and "sees" whatever codes the test shows it.
/// </summary>
internal sealed class FakeCameraScanner : ICameraScanner
{
    /// <inheritdoc/>
    public event EventHandler<CodesScannedEventArgs>? CodesScanned;

    /// <summary>Gets or sets what the next <see cref="StartAsync"/> reports.</summary>
    internal CameraStartResult NextStartResult { get; set; } = CameraStartResult.Started;

    /// <summary>Gets a value indicating whether the camera is running.</summary>
    internal bool IsRunning { get; private set; }

    /// <summary>Gets how many times the camera was asked to start.</summary>
    internal int StartCount { get; private set; }

    /// <summary>Gets how many times the camera was asked to stop.</summary>
    internal int StopCount { get; private set; }

    /// <inheritdoc/>
    public Task<CameraStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        StartCount++;
        IsRunning = NextStartResult is CameraStartResult.Started;

        return Task.FromResult(NextStartResult);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        StopCount++;
        IsRunning = false;
    }

    /// <summary>Reports one frame holding <paramref name="codes"/>, whether or not the camera is running.</summary>
    /// <param name="codes">The texts read from the frame.</param>
    internal void Show(params string[] codes) => CodesScanned?.Invoke(this, new CodesScannedEventArgs(codes));
}
