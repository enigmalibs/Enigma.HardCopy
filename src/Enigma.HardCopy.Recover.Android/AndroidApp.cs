using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Enigma.HardCopy.Recover.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.HardCopy.Recover.Android;

/// <summary>
/// The Android process's entry point: sets Avalonia up once per process, before any activity starts, and contributes
/// the services only Android can provide.
/// </summary>
[Application]
public sealed class AndroidApp : AvaloniaAndroidApplication<App>
{
    private readonly ActivityTracker _activities = new();

    /// <summary>Initializes a new instance of the <see cref="AndroidApp"/> class.</summary>
    /// <param name="javaReference">The handle of the Java peer.</param>
    /// <param name="transfer">Who owns <paramref name="javaReference"/>.</param>
    public AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    /// <inheritdoc />
    public override void OnCreate()
    {
        // Before Avalonia starts the first activity, so the tracker sees it being created.
        RegisterActivityLifecycleCallbacks(_activities);

        base.OnCreate();
    }

    /// <inheritdoc />
    protected override AppBuilder CreateAppBuilder()
        => AppBuilder.Configure(() => new App(AddPlatformServices)).UseAndroid();

    /// <inheritdoc />
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();

    /// <summary>Registers the camera and the haptics, which pre-empt the shared project's defaults.</summary>
    /// <param name="services">The application's container.</param>
    private void AddPlatformServices(IServiceCollection services)
    {
        services.AddSingleton(_activities);
        services.AddSingleton<ICameraScanner, CameraXScanner>();
        services.AddSingleton<IHapticFeedback, ViewHapticFeedback>();
    }
}
