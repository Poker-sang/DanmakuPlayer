using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ABI.System;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WebUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DanmakuPlayer.Views.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
            this.Loaded += WelcomePage_Loaded;
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
            var url = await dataPackageView.GetTextAsync();
            _backgroundPanel.Vm.Url = url;
            _backgroundPanel.StatusChanged(nameof(_backgroundPanel.Vm.Url), url);
            await _backgroundPanel.WebView.GotoAsync(url);
        }
    }
}
