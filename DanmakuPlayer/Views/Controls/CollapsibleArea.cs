using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class CollapsibleArea : ContentControl
{
    public CollapsibleArea()
    {
        DefaultStyleKey = typeof(CollapsibleArea);
        Loaded += (sender, _) =>
        {
            var that = sender.To<ContentControl>();
            if (that.FindDescendant<Border>() is { } border)
            {
                border.PointerEntered += OnPointerEntered;
                border.PointerExited += OnPointerExited;
            }
        };
    }

    [GeneratedDependencyProperty(DefaultValue = true)]
    public partial bool Pinned { get; set; }

    public void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        OnPointerEntered(e);
        sender.To<Border>().Opacity = 1;
    }

    public void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        OnPointerExited(e);
        sender.To<Border>().Opacity = Pinned ? 1 : 0;
    }

    partial void OnPinnedChanged(bool newValue)
    {
        if (this.FindDescendant<Border>() is { } border)
        {
            border.Opacity = newValue ? 1 : 0;
        }
    }
}
