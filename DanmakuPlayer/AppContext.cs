using DanmakuPlayer.Views.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Storage;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[AppContext<AppConfig>(ConfigKey = "Configuration", MethodName = "Configuration")]
public static partial class AppContext
{
    public static string AppLocalFolder { get; private set; } = null!;

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

    public static AppConfig AppConfig { get; set; } = null!;

    public static BackgroundPanel BackgroundPanel { get; set; } = null!;

    public static CanvasControl DanmakuCanvas { get; set; } = null!;
}
