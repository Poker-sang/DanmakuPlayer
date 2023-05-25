using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[GenerateConstructor]
public partial record AppConfig
{
    #region 应用设置

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 2]</remarks>
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
    /// <remarks>default: 5 ∈ [1, 20]</remarks>
    public int PlayFastForward { get; set; } = 5;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    /// <remarks>default: 1 ∈ [0.5, 2]</remarks>
    public double PlaySpeed { get; set; } = 1;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>default: 25 ∈ [5, 100]</remarks>
    public int PlayFramePerSecond { get; set; } = 25;

    #endregion

    #region 渲染设置

    /// <summary>
    /// 提前渲染并存储，会占用更高内存
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool RenderBefore { get; set; } = false;

    #endregion

    #region 弹幕设置

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>default: 15 ∈ [5, 20]</remarks>
    public int DanmakuDuration { get; set; } = 15;

    /// <inheritdoc cref="DanmakuDuration"/>
    /// 方便计算使用float
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public float DanmakuActualDuration => (float)(DanmakuDuration * PlaySpeed);

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>default: 0.7 ∈ [0.1, 1]</remarks>
    public float DanmakuOpacity { get; set; } = 0.7f;

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: "微软雅黑"</remarks>
    public string DanmakuFont { get; set; } = "微软雅黑";

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>default: 1 ∈ [0.5, 2]</remarks>
    public float DanmakuScale { get; set; } = 1;

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableOverlap { get; set; }

    #region 描边设置

    /// <summary>
    /// 弹幕是否允许描边
    /// </summary>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuEnableStrokes => DanmakuStrokeWidth is not 0;

    /// <summary>
    /// 弹幕描边宽度
    /// </summary>
    /// <remarks>default: 1 ∈ [0, 5]（0时禁用）</remarks>
    public int DanmakuStrokeWidth { get; set; } = 1;

    /// <summary>
    /// 弹幕描边颜色
    /// </summary>
    /// <remarks>default: 0xFFA9A9A9</remarks>
    public uint DanmakuStrokeColor { get; set; } = 0xFFA9A9A9;

    #endregion

    #region 数量设置

    /// <summary>
    /// 是否限制滚动弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountRollEnableLimit => DanmakuCountRollLimit is not -1;

    /// <summary>
    /// 限制滚动弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    public int DanmakuCountRollLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制底端弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountBottomEnableLimit => DanmakuCountBottomLimit is not -1;

    /// <summary>
    /// 限制底端弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    public int DanmakuCountBottomLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制顶端弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountTopEnableLimit => DanmakuCountTopLimit is not -1;

    /// <summary>
    /// 限制顶端弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    public int DanmakuCountTopLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制逆向弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountInverseEnableLimit => DanmakuCountInverseLimit is not -1;

    /// <summary>
    /// 限制逆向弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    public int DanmakuCountInverseLimit { get; set; } = -1;

    /// <summary>
    /// 允许M7高级弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuCountM7Enable { get; set; } = true;

    #endregion

    #region 合并设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableMerge { get; set; } = true;

    public int DanmakuMergeMaxCosine { get; set; } = 6;

    public int DanmakuMergeMaxDistance { get; set; } = 5;

    public int DanmakuMergeTimeSpan { get; set; } = 20;

    public bool DanmakuMergeCrossMode { get; set; } = false;

    // TODO: 优化合并弹幕代表性算法
    public int DanmakuMergeRepresentativePercent { get; set; } = 50;

    #endregion

    #region 正则设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableRegex { get; set; } = true;

    /// <summary>
    /// 正则表达式集合，用json序列化
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public string RegexPatterns { get; set; } = "[]";

    #endregion

    #endregion

    public AppConfig()
    {

    }
}
