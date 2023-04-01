using DanmakuRoomRollList = System.Collections.Generic.LinkedList<(float BottomPos, DanmakuPlayer.Models.IDanmakuWidth Danmaku)>;
using DanmakuRoomStaticList = System.Collections.Generic.LinkedList<(float BottomPos, double Time)>;

namespace DanmakuPlayer.Models;

/// <summary>
/// 用来统一各弹幕的上下文
/// </summary>
public class DanmakuContext
{
    public DanmakuContext(float viewHeight, int duration)
    {
        _ = StaticRoom.AddFirst((viewHeight, 0));
        _ = RollRoom.AddFirst((viewHeight, new DanmakuWidth(-duration)));
        _ = InverseRoom.AddFirst((viewHeight, new DanmakuWidth(-duration)));
    }

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
#pragma warning disable IDE0052 // 删除未读的私有成员
    private const bool DocProvider = false;
#pragma warning restore IDE0052 // 删除未读的私有成员

    /// <summary>
    /// 静止弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomStaticList StaticRoom { get; } = new();

    /// <summary>
    /// 滚动弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomRollList RollRoom { get; } = new();

    /// <summary>
    /// 滚动弹幕空间
    /// <inheritdoc cref="DocProvider"/>
    /// </summary>
    public DanmakuRoomRollList InverseRoom { get; } = new();

    /// <summary>
    /// 正在顶部弹幕数
    /// </summary>
    public int StaticTopCount { get; set; }

    /// <summary>
    /// 正在底部弹幕数
    /// </summary>
    public int StaticBottomCount { get; set; }

    /// <summary>
    /// 正在滚动弹幕数
    /// </summary>
    public int RollCount { get; set; }
}
