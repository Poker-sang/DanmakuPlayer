using System;
using System.Collections.Generic;
using AutoSettingsPage;
using AutoSettingsPage.Models;
using AutoSettingsPage.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.Controls;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Windows.System;

namespace DanmakuPlayer.Views.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public IReadOnlyList<ISettingsGroup> LocalGroups { get; }

    public SettingsViewModel(Func<BackgroundPanel> getBackgroundPanel)
    {
        LocalGroups = SettingsBuilder.CreateGroupList(AppContext.AppConfig)
            .NewGroup(SettingsDialogResources.AppHeaderText)
            .Config(group => group
                .Enum(t => t.Theme, _Themes,
                    entry => entry.ValueChanged += OnThemeChanged)
                .Bool(t => t.TopMost,
                    // 需要setter中设置OverlappedPresenter.IsAlwaysOnTop，所以不能直接用RaisePropertyChanged
                    entry => entry.ValueChanged += t => getBackgroundPanel().Vm.TopMost = t))
            .NewGroup(SettingsDialogResources.RenderingHeaderText)
            .Config(group => group
                .Bool(t => t.RenderBefore))
            .NewGroup(SettingsDialogResources.PlayHeaderText)
            .Config(group => group
                .Int(t => t.PlayFastForward, 1, 20, 1)
                .Double(t => t.PlaybackRate, 0.5, 3, 0.25,
                    entry => entry.ValueChanged += t =>
                    {
                        OnValueChanged(t);
                        getBackgroundPanel().Vm.RaisePropertyChanged(nameof(AppConfig.PlaybackRate));
                    })
                .Int(t => t.PlayFramePerSecond, 5, 100, 5,
                    entry => entry.ValueChanged += _ => AppContext.SetTimerInterval())
                .Int(t => t.DanmakuDuration, 5, 20, 1,
                    entry => entry.ValueChanged += OnValueChanged)
                .Double(t => t.DanmakuOpacity, 0.2, 1, 0.2,
                    entry => entry.ValueChanged += OnValueChanged)
                .Font(t => t.DanmakuFont,
                    entry => entry.ValueChanged += _ => Mode |= RenderMode.ReloadFormats)
                .Double(t => t.DanmakuScale, 0.5, 2, 0.25,
                    entry => entry.ValueChanged += OnValueChanged)
                .Bool(t => t.DanmakuDisableColorful)
                .Int(t => t.DanmakuStrokeWidth, 0, 5, 1)
                .Color(t => t.DanmakuStrokeColor)
                .Cookies(t => t.DanmakuCookie,
                    entry => entry.ValueChanged += _ => HttpClientHelper.ShouldRefreshHeader = true))
            .NewGroup(SettingsDialogResources.DanmakuFiltrationHeaderText)
            .Config(group => group
                .Bool(t => t.DanmakuEnableOverlap,
                    entry => entry.ValueChanged += OnValueChanged)
                .MultiValues(t => t.DanmakuCountRollEnableLimit, entry => entry
                    .Int(t => t.DanmakuCountRollLimit, -1, 100, 5,
                        e => e.ValueChanged += OnValueChanged)
                    .Int(t => t.DanmakuCountBottomLimit, -1, 500, 5,
                        e => e.ValueChanged += OnValueChanged)
                    .Int(t => t.DanmakuCountTopLimit, -1, 500, 5,
                        e => e.ValueChanged += OnValueChanged)
                    .Int(t => t.DanmakuCountInverseLimit, -1, 500, 5,
                        e => e.ValueChanged += OnValueChanged)
                    .Bool(t => t.DanmakuCountM7Enable,
                        e => e.ValueChanged += OnValueChanged)
                    .Bool(t => t.DanmakuCountSubtitleEnable,
                        e => e.ValueChanged += OnValueChanged))
                .MultiValuesWithSwitch(t => t.DanmakuEnableRegex, entry => entry
                    .Regex(t => t.RegexPatterns))
                .MultiValuesWithSwitch(t => t.DanmakuEnableMerge, entry => entry
                    .Int(t => t.DanmakuMergeMaxEditDistance, 0, 10, 1)
                    .Int(t => t.DanmakuMergeMaxCosineDistance, 0, 10, 1)
                    .Int(t => t.DanmakuMergeTimeSpan, 0, 60, 5)
                    .Bool(t => t.DanmakuMergeCrossMode)))
            .NewGroup(SettingsDialogResources.WebView2HeaderText)
            .Config(group => group
                .Bool(t => t.EnableWebView2,
                    entry => entry.ValueChanged += _ => getBackgroundPanel().Vm.RaisePropertyChanged(nameof(AppConfig.EnableWebView2)))
                .Bool(t => t.LockWebView2,
                    entry => entry.ValueChanged += _ => getBackgroundPanel().Vm.RaisePropertyChanged(nameof(AppConfig.LockWebView2)))
                .Bool(t => t.ClearStyleWhenFullScreen))
            .NewGroup(SettingsDialogResources.AboutHeaderText)
            .Config(group => group
                .MultiValues(ConstantStrings.AppTitle, ConstantStrings.AppVersion, Symbol.Info, entry => entry
                    .Clickable(ConstantStrings.StoreUri, SettingsDialogResources.StoreCardHeader, ConstantStrings.StoreUri, Symbol.StoreMicrosoft, NavigateUriTapped)
                    .Clickable(ConstantStrings.MailToUri, SettingsDialogResources.MailCardHeader, ConstantStrings.Mail, Symbol.Mail, NavigateUriTapped)
                    .Clickable(ConstantStrings.AuthorUri, SettingsDialogResources.AuthorCardHeader, ConstantStrings.Author, Symbol.PersonCircle, NavigateUriTapped)
                    .Clickable(ConstantStrings.QqUri, SettingsDialogResources.QqCardHeader, ConstantStrings.QqUin, Symbol.Communication, NavigateUriTapped)
                    .Clickable(ConstantStrings.RepositoryUri, SettingsDialogResources.RepositoryCardHeader, ConstantStrings.AppName, Symbol.Box, NavigateUriTapped))
                .Clickable(ConstantStrings.LicenseUri, SettingsDialogResources.LicenseCardHeader, SettingsDialogResources.LicenseCardDescription, Symbol.CheckmarkStarburst, NavigateUriTapped))
            .Build();
    }

    private static void OnThemeChanged(object value)
    {
        if (App.Window.Content is FrameworkElement rootElement && value is ElementTheme theme)
            rootElement.RequestedTheme = theme;
    }

    public RenderMode Mode = RenderMode.None;

    private void OnValueChanged<T>(T any) => Mode |= RenderMode.ReloadProvider;

    private async void NavigateUriTapped(ClickableSettingsEntry sender, EventArgs eventArgs)
    {
        _ = await Launcher.LaunchUriAsync(new(sender.Token));
    }

    private static readonly IReadOnlyList<EnumStringPair<object>> _Themes =
    [
        new(ElementTheme.Default, SettingsDialogResources.RadioButtonSystemThemeContent),
        new(ElementTheme.Light, SettingsDialogResources.RadioButtonLightThemeContent),
        new(ElementTheme.Dark, SettingsDialogResources.RadioButtonDarkThemeContent)
    ];
}
