using System;
using System.Collections.Generic;

namespace DanmakuPlayer.Models.Remote;

public record RoomInfo
{
    public string? Name { get; set; }
    public string? Id { get; set; }
    public List<UserInfo>? Users { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
