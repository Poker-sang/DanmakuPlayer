using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class CollapsibleArea : ContentControl
{
    [GeneratedDependencyProperty(DefaultValue = true)]
    public partial bool IsActive { get; set; }

    public CollapsibleArea() => DefaultStyleKey = typeof(CollapsibleArea);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (this.FindDescendant<Border>() is { } border)
        {
            border.PointerEntered += OnPointerEntered;
            border.PointerExited += OnPointerExited;
            border.Opacity = IsActive ? 0 : 1;
            // 在设置初始值后添加过渡动画
            border.OpacityTransition = new();
        }

        base.OnApplyTemplate();
    }

    public void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        OnPointerEntered(e);
        if (!IsActive)
            return;
        sender.To<Border>().Opacity = 1;
    }

    public void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        OnPointerExited(e);
        if (!IsActive)
            return;
        sender.To<Border>().Opacity = 0;
    }

    partial void OnIsActiveChanged(bool newValue)
    {
        if (this.FindDescendant<Border>() is { } border)
            border.Opacity = newValue ? 0 : 1;
    }
}
