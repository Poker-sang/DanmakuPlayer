namespace DanmakuPlayer.Models.Remote;

public record Message(string Type, string Data);
public static class MessageTypes
{
    // 有用户登录时向所有客户端广播
    public const string Login = "login";

    // 用户改变状态时广播给其他客户端
    public const string StatusUpdate = "status_update";

    // 要求客户端发送当前状态到服务器
    public const string SendCurrentStatus = "send_current_status";
}
