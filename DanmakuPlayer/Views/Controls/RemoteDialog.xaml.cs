using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DanmakuPlayer.Models.Remote;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

[ObservableObject]
public sealed partial class RemoteDialog : UserControl
{
    [ObservableProperty]
    public partial bool IsConnected { get; private set; }

    [ObservableProperty]
    public partial int ConnectedCount { get; set; }

    [ObservableProperty]
    public partial List<RoomInfo>? Rooms { get; private set; }

    public RemoteDialog()
    {
        InitializeComponent();
        ServerTextBlock.Text = AppContext.AppConfig.SyncUrl;     
    }


    private BackgroundPanel _backgroundPanel = null!;

    public async Task ShowAsync(BackgroundPanel backgroundPanel)
    {
        _backgroundPanel = backgroundPanel;


        if (ServerTextBlock.Text is not "")
        {
            _ = RefreshRoomsAsync();
        }

        _ = await Content.To<ContentDialog>().ShowAsync();
    }

    private async void CreateRoom_OnClick(object sender, RoutedEventArgs e)
    {
        ProgressRing.IsActive = true;
        try
        {
            if (RemoteService.IsCurrentConnected)
                await RemoteService.Current.DisposeAsync();
            var client = new RemoteService(ServerTextBlock.Text);
            client.MessageReceived += _backgroundPanel.OnMessageReceived;
            client.Disconnected += Disconnected;
            await client.CreateRoomAsync($"{Environment.UserName}的房间");
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

    private async void Disconnected(object? sender, EventArgs e)
    {
        IsConnected = false;
        ConnectedCount = 0;
        await RefreshRoomsAsync();
    }

    private async void RefreshRooms_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRoomsAsync();
    }

    private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (RemoteService.Current is not null && RemoteService.Current.IsConnected) return;
        var room = (RoomInfo)e.ClickedItem;
        await JoinRoomAsync(room.Id!);
    }

    private async Task RefreshRoomsAsync()
    {
        RemoteService client;
        client = RemoteService.Current ?? new RemoteService(ServerTextBlock.Text);
        Rooms = await client.GetRoomsAsync();
    }

    private async Task JoinRoomAsync(string id)
    {
        ProgressRing.IsActive = true;
        try
        {
            RemoteService client;
            if (RemoteService.Current is not null)
            {
                if (RemoteService.IsCurrentConnected)
                {
                    await RemoteService.Current.DisposeAsync();                  
                    client = new RemoteService(ServerTextBlock.Text);
                }
                else
                {
                    client = RemoteService.Current;
                }
            }
            else
            {
                client = new RemoteService(ServerTextBlock.Text);
            }

            client.MessageReceived += _backgroundPanel.OnMessageReceived;
            client.Disconnected += Disconnected;
            await client.ConnectAsync(id);
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
}
