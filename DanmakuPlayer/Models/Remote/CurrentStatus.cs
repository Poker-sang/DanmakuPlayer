namespace DanmakuPlayer.Models.Remote;

public record CurrentStatus
{
    public Message? LastMessageReceived { get; set; }

    public int TotalConnectedClients { get; set; }
}
