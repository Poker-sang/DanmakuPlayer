using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Xaml;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        App.Window = this;

        InitializeComponent();

        SwapChainPanelHelper.SetSwapChainPanel(RootPanel, (nint)AppWindow.Id.Value);
        var overlappedPresenter = App.OverlappedPresenter;
        overlappedPresenter.IsResizable = false;
        overlappedPresenter.SetBorderAndTitleBar(false, false);
        overlappedPresenter.IsAlwaysOnTop = AppContext.AppConfig.TopMost;

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
