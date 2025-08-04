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
/// <param name="Size">字号</param>
/// <param name="Color">颜色</param>
public partial record Danmaku(
    string Text,
    ulong Id,
    int TimeMs,
    DanmakuMode Mode,
    int Size,
    uint Color) : IDanmakuWidth
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
            elem.Color)
        {
            UserHash = elem.MidHash,
            Colorful = elem.Colorful is DmColorfulType.VipGradualColor,
            UnixTimeStamp = (ulong)elem.Ctime,
            Attribute = (DanmakuAttribute)elem.Attr,
            Pool = (DanmakuPool) elem.Pool
        };
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
                uint.Parse(tempInfo[5]))
            {
                UserHash = tempInfo[6],
                UnixTimeStamp = ulong.Parse(tempInfo[4]),
                Pool = Enum.Parse<DanmakuPool>(tempInfo[5])
            }
            : new(
                xElement.Value,
                0,
                (int) (double.Parse(tempInfo[0]) * 1000),
                Enum.Parse<DanmakuMode>(tempInfo[1]),
                int.Parse(tempInfo[2]),
                uint.Parse(tempInfo[3]))
            {
                UserHash = tempInfo[6],
                UnixTimeStamp = ulong.Parse(tempInfo[4]),
                Pool = Enum.Parse<DanmakuPool>(tempInfo[5])
            };
    }

    /// <summary>
    /// 属性位值
    /// </summary>
    public DanmakuAttribute Attribute { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserHash { get; init; } = "";

    /// <summary>
    /// 大会员专属颜色
    /// </summary>
    public bool Colorful { get; init; }

    /// <summary>
    /// 发送时间戳
    /// </summary>
    public ulong UnixTimeStamp { get; init; }

    /// <summary>
    /// 所属弹幕池
    /// </summary>
    public DanmakuPool Pool { get; init; }
}
