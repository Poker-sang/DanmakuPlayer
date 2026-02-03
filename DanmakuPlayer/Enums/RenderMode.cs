using System;
using DanmakuPlayer.Services;

namespace DanmakuPlayer.Enums;

[Flags]
public enum RenderMode
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
    /// 字幕渲染初始化
    /// </summary>
    SubtitleRenderInit = 1 << 3,

    /// <summary>
    /// 重新加载<see cref="DanmakuCreatorProvider"/>
    /// </summary>
    ReloadProvider = (1 << 4) | RenderInit,

    /// <summary>
    /// 重新加载<see cref="DanmakuCreatorProvider.Formats"/>
    /// </summary>
    ReloadFormats = (1 << 5) | ReloadProvider
}
