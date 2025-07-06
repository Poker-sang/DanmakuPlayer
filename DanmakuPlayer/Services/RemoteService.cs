using System;
using System.Threading.Tasks;

namespace DanmakuPlayer.Services;

public class RemoteService
{
    protected RemoteService()
    {
    }

    public static Task<RemoteServiceHost> CreateRoomAsHostAsync(string address, string roomId)
    {
        return Task.FromResult<RemoteServiceHost>(new RemoteServiceHost());
    }

    public static Task<RemoteServiceClient> JoinRoomAsClientAsync(string address, string roomId)
    {
        return Task.FromResult<RemoteServiceClient>(new RemoteServiceClient());
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

public class RemoteServiceHost : RemoteService
{
    public Task UpdateStatusAsync(RemoteStatus status)
    {
        return Task.CompletedTask;
    }
}

public class RemoteServiceClient : RemoteService
{
    public Task<RemoteStatus> FetchStatusAsync()
    {
        return Task.FromResult(new RemoteStatus());
    }
}
