using System;
using DanmakuPlayer.Services;

namespace DanmakuPlayer.Enums;

[Flags]
public enum RenderType
{
    /// <summary>
    /// 无操作
    /// </summary>
    None = 0,
    /// <summary>
    /// 渲染一次后停止
    /// </summary>
    RenderOnce = 1 << 0,
    /// <summary>
    /// 一直渲染
    /// </summary>
    RenderAlways = 1 << 1,
    /// <summary>
    /// 渲染初始化
    /// </summary>
    RenderInit = 1 << 2,
    /// <summary>
    /// 重新加载<see cref="CreatorProvider"/>
    /// </summary>
    ReloadProvider = (1 << 3) | RenderInit,
    /// <summary>
    /// 重新加载<see cref="CreatorProvider.Formats"/>
    /// </summary>
    ReloadFormats = (1 << 4) | ReloadProvider,
}
