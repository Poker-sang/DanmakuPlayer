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
        AppContext.ResetTimerInterval();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow();
        AppHelper.Initialize(WindowHelper.EstimatedWindowSize(), BackdropHelper.BackdropType.None);
    }
}
