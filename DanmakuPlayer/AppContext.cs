using System;
using Windows.Storage;
using DanmakuPlayer.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[AppContext<AppConfig>]
public static partial class AppContext
{
    public static string AppLocalFolder { get; private set; } = null!;

    public static void Initialize()
    {
        AppLocalFolder = ApplicationData.Current.LocalFolder.Path;
        InitializeConfigurationContainer();
        AppConfig = LoadConfiguration() is not { } appConfigurations
#if FIRST_TIME
        || true
#endif
            ? new AppConfig() : appConfigurations;
    }

    public static AppConfig AppConfig { get; private set; } = null!;

    public static BackgroundPanel BackgroundPanel { get; set; } = null!;

    public static CanvasControl DanmakuCanvas { get; set; } = null!;

    public static DispatcherTimer Timer { get; } = new();

    public static void ResetTimerInterval() => Timer.Interval = TimeSpan.FromSeconds(1d / (AppConfig.PlayFramePerSecond * AppConfig.PlaySpeed));
}
