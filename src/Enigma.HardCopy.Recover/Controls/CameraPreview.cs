using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Enigma.HardCopy.Recover.Camera;
using Enigma.HardCopy.Recover.Services;

namespace Enigma.HardCopy.Recover.Controls;

/// <summary>
/// Draws the camera's live picture, filling the control and cropping the overflow, so the user can aim at a code.
/// </summary>
/// <remarks>
/// <para>
/// <b>Drawn by Avalonia, not by a native view.</b> A native camera view embedded in the visual tree always paints
/// over Avalonia content, which would hide the info bar and the dialogs behind the preview. Frames are converted here
/// and drawn like any other bitmap, so everything layered above the page stays above it.
/// </para>
/// <para>
/// <b>At most one frame in flight.</b> Frames arrive on a camera thread. The newest one is converted into a buffer
/// only when the previous one has reached the screen; until then frames are skipped outright — not queued, and not
/// even converted — so a busy UI thread costs the preview smoothness, never memory or latency.
/// </para>
/// </remarks>
public sealed class CameraPreview : Control
{
    private ICameraScanner? _source;
    private bool _attached;

    /// <summary>1 while a converted frame waits for the UI thread, 0 otherwise.</summary>
    private int _pending;

    private byte[] _pixels = [];
    private int _pixelWidth;
    private int _pixelHeight;
    private WriteableBitmap? _bitmap;

    /// <summary>Gets or sets the camera whose frames are drawn.</summary>
    public ICameraScanner? Source
    {
        get => _source;
        set
        {
            if (ReferenceEquals(_source, value))
            {
                return;
            }

            Unsubscribe();
            _source = value;
            Subscribe();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_bitmap is not { } bitmap || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        // Uniform to fill: the shorter side fits, the longer one is cropped by the clip — a preview with bars would
        // waste the little screen there is.
        Size image = bitmap.Size;
        double scale = Math.Max(Bounds.Width / image.Width, Bounds.Height / image.Height);
        Size drawn = new(image.Width * scale, image.Height * scale);
        Rect destination = new(
            (Bounds.Width - drawn.Width) / 2,
            (Bounds.Height - drawn.Height) / 2,
            drawn.Width,
            drawn.Height);

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            context.DrawImage(bitmap, new Rect(image), destination);
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _attached = true;
        Subscribe();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        Unsubscribe();
        _attached = false;
        _bitmap?.Dispose();
        _bitmap = null;
    }

    private void Subscribe()
    {
        if (_attached && _source is not null)
        {
            _source.FrameAvailable += OnFrameAvailable;
        }
    }

    private void Unsubscribe()
    {
        if (_source is not null)
        {
            _source.FrameAvailable -= OnFrameAvailable;
        }
    }

    /// <summary>Runs on the camera thread: converts the frame, unless the previous one is still on its way.</summary>
    private void OnFrameAvailable(object? sender, CameraFrameEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _pending, 1, 0) != 0)
        {
            return;
        }

        try
        {
            YuvFrame frame = e.Frame;
            (int width, int height) = CameraFrameConverter.GetPreviewSize(frame.Width, frame.Height, frame.RotationDegrees);
            int length = width * height * CameraFrameConverter.BytesPerPixel;
            if (_pixels.Length != length)
            {
                _pixels = new byte[length];
            }

            CameraFrameConverter.ToBgraPreview(frame, _pixels);
            _pixelWidth = width;
            _pixelHeight = height;
        }
        catch
        {
            // The flag is what lets the next frame through; leaving it set would freeze the preview for good.
            Volatile.Write(ref _pending, 0);
            throw;
        }

        Dispatcher.UIThread.Post(Present, DispatcherPriority.Render);
    }

    /// <summary>Runs on the UI thread: copies the converted frame into the bitmap and asks for a redraw.</summary>
    private void Present()
    {
        try
        {
            if (!_attached)
            {
                return;
            }

            if (_bitmap is null || _bitmap.PixelSize.Width != _pixelWidth || _bitmap.PixelSize.Height != _pixelHeight)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(
                    new PixelSize(_pixelWidth, _pixelHeight),
                    new Vector(96, 96),
                    PixelFormats.Bgra8888,
                    AlphaFormat.Opaque);
            }

            using (ILockedFramebuffer framebuffer = _bitmap.Lock())
            {
                int rowLength = _pixelWidth * CameraFrameConverter.BytesPerPixel;
                for (int row = 0; row < _pixelHeight; row++)
                {
                    Marshal.Copy(_pixels, row * rowLength, framebuffer.Address + (row * framebuffer.RowBytes), rowLength);
                }
            }
        }
        finally
        {
            Volatile.Write(ref _pending, 0);
        }

        InvalidateVisual();
    }
}
