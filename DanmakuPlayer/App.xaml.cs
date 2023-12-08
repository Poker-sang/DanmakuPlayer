using System;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace DanmakuPlayer;

public partial class App : Application
{
    public const ushort RemoteDebuggingPort = 9222;

    public App()
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--remote-debugging-port={RemoteDebuggingPort}");
        InitializeComponent();
        CurrentContext.Title = nameof(DanmakuPlayer);
        AppContext.Initialize();
        HttpClientHelper.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        new MainWindow().Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            TitleBarType = TitleBarType.AppWindow,
            BackdropType = BackdropType.None
        });
        AppHelper.RegisterUnhandledExceptionHandler(CurrentContext.Window);
    }
}
