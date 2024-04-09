using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
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
    [ObservableProperty] private SettingsViewModel _vm = null!;

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
        if (before.EnableWebView2 != after.EnableWebView2)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.EnableWebView2));
        if (before.LockWebView2 != after.LockWebView2)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.LockWebView2));
        if (before.PlaybackRate != after.PlaybackRate)
            backgroundPanel.Vm.RaisePropertyChanged(nameof(AppConfig.PlaybackRate));
        if (before.TopMost != after.TopMost)
            // 需要setter中设置OverlappedPresenter.IsAlwaysOnTop，所以不能直接用RaisePropertyChanged
            backgroundPanel.Vm.TopMost = after.TopMost;
    }

    #region 事件处理

    private async void NavigateUriTapped(object sender, TappedRoutedEventArgs e)
    {
        _ = await Launcher.LaunchUriAsync(new(sender.To<FrameworkElement>().GetTag<string>()));
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

        if (App.Window.Content is FrameworkElement rootElement)
            rootElement.RequestedTheme = selectedTheme;
    }

    private void SetDefaultAppConfigClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        e.Cancel = true;
        Vm.ResetDefault();
        OnPropertyChanged(nameof(Vm));
    }

    private void AddRegexPattern(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        if (string.IsNullOrEmpty(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = SettingsDialogResources.RegexCannotBeEmpty;
            RegexErrorInfoBar.IsOpen = true;
            return;
        }

        if (Vm.RegexPatterns.Contains(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = SettingsDialogResources.DuplicatesWithExistingRegex;
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
        Vm.RegexPatterns.Add(sender.Text);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void RegexPatternChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        try
        {
            _ = new Regex(sender.Text);
            sender.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }
        catch (RegexParseException)
        {
            sender.BorderBrush = new SolidColorBrush(Colors.Red);
        }
    }

    private void RemoveTapped(object sender, TappedRoutedEventArgs e) =>
        Vm.RegexPatterns.Remove(sender.To<FrameworkElement>().GetTag<string>());

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        Vm.SaveCollections();
        var copy = AppContext.AppConfig;
        AppContext.AppConfig = Vm.AppConfig;
        CompareChanges(copy, Vm.AppConfig);
        AppContext.SaveConfiguration(Vm.AppConfig);
    }

    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    private async void DanmakuGetCookieFromClipboardAppBarButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var cookieText = await Clipboard.GetContent().GetTextAsync();
        try
        {
            if (JsonSerializer.Deserialize<Cookie[]>(cookieText, _options) is { Length: > 2 } cookie)
            {
                Vm.DanmakuCookie = cookie.ToDictionary(c => c.Name, c => c.Value);
                return;
            }
        }
        catch
        {
            // ignored
        }

        var cookiesDict = new Dictionary<string, string>();
        if (cookieText.Split(';') is { Length: > 2 } cookies)
            foreach (var cookie in cookies)
                if (cookie.Split('=') is [var name, var value])
                    cookiesDict[name.Trim()] = value.Trim();
        if (cookiesDict.Count > 2)
            Vm.DanmakuCookie = cookiesDict;
    }

    private async void DanmakuGetCookieFromWebViewAppBarButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var backgroundPanel = Parent.To<BackgroundPanel>();
        var cookie = await backgroundPanel.WebView.GetBiliCookieAsync();
        Vm.DanmakuCookie = cookie.ToDictionary(c => c.Name, c => c.Value);
    }

    #endregion
}
