using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.Controls;

[INotifyPropertyChanged]
[DependencyProperty<bool>("TopMost")]
public sealed partial class SettingsDialog : UserControl
{
    [ObservableProperty] private SettingViewModel _vm = new();

    public SettingsDialog() => InitializeComponent();

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

    private void ResetTimer(object sender, RoutedEventArgs e) => DispatcherTimerHelper.ResetTimerInterval();

    private void ResetProvider(object sender, RoutedEventArgs e) => AppContext.BackgroundPanel.ReloadDanmaku(RenderType.ReloadProvider);

    private void ResetTimerAndProvider(object sender, RoutedEventArgs e)
    {
        ResetTimer(sender, e);
        ResetProvider(sender, e);
    }

    private void DanmakuFontChanged(object sender, SelectionChangedEventArgs e) => AppContext.BackgroundPanel.ReloadDanmaku(RenderType.ReloadFormats);

    private void SetDefaultAppConfigClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        e.Cancel = true;
        AppContext.SetDefaultAppConfig();
        OnPropertyChanged(nameof(Vm));
    }

    private void AddRegexPattern(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        if (string.IsNullOrEmpty(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = "正则表达式不能为空";
            RegexErrorInfoBar.IsOpen = true;
            return;
        }
        if (Vm.PatternsCollection.Contains(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = "与已有正则表达式重复";
            RegexErrorInfoBar.IsOpen = true;
            return;
        }
        try
        {
            _ = new Regex(sender.Text);
        }
        catch (RegexParseException ex)
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Error;
            RegexErrorInfoBar.Message = ex.Message;
            RegexErrorInfoBar.IsOpen = true;
            return;
        }
        RegexErrorInfoBar.IsOpen = false;
        Vm.PatternsCollection.Add(sender.Text);
    }

    private void RegexPatternChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        try
        {
            _ = new Regex(sender.Text);
        }
        catch (RegexParseException)
        {
            ErrorBorder.BorderBrush = new SolidColorBrush(Colors.Red);
            return;
        }
        ErrorBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
    }

    private void RemoveTapped(object sender, TappedRoutedEventArgs e) => Vm.PatternsCollection.Remove(sender.GetTag<string>());

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => AppContext.SaveConfiguration(Vm.AppConfig);

    #endregion
}
