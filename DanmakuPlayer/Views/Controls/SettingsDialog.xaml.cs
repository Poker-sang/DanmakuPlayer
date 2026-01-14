using System;
using System.Threading.Tasks;
using AutoSettingsPage.WinUI;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class SettingsDialog
{
    private SettingsViewModel _viewModel = null!;

    public SettingsDialog() => InitializeComponent();

    private void SettingsDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = new(() => Parent.To<BackgroundPanel>());

        var style = Resources["SettingHeaderStyle"] as Style;
        var first = true;
        foreach (var group in _viewModel.LocalGroups)
        {
            var textBlock = new TextBlock
            {
                Style = style,
                Text = group.Header
            };
            SettingsPanel.Children.Add(textBlock);
            if (first)
            {
                textBlock.Margin = new(1, 0, 0, 4);
                first = false;
            }
            foreach (var entry in group)
                SettingsPanel.Children.Add(SettingsEntryHelper.GetControl(entry));
        }
    }

    public async Task ShowAsync() => await Content.To<ContentDialog>().ShowAsync();

    private void ResetDefaultAppConfigClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        e.Cancel = true;
        var config = new AppConfig();
        foreach (var localGroup in _viewModel.LocalGroups)
            foreach (var settingsEntry in localGroup)
                settingsEntry.LocalValueReset(config);
    }

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        foreach (var localGroup in _viewModel.LocalGroups)
            foreach (var settingsEntry in localGroup)
                settingsEntry.LocalValueSaving(AppContext.LocalConfig);

        if (_viewModel.Mode is not RenderMode.None)
            Parent.To<BackgroundPanel>().ReloadDanmaku(_viewModel.Mode);
    }
}
