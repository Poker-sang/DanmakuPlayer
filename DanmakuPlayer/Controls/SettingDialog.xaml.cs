using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using DanmakuPlayer.Enums;
using Microsoft.UI;
using WinUI3Utilities;

namespace DanmakuPlayer.Controls;

public sealed partial class SettingDialog : UserControl
{
    private readonly SettingViewModel _vm = new();

    public SettingDialog() => InitializeComponent();

    public async Task ShowAsync() => await ((ContentDialog)Content).ShowAsync();

    #region 事件处理

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        sender.Hide();

        AppContext.SaveConfiguration(_vm.AppConfig);
    }

    #endregion

    private void NavigateUriClick(object sender, RoutedEventArgs e)
    {
        var process = new Process
        {
            StartInfo = new()
            {
                FileName = (string)((FrameworkElement)sender).Tag,
                UseShellExecute = true
            }
        };
        _ = process.Start();
    }

    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        var value = sender.ToNotNull<RadioButtons>().SelectedIndex;

        var selectedTheme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (CurrentContext.Window.Content is FrameworkElement rootElement)
            rootElement.RequestedTheme = selectedTheme;

        CurrentContext.App.Resources["WindowCaptionForeground"] = selectedTheme switch
        {
            ElementTheme.Dark => Colors.White,
            ElementTheme.Light => Colors.Black,
            _ => CurrentContext.App.RequestedTheme is ApplicationTheme.Dark ? Colors.White : Colors.Black
        };
    }

    private void ResetTimer(object sender, RoutedEventArgs e) => AppContext.ResetTimerInterval();

    private void ResetProvider(object sender, RoutedEventArgs e) => AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadProvider);

    private void DanmakuOpacityChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var value = (float)sender.ToNotNull<Slider>().Value;

        if (AppContext.DanmakuCanvas != null!)
            AppContext.DanmakuCanvas.Opacity = value;
    }

    private void DanmakuFontChanged(object sender, SelectionChangedEventArgs e) => AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadFormat);
}
