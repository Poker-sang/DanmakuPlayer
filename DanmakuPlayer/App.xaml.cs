using System;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace DanmakuPlayer;

public partial class App : Application
{
    public const ushort RemoteDebuggingPort = 9222;

    public static MainWindow Window { get; set; } = null!;

    public static OverlappedPresenter OverlappedPresenter => Window.AppWindow.Presenter.To<OverlappedPresenter>();

    public App()
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--remote-debugging-port={RemoteDebuggingPort}");
        InitializeComponent();
        AppContext.Initialize();
        HttpClientHelper.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        new MainWindow().Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            ExtendTitleBar = true,
            BackdropType = BackdropType.None,
            Title = nameof(DanmakuPlayer)
        });
        Window.RegisterUnhandledExceptionHandler();
    }
}
