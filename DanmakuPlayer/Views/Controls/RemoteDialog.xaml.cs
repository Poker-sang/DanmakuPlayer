using System;
using System.Threading.Tasks;
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
        var host = await RemoteService.CreateRoomAsHostAsync(ServerTextBlock.Text, RoomIdTextBlock.Text);
        _timer.Tick += async (_, _) => await host.UpdateStatusAsync(_backgroundPanel.Status);
        _timer.Start();
    }

    private async void JoinRoom_OnClick(object sender, RoutedEventArgs e)
    {
        var client = await RemoteService.JoinRoomAsClientAsync(ServerTextBlock.Text, RoomIdTextBlock.Text);
        _timer.Tick += async (_, _) => _backgroundPanel.Status = await client.FetchStatusAsync();
        _timer.Start();
    }

    private void Disconnect_OnClick(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        RefreshTimer();
    }

    private DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    private void RefreshTimer()
    {
        _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    }
}
