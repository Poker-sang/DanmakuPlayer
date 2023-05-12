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

        SwapChainPanelHelper.SetSwapChainPanel(RootPanel, (nint)CurrentContext.HWnd);
        CurrentContext.OverlappedPresenter.IsResizable = false;
        CurrentContext.OverlappedPresenter.SetBorderAndTitleBar(false, false);
    }

    ~MainWindow()
    {
        DispatcherTimerHelper.IsRunning = false;
        SwapChainPanelHelper.Dispose();
        DanmakuHelper.Current.Dispose();
        CreatorProvider.DisposeFormats();
    }
}
