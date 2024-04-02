using System;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUIEx;

namespace DanmakuPlayer;

public partial class App : Application
{
    public const ushort RemoteDebuggingPort = 9222;

    public static MainWindow Window { get; set; } = null!;

    public static OverlappedPresenter OverlappedPresenter => Window.OverlappedPresenter;

    public App()
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--remote-debugging-port={RemoteDebuggingPort}");
        InitializeComponent();
        AppContext.Initialize();
        HttpClientHelper.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new() { SystemBackdrop = new TransparentTintBackdrop() };
        Window.Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            ExtendTitleBar = true,
            BackdropType = BackdropType.Maintain,
            Title = nameof(DanmakuPlayer),
            IconPath = new(AppContext.ApplicationUriToPath(new("ms-appx:///Assets/DanmakuPlayer.ico")))
        });
        Window.RegisterUnhandledExceptionHandler();
        Window.Activate();
    }
}
