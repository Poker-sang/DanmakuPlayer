using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DanmakuPlayer.Services;

public class RemoteService : IAsyncDisposable
{
    public RemoteService(string serverUrl)
    {
        Current = this;
        _serverUrl = serverUrl;
    }

    [MemberNotNullWhen(true, nameof(Current))]
    public static bool IsCurrentConnected => Current?.IsConnected is true;

    public static RemoteService? Current { get; private set; }

    private readonly ClientWebSocket _webSocket = new();

    private const int ReceiveBufferSize = 4096;

    private CancellationTokenSource _cts = new();

    private readonly string _serverUrl;

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    public event EventHandler<RemoteStatus>? MessageReceived;

    public bool IsConnected => _webSocket?.State is WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken token = default)
    {
        try
        {
            await _webSocket.ConnectAsync(new(_serverUrl), token);
            Connected?.Invoke(this, EventArgs.Empty);
            _ = ReceiveStatusAsync(_cts.Token);
        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (IsConnected)
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Service disconnecting",
                    CancellationToken.None);
                await _cts.CancelAsync();
                _cts.Dispose();
                _webSocket.Dispose();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        Disconnected?.Invoke(this, EventArgs.Empty);
        _cts = new();
    }

    public async Task SendStatusAsync(RemoteStatus status, CancellationToken token = default)
    {
        if (!IsConnected)
            return;

        try
        {
            var buffer = JsonSerializer.SerializeToUtf8Bytes(status);
            await _webSocket.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                true,
                token);
        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }

    private async Task ReceiveStatusAsync(CancellationToken token = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

        try
        {
            while (IsConnected && !token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(buffer, token);

                if (result.MessageType is WebSocketMessageType.Close)
                    return;

                var status = JsonSerializer.Deserialize<RemoteStatus>(new ReadOnlySpan<byte>(buffer, 0, result.Count));

                MessageReceived?.Invoke(this, status);
            }
        }
        catch (WebSocketException ex)
        {
            throw;
        }
    }
}

public struct RemoteStatus()
{
    public bool IsPlaying { get; set; }

    public DateTime CurrentTime { get; set; }

    public TimeSpan VideoTime { get; set; }

    public TimeSpan DanmakuDelayTime { get; set; }

    public double PlaybackRate { get; set; }

    public Dictionary<string, string> ChangedValues { get; set; } = [];
}
