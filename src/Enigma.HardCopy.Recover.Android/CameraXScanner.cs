using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using AndroidX.AppCompat.App;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Avalonia.Android;
using Avalonia.Threading;
using Enigma.HardCopy.Core;
using Enigma.HardCopy.Recover.Camera;
using Enigma.HardCopy.Recover.Services;
using Google.Common.Util.Concurrent;
using Java.Lang;
using Java.Nio;
using Java.Util.Concurrent;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;
using Size = Android.Util.Size;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// The <see cref="ICameraScanner"/> of an Android phone: CameraX frames, decoded in this process by Core.
/// </summary>
/// <remarks>
/// <para>
/// <b>One use case, frames only.</b> CameraX delivers each frame to an analyzer on a dedicated thread, and nothing
/// else is bound — no native preview surface. That thread hands the frame to <see cref="FrameAvailable"/> for the
/// preview, and passes its luminance to a single decode worker when that worker is idle. A frame that arrives while
/// a read is running is shown but not read: reading is the slow part, and the next idle moment takes the newest
/// frame rather than a queue of stale ones.
/// </para>
/// <para>
/// <b>Nothing leaves the process, and nothing is written.</b> Decoding is ZXing behind Core's
/// <see cref="ImageDecoder"/> — no Play Services, no barcode SDK — and frames live in reused memory buffers only.
/// </para>
/// <para>
/// The camera is bound to the activity's lifecycle, so it pauses by itself when the app goes to the background and
/// resumes when it returns. <see cref="CodesScanned"/> is raised on the UI thread, as the interface promises.
/// </para>
/// </remarks>
internal sealed class CameraXScanner : ICameraScanner
{
    /// <summary>Identifies this scanner's permission request among the activity's results.</summary>
    private const int CameraPermissionRequestCode = 0x4543;

    /// <summary>
    /// The analysis resolution asked for. A dense code needs the pixels — a default-density symbol has over a hundred
    /// modules a side — and 1080p is what every current phone delivers to an analyzer without strain.
    /// </summary>
    private static readonly Size _analysisResolution = new(1920, 1080);

    private readonly ActivityTracker _activities;
    private readonly ILogger<CameraXScanner> _logger;
    private readonly IExecutorService _analysisThread = Executors.NewSingleThreadExecutor()
        ?? throw new InvalidOperationException("No executor could be created for the camera frames.");

    private ProcessCameraProvider? _provider;
    private bool _running;

    /// <summary>Whether codes still go out — cleared by <see cref="Stop"/> before any frame can be unbound.</summary>
    private volatile bool _delivering;

    /// <summary>1 while the decode worker is reading a frame, 0 when it is idle.</summary>
    private int _decoding;

    // Reused across frames. The three planes belong to the analysis thread; the luminance copy belongs to the decode
    // worker while it runs, and to the analysis thread otherwise.
    private byte[] _y = [];
    private byte[] _u = [];
    private byte[] _v = [];
    private byte[] _luminance = [];

    /// <summary>Initializes a new instance of the <see cref="CameraXScanner"/> class.</summary>
    /// <param name="activities">Knows the activity the camera binds to and asks permission through.</param>
    /// <param name="logger">Records why a camera could not start; never a code's text.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public CameraXScanner(ActivityTracker activities, ILogger<CameraXScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(logger);

        _activities = activities;
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<CodesScannedEventArgs>? CodesScanned;

    /// <inheritdoc/>
    public event EventHandler<CameraFrameEventArgs>? FrameAvailable;

