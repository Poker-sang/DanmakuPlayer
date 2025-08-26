using System;
using System.Collections.Generic;

namespace DanmakuPlayer.Models.Remote;

public record RoomInfo
{
    public string Name { get; set; } = "";

    public string Id { get; set; } = "";

    public IReadOnlyList<UserInfo> Users { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 是否是默认房间（用于UI显示“创建房间”选项）
    /// </summary>
    public bool IsDefault { get; set; }
}
