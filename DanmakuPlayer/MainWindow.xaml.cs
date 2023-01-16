using System;
using Microsoft.UI.Xaml;
using DanmakuPlayer.Services;
using WinUI3Utilities;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        CurrentContext.Window = this;

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
