using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class RemoteDialog : UserControl
{
    [GeneratedDependencyProperty]
    public partial bool IsConnected { get; set; }

    [GeneratedDependencyProperty]
    public partial int ConnectedCount { get; set; }

    public RemoteDialog()
    {
        InitializeComponent();
        ServerTextBlock.Text = AppContext.AppConfig.SyncUrl;
    }

    private BackgroundPanel _backgroundPanel = null!;

    public async Task ShowAsync(BackgroundPanel backgroundPanel)
    {
        _backgroundPanel = backgroundPanel;
        _ = await Content.To<ContentDialog>().ShowAsync();
    }

    private async void CreateRoom_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private async void JoinRoom_OnClick(object sender, RoutedEventArgs e)
    {
        ProgressRing.IsActive = true;
        try
        {
            if (RemoteService.IsCurrentConnected)
                await RemoteService.Current.DisposeAsync();
            var client = new RemoteService(ServerTextBlock.Text);
            client.MessageReceived += _backgroundPanel.OnMessageReceived;
            client.Disconnected += Disconnected;
            await client.ConnectAsync(RoomIdTextBlock.Text);
            IsConnected = client.IsConnected;
            AppContext.AppConfig.SyncUrl = ServerTextBlock.Text;
            AppContext.SaveConfiguration(AppContext.AppConfig);
        }
        catch (Exception ex)
        {
            _backgroundPanel.InfoBarService.Error(Emoticon.Shocked + " " + MainPanelResources.ExceptionThrown, ex.Message);
        }
        finally
        {
            ProgressRing.IsActive = false;
        }
    }

    private async void Disconnect_OnClick(object sender, RoutedEventArgs e)
    {
        ProgressRing.IsActive = true;
        try
        {
            if (RemoteService.IsCurrentConnected)
                await RemoteService.Current.DisposeAsync();
        }
        catch (Exception ex)
        {
            _backgroundPanel.InfoBarService.Error(Emoticon.Shocked + " " + MainPanelResources.ExceptionThrown, ex.Message);
        }
        finally
        {
            ProgressRing.IsActive = false;
        }
    }

    private void Disconnected(object? sender, EventArgs e)
    {
        IsConnected = false;
        ConnectedCount = 0;
    }
}
