using Microsoft.UI.Xaml;
using DanmakuPlayer.Services;
using WinUI3Utilities;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.Window = this;
        // CurrentContext.OverlappedPresenter.SetBorderAndTitleBar(false, false);

        InitializeComponent();
        SwapChainPanelHelper.SetSwapChainPanel(RootPanel, CurrentContext.HWnd);

        CurrentContext.OverlappedPresenter.IsResizable = false;
        CurrentContext.OverlappedPresenter.SetBorderAndTitleBar(false, false);
    }
}
