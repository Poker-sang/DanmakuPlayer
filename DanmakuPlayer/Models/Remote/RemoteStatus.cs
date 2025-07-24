using System;
using System.Collections.Generic;

namespace DanmakuPlayer.Models.Remote;

public struct RemoteStatus()
{
    public bool IsPlaying { get; set; }

    public DateTime CurrentTime { get; set; }

    public TimeSpan VideoTime { get; set; }

    public TimeSpan DanmakuDelayTime { get; set; }

    public double PlaybackRate { get; set; }

    public Dictionary<string, string> ChangedValues { get; set; } = [];
}
