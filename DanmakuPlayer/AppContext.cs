using System;
using System.IO;
using Windows.ApplicationModel;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Windows.Storage;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[AppContext<AppConfig>(ConfigKey = "Configuration", MethodName = "Configuration")]
public static partial class AppContext
{
    public static string AppLocalFolder { get; private set; } = null!;

    public static AppConfig AppConfig { get; set; } = null!;

    public static CanvasAnimatedControl DanmakuCanvas { get; set; } = null!;

    public static void Initialize()
    {
        AppLocalFolder = ApplicationData.GetDefault().LocalFolder.Path;
        InitializeConfiguration();
        AppConfig = LoadConfiguration() is not { } appConfigurations
#if FIRST_TIME
            || true
#endif
            ? new() : appConfigurations;
    }

    public static string ApplicationUriToPath(Uri uri)
    {
        if (uri.Scheme is not "ms-appx")
            // ms-appdata is handled by the caller.
            throw new InvalidOperationException("Uri is not using the ms-appx scheme");

        var path = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');

        return Path.Combine(Package.Current.InstalledPath, uri.Host, path);
    }

    public static void SetTimerInterval()
    {
        DanmakuCanvas.TargetElapsedTime = TimeSpan.FromSeconds(1d / AppConfig.PlayFramePerSecond);
    }
}
