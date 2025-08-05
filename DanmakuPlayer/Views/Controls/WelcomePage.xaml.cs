using System;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class WelcomePage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private BackgroundPanel _backgroundPanel = null!;

    private void WelcomePage_Loaded(object sender, RoutedEventArgs e)
    {
        _backgroundPanel = this.FindParent<BackgroundPanel>()!;
    }

    private async void ImportClicked(object sender, RoutedEventArgs e)
    {
        await _backgroundPanel.ImportDanmakuOnline();
    }

    private async void FileClicked(object sender, RoutedEventArgs e)
    {
        await _backgroundPanel.ImportDanmakuFromFile();
    }

    private async void RemoteClicked(object sender, RoutedEventArgs e)
    {
        await _backgroundPanel.DialogRemote.ShowAsync(_backgroundPanel);
    }

    private async void PasteClicked(object sender, RoutedEventArgs e)
    {
        var dataPackageView = Clipboard.GetContent();

        if (!dataPackageView.Contains("AnsiText") && !dataPackageView.Contains("OEMText"))
        {
            _backgroundPanel.InfoBarService.Error(WelcomePageResources.IncorrectUrlFormat);
            return;
        }
        var url = await dataPackageView.GetTextAsync();

        if (string.IsNullOrEmpty(url) || !url.StartsWith("http"))
        {
            _backgroundPanel.InfoBarService.Error(WelcomePageResources.IncorrectUrlFormat);
            return;
        }

        _backgroundPanel.Vm.Url = url;
        _backgroundPanel.StatusChanged(nameof(_backgroundPanel.Vm.Url), url);
        await _backgroundPanel.WebView.GotoAsync(url);
    }
}
