using System;
using System.Xml.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Resources;
using WinUI3Utilities;

namespace DanmakuPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Text">内容</param>
/// <param name="Time">出现时间</param>
/// <param name="Mode">模式</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="UnixTimeStamp">发送时间戳</param>
/// <param name="Pool">所属弹幕池</param>
/// <param name="UserHash">用户ID</param>
public partial record Danmaku(
    string Text,
    float Time,
    DanmakuMode Mode,
    int Size,
    uint Color,
    bool Colorful,
    ulong UnixTimeStamp,
    DanmakuPool Pool,
    string UserHash) : IDanmakuWidth
{
    public override string ToString() => $"{Text},{Color},{Size}";

    public static Danmaku Parse(DanmakuElem elem)
    {
        return new(
            elem.Content,
            elem.Progress / 1000f,
            (DanmakuMode)elem.Mode,
            elem.Fontsize,
            elem.Color,
            elem.Colorful is DmColorfulType.VipGradualColor,
            (ulong)elem.Ctime,
            (DanmakuPool)elem.Pool,
            elem.midHash);
    }

    public static Danmaku Parse(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        return tempInfo.Length switch
        {
            8 => new(
                xElement.Value,
                float.Parse(tempInfo[0]),
                Enum.Parse<DanmakuMode>(tempInfo[1]),
                int.Parse(tempInfo[2]),
                uint.Parse(tempInfo[3]),
                false,
                ulong.Parse(tempInfo[4]),
                Enum.Parse<DanmakuPool>(tempInfo[5]),
                tempInfo[6]),
            9 => new(
                xElement.Value,
                float.Parse(tempInfo[0]),
                Enum.Parse<DanmakuMode>(tempInfo[1]),
                int.Parse(tempInfo[2]),
                uint.Parse(tempInfo[3]),
                false,
                ulong.Parse(tempInfo[4]),
                Enum.Parse<DanmakuPool>(tempInfo[5]),
                tempInfo[6]),
            _ => ThrowHelper.InvalidCast<Danmaku>()
        };
    }
}
