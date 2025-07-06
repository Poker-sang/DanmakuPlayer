using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class RemoteDialog : UserControl
{
    public RemoteDialog() => InitializeComponent();

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
            // RoomIdTextBlock.Text
            var client = new RemoteService(ServerTextBlock.Text);
            client.MessageReceived += (s, status) => _backgroundPanel.Status = status;
            await client.ConnectAsync();
            IsConnected = client.IsConnected;
        }
        finally
        {
            ProgressRing.IsActive = false;
        }
    }

    private async void Disconnect_OnClick(object sender, RoutedEventArgs e)
    {
        if (RemoteService.IsCurrentConnected)
            await RemoteService.Current.DisposeAsync();
        IsConnected = false;
    }

    [GeneratedDependencyProperty]
    private partial bool IsConnected { get; set; }
}
