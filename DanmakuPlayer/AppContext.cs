using System;
using System.IO;
using Windows.Storage;
using DanmakuPlayer.Views.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using WinUI3Utilities.Attributes;
using Windows.ApplicationModel;
using WinUI3Utilities;

namespace DanmakuPlayer;

[AppContext<AppConfig>(ConfigKey = "Configuration", MethodName = "Configuration")]
public static partial class AppContext
{
    public static string AppLocalFolder { get; private set; } = null!;

    public static AppConfig AppConfig { get; set; } = null!;

    public static BackgroundPanel BackgroundPanel { get; set; } = null!;

    public static CanvasControl DanmakuCanvas { get; set; } = null!;

    public static void Initialize()
    {
        AppLocalFolder = ApplicationData.Current.LocalFolder.Path;
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
        {
            // ms-appdata is handled by the caller.
            ThrowHelper.InvalidOperation("Uri is not using the ms-appx scheme");
        }

        var path = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');

        return Path.Combine(Package.Current.InstalledPath, uri.Host, path);
    }
}
