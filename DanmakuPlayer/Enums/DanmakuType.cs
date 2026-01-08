using System;

namespace DanmakuPlayer.Enums;

/// <summary>
/// 弹幕类型
/// </summary>
public enum DanmakuMode
{
    /// <summary>
    /// 1-3 普通滚动弹幕
    /// </summary>
    Roll = 1,

    /// <summary>
    /// 底端弹幕
    /// </summary>
    Bottom = 4,

    /// <summary>
    /// 顶端弹幕
    /// </summary>
    Top = 5,

    /// <summary>
    /// 逆向弹幕
    /// </summary>
    Inverse = 6,

    /// <summary>
    /// 高级弹幕
    /// </summary>
    Advanced = 7,

    /// <summary>
    /// Flash 代码弹幕（已弃用）
    /// </summary>
    Code = 8,

    /// <summary>
    /// Bilibili Animation Script（必须为<see cref="DanmakuPool.Advanced"/>）
    /// </summary>
    Bas = 9
}
