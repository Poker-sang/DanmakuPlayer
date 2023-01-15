using System;

namespace DanmakuPlayer.Enums;

[Flags]
public enum RenderType
{
    None = 0,
    RenderInit = 1 << 0,
    RenderOnce = 1 << 1,
    RenderAlways = 1 << 2,
    ReloadProvider = 1 << 3,
    ReloadFormat = 1 << 4,
}
