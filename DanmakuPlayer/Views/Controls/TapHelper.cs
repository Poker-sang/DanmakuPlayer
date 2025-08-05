using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DanmakuPlayer.Views.Controls;

/// <summary>
/// 阻止按钮把双击事件传递给背景
/// </summary>
public class TapHelper
{
    public static readonly DependencyProperty DoubleTappedDisabledProperty =
        DependencyProperty.RegisterAttached(
            "DoubleTappedDisabled",
            typeof(bool),
            typeof(Button),
            new(false)
        );

    public static void SetDoubleTappedDisabled(UIElement element, bool value)
    {
        element.SetValue(DoubleTappedDisabledProperty, value);
        if (value)
            element.DoubleTapped += UIElement_OnDoubleTapped;
        else
            element.DoubleTapped -= UIElement_OnDoubleTapped;
    }

    public static bool GetDoubleTappedDisabled(UIElement element)
    {
        return (bool) element.GetValue(DoubleTappedDisabledProperty);
    }

    private static void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => e.Handled = true;
}
