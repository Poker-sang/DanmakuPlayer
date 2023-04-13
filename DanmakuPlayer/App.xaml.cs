using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace DanmakuPlayer;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        CurrentContext.Title = nameof(DanmakuPlayer);
        AppContext.Initialize();
        HttpClientHelper.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow();
        AppHelper.Initialize(new()
        {
            Size = WindowHelper.EstimatedWindowSize(),
            TitleBarType = TitleBarHelper.TitleBarType.AppWindow,
            BackdropType = BackdropType.None
        });
    }
}
