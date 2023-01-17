using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[GenerateConstructor]
public partial record AppConfig
{
    #region 应用设置

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0</remarks>
    public int Theme { get; set; }

    /// <summary>
    /// 前景色
    /// </summary>
    /// <remarks>default: 0xFFA9A9A9</remarks>
    public uint Foreground { get; set; } = 0xFFA9A9A9;

    #endregion

    #region 播放设置

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    /// <remarks>default: 5</remarks>
    public int PlayFastForward { get; set; } = 5;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    /// <remarks>default: 1</remarks>
    public double PlaySpeed { get; set; } = 1;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>PlayFramePerSecond ∈ [5, 100], default: 25</remarks>
    public int PlayFramePerSecond { get; set; } = 25;

    #endregion

    #region 弹幕设置

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>DanmakuDuration ∈ [5, 20], default: 15</remarks>
    public int DanmakuDuration { get; set; } = 15;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ [0.1, 1], default: 0.7</remarks>
    public float DanmakuOpacity { get; set; } = 0.7f;

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: "微软雅黑"</remarks>
    public string DanmakuFont { get; set; } = "微软雅黑";

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>DanmakuScale ∈ [0.5, 2], default: 1</remarks>
    public float DanmakuScale { get; set; } = 1;

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuAllowOverlap { get; set; }

    #region 合并设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableMerge { get; set; } = true;

    public int MaxCosine { get; set; } = 6;


    public int MaxDistance { get; set; } = 5;

    public int TimeSpan { get; set; } = 20;

    public bool CrossMode { get; set; } = false;

    public int RepresentativePercent { get; set; } = 50;

    #endregion

    #endregion

    public AppConfig()
    {

    }
}
