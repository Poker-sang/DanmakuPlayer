using System;

namespace DanmakuPlayer.Models.Remote;

public record LoginInfo
(
    string UserName,
    string ClientIp,
    DateTimeOffset Time,
    CurrentStatus Current
);
