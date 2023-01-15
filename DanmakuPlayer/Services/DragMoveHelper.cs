using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using Windows.Graphics;
using Microsoft.UI.Windowing;

namespace DanmakuPlayer.Services;

public static class DragMoveHelper
{
    private static POINT _mousePoint;
    private static PointInt32 _windowPosition;
    private static bool _moving;

    public static void SetDragMove(this UIElement element, AppWindow appWindow, OverlappedPresenter presenter)
    {
        element.PointerPressed += (o, e) => RootPointerPressed(o, e, appWindow);
        element.PointerMoved += (o, e) => RootPointerMoved(o, e, appWindow, presenter);
        element.PointerReleased += RootPointerReleased;
    }

    private static void RootPointerPressed(object sender, PointerRoutedEventArgs e, AppWindow appWindow)
    {
        var element = (UIElement)sender;
        var properties = e.GetCurrentPoint(element).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _ = element.CapturePointer(e.Pointer);
            _windowPosition = appWindow.Position;
            _ = User32.GetCursorPos(out _mousePoint);
            _moving = true;
        }
    }

    private static void RootPointerMoved(object sender, PointerRoutedEventArgs e, AppWindow appWindow, OverlappedPresenter presenter)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _ = User32.GetCursorPos(out var pt);
            if (_moving && Math.Pow(pt.X - _mousePoint.X, 2) + Math.Pow(pt.Y - _mousePoint.Y, 2) > 5)
            {
                if (presenter.State is OverlappedPresenterState.Maximized)
                {
                    var originalSize = appWindow.Size;
                    presenter.Restore();
                    var size = appWindow.Size;
                    var rate = 1 - (double)size.Width / originalSize.Width;
                    _windowPosition = new((int)(pt.X * rate), (int)(pt.Y * rate));
                }
                appWindow.Move(new(_windowPosition.X + (pt.X - _mousePoint.X),
                    _windowPosition.Y + (pt.Y - _mousePoint.Y)));
            }
        }
    }

    private static void RootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ((UIElement)sender).ReleasePointerCaptures();
        _moving = false;
    }
}
