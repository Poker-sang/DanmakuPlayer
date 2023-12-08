using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

[INotifyPropertyChanged]
public sealed partial class SettingsDialog : UserControl
{
    [ObservableProperty] private SettingViewModel _vm = null!;

    public SettingsDialog() => InitializeComponent();

    public async Task ShowAsync()
    {
        Vm = new();
        _ = await Content.To<ContentDialog>().ShowAsync();
    }

    /// <summary>
    /// <see cref="Selector.SelectionChanged" />、<see cref="ToggleSwitch.Toggled" />等事件在属性变化前触发，
    /// 所以不能使用这些事件， 而是在保证属性变化后再去操作。此方法在<see cref="SettingsDialog" />关闭时调用
    /// </summary>
    /// <param name="before"></param>
    /// <param name="after"></param>
#pragma warning disable IDE0079 // 请删除不必要的忽略
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
#pragma warning restore IDE0079 // 请删除不必要的忽略
    private void CompareChanges(AppConfig before, AppConfig after)
    {
        var backgroundPanel = Parent.To<BackgroundPanel>();
        if (before.PlaybackRate != after.PlaybackRate)
        {
            DispatcherTimerHelper.ResetTimerInterval();
            backgroundPanel.ResetProvider();
            backgroundPanel.TrySetPlaybackRate();
        }
        else
        {
            if (before.RenderBefore != after.RenderBefore
                || before.DanmakuDuration != after.DanmakuDuration
                || before.DanmakuOpacity != after.DanmakuOpacity
                || before.DanmakuScale != after.DanmakuScale
                || before.DanmakuEnableOverlap != after.DanmakuEnableOverlap
                || before.DanmakuCountRollLimit != after.DanmakuCountRollLimit
                || before.DanmakuCountBottomLimit != after.DanmakuCountBottomLimit
                || before.DanmakuCountTopLimit != after.DanmakuCountTopLimit
                || before.DanmakuCountInverseLimit != after.DanmakuCountInverseLimit
                || before.DanmakuCountM7Enable != after.DanmakuCountM7Enable)
                backgroundPanel.ResetProvider();
            if (before.PlayFramePerSecond != after.PlayFramePerSecond)
                DispatcherTimerHelper.ResetTimerInterval();
        }
        if (before.DanmakuFont != after.DanmakuFont)
            backgroundPanel.DanmakuFontChanged();
        if (before.Foreground != after.Foreground)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.Foreground));
        if (before.EnableWebView2 != after.EnableWebView2)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.EnableWebView2));
        if (before.LockWebView2 != after.LockWebView2)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.LockWebView2));
        if (before.TopMost != after.TopMost)
            backgroundPanel.Vm.TopMost = after.TopMost; // 需要setter中设置OverlappedPresenter.IsAlwaysOnTop，所以不能直接用RaisePropertyChanged
    }

    #region 事件处理

    private void NavigateUriTapped(object sender, TappedRoutedEventArgs e)
    {
        using var process = new Process();
        process.StartInfo = new()
        {
            FileName = sender.GetTag<string>(),
            UseShellExecute = true
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

    private void SetDefaultAppConfigClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        e.Cancel = true;
        Vm.AppConfig = new();
        OnPropertyChanged(nameof(Vm));
    }

    private void AddRegexPattern(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        // TODO: Localization
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

    private void RemoveTapped(object sender, TappedRoutedEventArgs e) =>
        Vm.PatternsCollection.Remove(sender.GetTag<string>());

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var copy = AppContext.AppConfig;
        AppContext.AppConfig = Vm.AppConfig;
        CompareChanges(copy, Vm.AppConfig);
        AppContext.SaveConfiguration(Vm.AppConfig);
    }

    #endregion
}
