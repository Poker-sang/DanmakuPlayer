using System.Collections.Generic;
using DanmakuRoomRollList = System.Collections.Generic.LinkedList<(float BottomPos, DanmakuPlayer.Models.IDanmakuWidth Danmaku)>;
using DanmakuRoomStaticList = System.Collections.Generic.LinkedList<(float BottomPos, int TimeMs)>;

namespace DanmakuPlayer.Models;

/// <summary>
/// 用来统一各弹幕的上下文
/// </summary>
public class DanmakuContext
{
    /// <summary>
    /// <list type="bullet">
    /// <item>
    /// <term>BottomPos</term>
    /// <description>从前一个结点的BottomPos（表头前一个算作0）到本结点的BottomPos的区间，算作一个空间（连续的）</description>
    /// </item>
    /// <item>
    /// <term>Time</term>
    /// <description>等该空间空余时，进度条的时间</description>
    /// </item>
    /// </list>
    /// </summary>
    /// ReSharper disable once UnusedMember.Local
#pragma warning disable IDE0052
    private const bool DocProvider = false;
#pragma warning restore IDE0052

    public DanmakuContext(float viewHeight, AppConfig appConfig)
    {
        _ = StaticRoom.AddFirst((viewHeight, 0));
        _ = RollRoom.AddFirst((viewHeight, new DanmakuWidth(-appConfig.DanmakuActualDurationMs)));
        _ = InverseRoom.AddFirst((viewHeight, new DanmakuWidth(-appConfig.DanmakuActualDurationMs)));
        if (appConfig.DanmakuCountRollEnableLimit)
            Roll = new(appConfig.DanmakuCountRollLimit);
        if (appConfig.DanmakuCountBottomEnableLimit)
            Bottom = new(appConfig.DanmakuCountBottomLimit);
        if (appConfig.DanmakuCountTopEnableLimit)
            Top = new(appConfig.DanmakuCountTopLimit);
        if (appConfig.DanmakuCountInverseEnableLimit)
            Inverse = new(appConfig.DanmakuCountInverseLimit);
    }

    /// <summary>
    /// 滚动弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomRollList RollRoom { get; } = [];

    /// <summary>
    /// 静止弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomStaticList StaticRoom { get; } = [];

    /// <summary>
    /// 滚动弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomRollList InverseRoom { get; } = [];

    /// <summary>
    /// 正在顶部的弹幕
    /// </summary>
    public Queue<Danmaku>? Top { get; }

    /// <summary>
    /// 正在底部的弹幕
    /// </summary>
    public Queue<Danmaku>? Bottom { get; }

    /// <summary>
    /// 正在滚动的弹幕
    /// </summary>
    public Queue<Danmaku>? Roll { get; }

    /// <summary>
    /// 正在逆向滚动的弹幕
    /// </summary>
    public Queue<Danmaku>? Inverse { get; }
}
