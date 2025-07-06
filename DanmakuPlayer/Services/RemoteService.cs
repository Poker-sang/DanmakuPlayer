using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Animations;

namespace DanmakuPlayer.Services;

public class RemoteService
{

    private readonly ClientWebSocket _webSocket;

    private readonly string _serverUrl;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<RemoteStatus>? MessageReceived;
    private const int ReceiveBufferSize = 4096;
    private CancellationTokenSource _cts = new();

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    protected RemoteService(string serverUrl)
    {
        _serverUrl = serverUrl;
        _webSocket = new ClientWebSocket();
    }

    private async Task HandleConnection()
    {
        while (_webSocket.State == WebSocketState.Open)
        {
            await ReceiveStatusAsync(_cts.Token);
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        try
        {
            await _webSocket.ConnectAsync(new Uri(_serverUrl), ct);
            Connected?.Invoke(this, EventArgs.Empty);
            await HandleConnection();
        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _cts.CancelAsync();
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Service disconnecting",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        Disconnected?.Invoke(this, EventArgs.Empty);
        _cts = new CancellationTokenSource();
    }

    public async Task SendStatusAsync(RemoteStatus status)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(status));
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }

    private async Task ReceiveStatusAsync(CancellationToken ct)
    {
        var buffer = new byte[ReceiveBufferSize];

        try
        {
            while (IsConnected && !ct.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var status = JsonSerializer.Deserialize<RemoteStatus>(message);

                // Process message
                MessageReceived?.Invoke(this, status);
            }
        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }
}

public struct RemoteStatus
{
    public bool IsPlaying { get; set; }

    public TimeSpan CurrentTime { get; set; }

    public TimeSpan VideoTime { get; set; }

    public TimeSpan DanmakuDelayTime { get; set; }

    public string WebUri { get; set; }

    public double VideoDuration { get; set; }
    
    public ulong DanmakuCId { get; set; }

    public double PlaybackRate { get; set; }
}
