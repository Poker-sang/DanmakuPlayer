using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace DanmakuPlayer.Services;

public static partial class LayerWindowHelper
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
    private const int WS_EX_LAYERED = 0x80000;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
    private const int GWL_EXSTYLE = -20;

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int GetWindowLongA(nint hWnd, int nIndex);


    [LibraryImport("user32.dll")]
    internal static partial int SetWindowLongA(nint hWnd, int nIndex, int dwNewLong);

    public static void SetLayerWindow(Window window)
    {
        var hWnd = (nint)window.AppWindow.Id.Value;
        var exStyle = GetWindowLongA(hWnd, GWL_EXSTYLE);
        if ((exStyle & WS_EX_LAYERED) is 0) 
            _ = SetWindowLongA(hWnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
    }
}
