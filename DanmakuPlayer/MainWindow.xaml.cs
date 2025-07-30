using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUI3Utilities;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.LAYERED_WINDOW_ATTRIBUTES_FLAGS;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX;

namespace DanmakuPlayer;

public sealed partial class MainWindow : Window
{
    public OverlappedPresenter OverlappedPresenter => AppWindow.Presenter.To<OverlappedPresenter>();

    public MainWindow()
    {
        InitializeComponent();

        var presenter = OverlappedPresenter;
        presenter.PreferredMinimumHeight = 450;
        presenter.PreferredMinimumWidth = 800;
        presenter.SetBorderAndTitleBar(true, false);

        var hwnd = WindowNative.GetWindowHandle(this);
        ApplyLegacyWindowEffects((HWND)hwnd);

        Activate();
    }

    private static void ApplyLegacyWindowEffects(HWND hwnd)
    {
        // 添加 WS_EX_LAYERED 样式
        var exStyle = (WINDOW_EX_STYLE) GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (int) (exStyle | WS_EX_LAYERED));

        // 设置窗口透明度
        SetLayeredWindowAttributes(hwnd, default, 255, LWA_ALPHA);

        // 扩展 DWM 边框
        DwmExtendFrameIntoClientArea(hwnd, new()
        {
            cxLeftWidth = 1,
            cxRightWidth = 1,
            cyBottomHeight = 1,
            cyTopHeight = 1
        });
    }

    ~MainWindow()
    {
        DanmakuHelper.Current.Dispose();
        CreatorProvider.DisposeFormats();
    }
}
