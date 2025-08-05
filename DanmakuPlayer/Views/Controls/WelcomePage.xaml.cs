using System;
using CommunityToolkit.WinUI;
using DanmakuPlayer.Resources;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class WelcomePage
{
    public WelcomePage() => InitializeComponent();

    private BackgroundPanel _backgroundPanel = null!;

    private void WelcomePageOnLoading(FrameworkElement sender, object e) => _backgroundPanel = this.FindParent<BackgroundPanel>()!;

    private async void PasteClicked(object sender, RoutedEventArgs e)
    {
        var dataPackageView = Clipboard.GetContent();

        if (!dataPackageView.Contains("AnsiText") && !dataPackageView.Contains("OEMText"))
        {
            _backgroundPanel.InfoBarService.Error(MainPanelResources.IncorrectUrlFormat, Emoticon.Depressed);
            return;
        }

        var url = await dataPackageView.GetTextAsync();

        if (string.IsNullOrEmpty(url))
        {
            _backgroundPanel.InfoBarService.Error(MainPanelResources.IncorrectUrlFormat, Emoticon.Depressed);
            return;
        }

        _backgroundPanel.StatusChanged(nameof(_backgroundPanel.Vm.Url), url);
        await _backgroundPanel.WebView.GotoAsync(url);
    }
}
