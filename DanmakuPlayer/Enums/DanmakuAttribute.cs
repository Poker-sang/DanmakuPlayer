using System;

namespace DanmakuPlayer.Enums;

/// <summary>
/// 弹幕属性位值
/// </summary>
[Flags]
public enum DanmakuAttribute
{
    /// <summary>
    /// 保护弹幕
    /// </summary>
    Protect = 0,

    /// <summary>
    /// 直播弹幕
    /// </summary>
    FromLive = 0b1,

    /// <summary>
    /// 高赞弹幕
    /// </summary>
    HighLike = 0b10
}
