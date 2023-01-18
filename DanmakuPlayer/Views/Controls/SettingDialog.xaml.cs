using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

[INotifyPropertyChanged]
public sealed partial class SettingDialog : UserControl
{
    [ObservableProperty] private SettingViewModel _vm = new();

    public SettingDialog() => InitializeComponent();

    public async Task ShowAsync() => await Content.To<ContentDialog>().ShowAsync();

    #region 事件处理

    private void NavigateUriClick(object sender, RoutedEventArgs e)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = sender.GetTag<string>(),
                UseShellExecute = true
            }
        };
        _ = process.Start();
    }

    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        var value = sender.To<RadioButtons>().SelectedIndex;

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

    private void ForegroundColorChanged(ColorPicker sender, ColorChangedEventArgs e) => Parent.To<BackgroundPanel>().RaiseForegroundChanged();

    private void ResetTimer(object sender, RoutedEventArgs e) => AppContext.ResetTimerInterval();

    private void ResetProvider(object sender, RoutedEventArgs e) => AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadProvider);

    private void DanmakuOpacityChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var value = (float)sender.To<Slider>().Value;

        if (AppContext.DanmakuCanvas != null!)
            AppContext.DanmakuCanvas.Opacity = value;
    }

    private void DanmakuFontChanged(object sender, SelectionChangedEventArgs e) => AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadFormats);

    private void SetDefaultAppConfigClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        e.Cancel = true;
        AppContext.SetDefaultAppConfig();
        OnPropertyChanged(nameof(Vm));
    }

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => AppContext.SaveConfiguration(_vm.AppConfig);

    #endregion
}
