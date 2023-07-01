using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.Window = this;

        InitializeComponent();

        SwapChainPanelHelper.SetSwapChainPanel(RootPanel, (nint)AppWindow.Id.Value);
        CurrentContext.OverlappedPresenter.IsResizable = false;
        CurrentContext.OverlappedPresenter.SetBorderAndTitleBar(false, false);
        CurrentContext.OverlappedPresenter.IsAlwaysOnTop = AppContext.AppConfig.TopMost;

        Activate();
    }

    ~MainWindow()
    {
        DispatcherTimerHelper.IsRunning = false;
        SwapChainPanelHelper.Dispose();
        DanmakuHelper.Current.Dispose();
        CreatorProvider.DisposeFormats();
    }
}
