using System;

namespace DanmakuPlayer.Models.Remote;

public record LoginInfo
(
    string UserName,
    DateTimeOffset Time,
    CurrentStatus Current
);