    /// <inheritdoc/>
    public async Task<CameraStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_running)
        {
            return CameraStartResult.Started;
        }

        if (_activities.Current is not { } activity
            || activity.PackageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) != true)
        {
            return CameraStartResult.Unavailable;
        }

        if (!await EnsurePermissionAsync(activity, cancellationToken))
        {
            return CameraStartResult.PermissionDenied;
        }

        try
        {
            ProcessCameraProvider provider = _provider ??= await GetProviderAsync(activity);
            if (ChooseCamera(provider) is not { } camera)
            {
                return CameraStartResult.Unavailable;
            }

            provider.UnbindAll();
            provider.BindToLifecycle(activity, camera, CreateAnalysis());
        }
        catch (Java.Lang.Exception ex)
        {
            // A camera in use by another app, a device policy, a driver failure: all mean "no camera" to the user.
            _logger.LogError(ex, "The camera could not be started.");
            return CameraStartResult.Unavailable;
        }

        _delivering = true;
        _running = true;

        return CameraStartResult.Started;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _delivering = false;
        if (!_running)
        {
            return;
        }

        _running = false;
        _provider?.UnbindAll();
    }

    /// <summary>
    /// The one use case bound: frames to the analyzer, at the analysis resolution or the nearest the camera offers,
    /// always the newest — a frame not analysed in time is dropped rather than queued.
    /// </summary>
    /// <remarks>
    /// The builders mutate themselves and return themselves, so their return values are not chained: the binding
    /// declares them nullable, and the builder in hand is the same object anyway.
    /// </remarks>
    private ImageAnalysis CreateAnalysis()
    {
        ResolutionSelector.Builder resolution = new();
        resolution.SetResolutionStrategy(new ResolutionStrategy(
            _analysisResolution,
            ResolutionStrategy.FallbackRuleClosestHigherThenLower));

        ImageAnalysis.Builder analysis = new();
        analysis.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest);
        analysis.SetResolutionSelector(resolution.Build());

        ImageAnalysis useCase = analysis.Build()
            ?? throw new InvalidOperationException("CameraX built no analysis use case.");
        useCase.SetAnalyzer(_analysisThread, new FrameAnalyzer(this));

        return useCase;
    }

    /// <summary>Runs on the analysis thread, once per frame.</summary>
    /// <param name="image">The frame. Always closed before this returns, or CameraX delivers no further frame.</param>
    private void Analyze(IImageProxy image)
    {
        try
        {
            IImageProxyPlaneProxy[] planes = image.GetPlanes()
                ?? throw new InvalidOperationException("The camera delivered a frame without planes.");
            int width = image.Width;
            int height = image.Height;

            int lumaLength = CopyPlane(planes[0], ref _y);
            CopyPlane(planes[1], ref _u);
            CopyPlane(planes[2], ref _v);

            YuvFrame frame = new(
                _y,
                planes[0].RowStride,
                _u,
                _v,
                planes[1].RowStride,
                planes[1].PixelStride,
                width,
                height,
                image.ImageInfo?.RotationDegrees ?? 0);

            FrameAvailable?.Invoke(this, new CameraFrameEventArgs(frame));
            ReadCodesWhenIdle(width, height, planes[0].RowStride, lumaLength);
        }
        catch (Exception ex)
        {
            // This runs on a Java thread: an exception escaping here would end the process. One bad frame is not worth
            // that — the next one is a few milliseconds away.
            _logger.LogError(ex, "A camera frame could not be processed.");
        }
        finally
        {
            image.Close();
        }
    }

    /// <summary>
    /// Hands the frame's luminance to the decode worker, unless it is still busy with an earlier frame.
    /// </summary>
    private void ReadCodesWhenIdle(int width, int height, int rowStride, int length)
    {
        if (Interlocked.CompareExchange(ref _decoding, 1, 0) != 0)
        {
            return;
        }

        if (_luminance.Length < length)
        {
            _luminance = new byte[length];
        }

        Array.Copy(_y, _luminance, length);
        byte[] luminance = _luminance;

        _ = Task.Run(() =>
        {
            try
            {
                IReadOnlyList<string> codes = ImageDecoder.ReadCodes(luminance, width, height, rowStride);
                if (codes.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_delivering)
                        {
                            CodesScanned?.Invoke(this, new CodesScannedEventArgs(codes));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Nothing awaits this task; a failure must end here rather than vanish as an unobserved exception.
                _logger.LogError(ex, "A camera frame could not be read.");
            }
            finally
            {
                Volatile.Write(ref _decoding, 0);
            }
        });
    }

    /// <summary>Copies one plane out of the camera's direct buffer into a reused managed array.</summary>
    /// <returns>The number of bytes copied.</returns>
    private static int CopyPlane(IImageProxyPlaneProxy plane, ref byte[] target)
    {
        ByteBuffer buffer = plane.Buffer
            ?? throw new InvalidOperationException("The camera delivered a plane without a buffer.");
        int length = buffer.Remaining();
        if (target.Length < length)
        {
            target = new byte[length];
        }

        IntPtr address = buffer.GetDirectBufferAddress();
        Marshal.Copy(address + buffer.Position(), target, 0, length);

        return length;
    }

    private static CameraSelector? ChooseCamera(ProcessCameraProvider provider)
    {
        // A tablet or a kiosk device may have only a front camera; a code held up to it still scans.
        foreach (CameraSelector? camera in (CameraSelector?[])[CameraSelector.DefaultBackCamera, CameraSelector.DefaultFrontCamera])
        {
            if (camera is not null && provider.HasCamera(camera))
            {
                return camera;
            }
        }

        return null;
    }

    private static Task<ProcessCameraProvider> GetProviderAsync(Context context)
    {
        IListenableFuture future = ProcessCameraProvider.GetInstance(context);
        TaskCompletionSource<ProcessCameraProvider> provider = new(TaskCreationOptions.RunContinuationsAsynchronously);

        future.AddListener(
            new Runnable(() =>
            {
                try
                {
                    if (future.Get().JavaCast<ProcessCameraProvider>() is { } instance)
                    {
                        provider.TrySetResult(instance);
                    }
                    else
                    {
                        provider.TrySetException(new InvalidOperationException("CameraX returned no camera provider."));
                    }
                }
                catch (Java.Lang.Exception ex)
                {
                    provider.TrySetException(ex);
                }
            }),
            ContextCompat.GetMainExecutor(context));

        return provider.Task;
    }

    /// <summary>
    /// Asks for the camera permission when it has not been granted yet, and waits for the user's answer.
    /// </summary>
    /// <returns><see langword="true"/> if the camera may be used.</returns>
    private static async Task<bool> EnsurePermissionAsync(AppCompatActivity activity, CancellationToken cancellationToken)
    {
        if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.Camera) == Permission.Granted)
        {
            return true;
        }

        if (activity is not IActivityResultHandler results)
        {
            return false;
        }

        TaskCompletionSource<bool> answer = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == CameraPermissionRequestCode)
            {
                answer.TrySetResult(grantResults.Length > 0 && grantResults[0] == Permission.Granted);
            }
        }

        results.RequestPermissionsResult += OnResult;
        try
        {
            ActivityCompat.RequestPermissions(activity, [Manifest.Permission.Camera], CameraPermissionRequestCode);

            using (cancellationToken.Register(() => answer.TrySetCanceled(cancellationToken)))
            {
                return await answer.Task;
            }
        }
        finally
        {
            results.RequestPermissionsResult -= OnResult;
        }
    }

    /// <summary>The Java-side analyzer CameraX calls; it only forwards to the scanner.</summary>
    private sealed class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly CameraXScanner _scanner;

        public FrameAnalyzer(CameraXScanner scanner)
        {
            _scanner = scanner;
        }

        public void Analyze(IImageProxy? image)
        {
            if (image is not null)
            {
                _scanner.Analyze(image);
            }
        }
    }
}
