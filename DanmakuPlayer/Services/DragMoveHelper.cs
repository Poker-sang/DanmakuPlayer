using System.Drawing;
using System.Numerics;
using Windows.Win32;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;

namespace DanmakuPlayer.Services;

/// <summary>
/// Apply drag-move and resize function to <see cref="UIElement"/>
/// </summary>
public static class DragMoveHelper
{
    /// <summary>
    /// Subscribe <see cref="UIElement.PointerPressed"/>, <see cref="UIElement.PointerMoved"/>, <see cref="UIElement.PointerReleased"/>
    /// </summary>
    /// <param name="window"></param>
    /// <param name="element"></param>
    public static void SetDragMove(this Window window, UIElement element)
    {
        _Window = window;
        element.PointerPressed += RootPointerPressed;
        element.PointerMoved += RootPointerMoved;
        element.PointerReleased += RootPointerReleased;
    }

    /// <summary>
    /// Unsubscribe <see cref="UIElement.PointerPressed"/>, <see cref="UIElement.PointerMoved"/>, <see cref="UIElement.PointerReleased"/>
    /// </summary>
    /// <param name="element"></param>
    public static void UnsetDragMove(UIElement element)
    {
        element.PointerPressed -= RootPointerPressed;
        element.PointerMoved -= RootPointerMoved;
        element.PointerReleased -= RootPointerReleased;
    }

    private static Window _Window = null!;

    private static Point _LastPoint;
    private static bool _IsMoving;

    private const int MinOffset = 10;

    private static void RootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var frameworkElement = sender.To<FrameworkElement>();
        var point = e.GetCurrentPoint(frameworkElement);
        var properties = point.Properties;

        if (!properties.IsLeftButtonPressed)
            return;

        _IsMoving = true;

        _ = frameworkElement.CapturePointer(e.Pointer);
        _ = PInvoke.GetCursorPos(out _LastPoint);
    }

    private static void RootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var frameworkElement = sender.To<FrameworkElement>();
        var pointer = e.GetCurrentPoint(frameworkElement);

        var properties = pointer.Properties;
        if (!properties.IsLeftButtonPressed || !_IsMoving)
            return;

        _ = PInvoke.GetCursorPos(out var point);

        var xOffset = point.X - _LastPoint.X;
        var yOffset = point.Y - _LastPoint.Y;
        var offset = Vector2.DistanceSquared(Vector2.Zero, new(xOffset, yOffset));

        if (offset < MinOffset)
            return;

        var appWindow = _Window.AppWindow;
        if (appWindow.Presenter.To<OverlappedPresenter>() is { State : OverlappedPresenterState.Maximized } presenter)
        {
            var originalSize = appWindow.Size;
            presenter.Restore();
            var size = appWindow.Size;
            var rate = 1 - ((double) size.Width / originalSize.Width);
            appWindow.Move(new((int) (point.X * rate), (int) (point.Y * rate)));
        }
        else
        {
            var xPos = appWindow.Position.X;
            var yPos = appWindow.Position.Y;

            appWindow.Move(new(xPos + xOffset, yPos + yOffset));
        }

        _LastPoint.X = point.X;
        _LastPoint.Y = point.Y;
    }

    private static void RootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        sender.To<UIElement>().ReleasePointerCaptures();
        _IsMoving = false;
    }
}
