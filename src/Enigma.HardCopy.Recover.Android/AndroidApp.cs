using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// The Android process's entry point: sets Avalonia up once per process, before any activity starts.
/// </summary>
[Application]
public sealed class AndroidApp : AvaloniaAndroidApplication<App>
{
    /// <summary>Initializes a new instance of the <see cref="AndroidApp"/> class.</summary>
    /// <param name="javaReference">The handle of the Java peer.</param>
    /// <param name="transfer">Who owns <paramref name="javaReference"/>.</param>
    public AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    /// <inheritdoc />
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
}
