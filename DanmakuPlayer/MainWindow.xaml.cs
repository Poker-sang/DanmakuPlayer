using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Utilities;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public OverlappedPresenter OverlappedPresenter => AppWindow.Presenter.To<OverlappedPresenter>();

    public MainWindow()
    {
        InitializeComponent();

        LayerWindowHelper.SetLayerWindow(this);
        var overlappedPresenter = OverlappedPresenter;
        overlappedPresenter.IsResizable = false;
        overlappedPresenter.SetBorderAndTitleBar(false, false);
        overlappedPresenter.IsAlwaysOnTop = AppContext.AppConfig.TopMost;

        Activate();
    }

    ~MainWindow()
    {
        DispatcherTimerHelper.IsRunning = false;
        DanmakuHelper.Current.Dispose();
        CreatorProvider.DisposeFormats();
    }
}
