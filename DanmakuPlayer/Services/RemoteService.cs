using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;
using DanmakuPlayer.Models.Remote;

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

    public event EventHandler<Message>? MessageReceived;

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
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Service disconnecting",
                    combinedCts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _webSocket.Abort();
            }
            finally
            {
                _cts.Dispose();
                _webSocket.Dispose();
            }
        }

        Disconnected?.Invoke(this, EventArgs.Empty);
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

                var status = JsonSerializer.Deserialize<Message>(new ReadOnlySpan<byte>(buffer, 0, result.Count));

                MessageReceived?.Invoke(this, status!);
            }
        }
        catch (WebSocketException ex)
        {
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
