using System;
using System.Xml.Linq;
using Bilibili.Community.Service.Dm.V1;
using DanmakuPlayer.Enums;

namespace DanmakuPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Text">内容</param>
/// <param name="Id">Used to sort danmaku with the same StartMs</param>
/// <param name="TimeMs">出现时间</param>
/// <param name="Mode">模式</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="UnixTimeStamp">发送时间戳</param>
/// <param name="Pool">所属弹幕池</param>
/// <param name="UserHash">用户ID</param>
public partial record Danmaku(
    string Text,
    ulong Id,
    int TimeMs,
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
            (ulong)elem.Id,
            elem.Progress,
            (DanmakuMode)elem.Mode,
            elem.Fontsize,
            elem.Color,
            elem.Colorful is DmColorfulType.VipGradualColor,
            (ulong)elem.Ctime,
            (DanmakuPool)elem.Pool,
            elem.MidHash);
    }

    public static Danmaku Parse(XElement xElement, bool isNewFormat)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        return isNewFormat
            ? new(
                xElement.Value,
                ulong.Parse(tempInfo[0]),
                int.Parse(tempInfo[2]),
                Enum.Parse<DanmakuMode>(tempInfo[3]),
                int.Parse(tempInfo[4]),
                uint.Parse(tempInfo[5]),
                false,
                ulong.Parse(tempInfo[4]),
                Enum.Parse<DanmakuPool>(tempInfo[5]),
                tempInfo[6])
            : new(
                xElement.Value,
                0,
                (int) (double.Parse(tempInfo[0]) * 1000),
                Enum.Parse<DanmakuMode>(tempInfo[1]),
                int.Parse(tempInfo[2]),
                uint.Parse(tempInfo[3]),
                false,
                ulong.Parse(tempInfo[4]),
                Enum.Parse<DanmakuPool>(tempInfo[5]),
                tempInfo[6]);
    }
}
