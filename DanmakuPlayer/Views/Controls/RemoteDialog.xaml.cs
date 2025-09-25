using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial IList<RoomInfo> Rooms { get; private set; } = [GetDefaultRoomInfo()];

    public RemoteDialog()
    {
        InitializeComponent();
        ServerTextBlock.Text = AppContext.AppConfig.ServerUrl;     
    }

    private BackgroundPanel _backgroundPanel = null!;

    public async Task ShowAsync(BackgroundPanel backgroundPanel)
    {
        _backgroundPanel = backgroundPanel;

        if (ServerTextBlock.Text is not "")
            _ = RefreshRoomsAsync();

        _ = await Content.To<ContentDialog>().ShowAsync();
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

    private async void Disconnected(object? sender, Exception? e)
    {
        IsConnected = false;
        ConnectedCount = 0;
        await RefreshRoomsAsync();
    }

    private async void RefreshRooms_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRoomsAsync();
    }

    private async Task RefreshRoomsAsync()
    {
        var client = RemoteService.Current ?? new RemoteService(ServerTextBlock.Text);
        var rooms = await client.GetRoomsAsync();
        if (RemoteService.Current is not { IsConnected: true })
            rooms.Insert(0, GetDefaultRoomInfo());
        Rooms = rooms;
    }

    private static RoomInfo GetDefaultRoomInfo() => new()
    {
        IsDefault = true,
        Name = string.Format(RemoteDialogResources.CreateRoomNameFormatted, Environment.UserName),
        CreatedAt = DateTimeOffset.Now
    };

    private async Task CreateRoomAsync(string name)
    {
        ProgressRing.IsActive = true;
        try
        {
            var serverUrl = ServerTextBlock.Text;
            var client = await GetRemoteServiceAsync(serverUrl);
            client.MessageReceived += _backgroundPanel.OnMessageReceived;
            client.Disconnected += Disconnected;
            await client.CreateRoomAsync(name);
            IsConnected = client.IsConnected;
            SaveConfig(serverUrl);
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

    private async Task JoinRoomAsync(string id)
    {
        ProgressRing.IsActive = true;
        try
        {
            var serverUrl = ServerTextBlock.Text;
            var client = await GetRemoteServiceAsync(serverUrl);
            client.MessageReceived += _backgroundPanel.OnMessageReceived;
            client.Disconnected += Disconnected;
            await client.ConnectAsync(id);
            IsConnected = client.IsConnected;
            SaveConfig(serverUrl);
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

    private static void SaveConfig(string serverUrl)
    {
        AppContext.AppConfig.ServerUrl = serverUrl;
        AppContext.SaveConfiguration(AppContext.AppConfig);
    }

    private async void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (RemoteService.Current is { IsConnected: true })
            return;

        var room = (RoomInfo) args.InvokedItem;
        if (room.IsDefault)
            await CreateRoomAsync(string.Format(RemoteDialogResources.RoomDefaultNameFormatted, Environment.UserName));
        else
            await JoinRoomAsync(room.Id);
        await RefreshRoomsAsync();
    }

    private static async ValueTask<RemoteService> GetRemoteServiceAsync(string serverUrl)
    {
        if (RemoteService.Current is not null)
        {
            if (RemoteService.IsCurrentConnected)
            {
                await RemoteService.Current.DisposeAsync();
                return new(serverUrl);
            }

            return RemoteService.Current;
        }

        return new(serverUrl);
    }
}

internal partial class RoomInfoDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Normal { get; set; }
    public DataTemplate? New { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item is RoomInfo { IsDefault : true } ? New : Normal;
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item is RoomInfo { IsDefault: true } ? New : Normal;
    }
}
