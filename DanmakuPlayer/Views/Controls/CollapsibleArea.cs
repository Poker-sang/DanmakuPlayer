using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed class CollapsibleArea : ContentControl
{
    public CollapsibleArea()
    {
        DefaultStyleKey = typeof(CollapsibleArea);
        Loaded += (sender, _) =>
        {
            var that = sender.To<ContentControl>();
            that.Content.To<UIElement>().Visibility = Visibility.Collapsed;
            if (that.FindDescendant<Border>() is { } border)
            {
                border.PointerEntered += OnPointerEntered;
                border.PointerExited += OnPointerExited;
            }
        };
    }

    public void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        OnPointerEntered(e);
        sender.To<Border>().Child.To<UIElement>().Visibility = Visibility.Visible;
    }

    public void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        OnPointerExited(e);
        sender.To<Border>().Child.To<UIElement>().Visibility = Visibility.Collapsed;
    }
}
