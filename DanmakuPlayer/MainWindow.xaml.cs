using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUI3Utilities.Models;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.WindowInfo = new WindowInfo(this);

        InitializeComponent();

        SwapChainPanelHelper.SetSwapChainPanel(RootPanel, CurrentContext.HWnd);
        CurrentContext.OverlappedPresenter.IsResizable = false;
        CurrentContext.OverlappedPresenter.SetBorderAndTitleBar(false, false);
    }

    ~MainWindow()
    {
        SwapChainPanelHelper.Dispose();
        DanmakuHelper.Current.Dispose();
        CreatorProvider.DisposeFormats();
    }
}
