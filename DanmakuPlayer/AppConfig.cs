using System.Collections.Generic;
using System.Text.Json.Serialization;
using DanmakuPlayer.Resources;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[GenerateConstructor(CallParameterlessConstructor = true)]
public partial record AppConfig()
{
    #region 渲染设置

    /// <summary>
    /// 提前渲染并存储，会占用更高内存
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool RenderBefore { get; set; } = false;

    #endregion

    #region 应用设置

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 2]</remarks>
    public int Theme { get; set; }

    /// <summary>
    /// 固定最前
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool TopMost { get; set; } = false;

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
    public double PlaybackRate { get; set; } = 1;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>default: 25 ∈ [5, 100]</remarks>
    public int PlayFramePerSecond { get; set; } = 25;

    #endregion

    #region 弹幕设置

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>default: 15 ∈ [5, 20]</remarks>
    public int DanmakuDuration { get; set; } = 15;

    /// <inheritdoc cref="DanmakuDuration"/>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>), typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public int DanmakuActualDurationMs => (int)(DanmakuDuration * PlaybackRate * 1000);

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>default: 0.6 ∈ [0.2, 1]</remarks>
    public float DanmakuOpacity { get; set; } = 0.6f;

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: <see cref="ConstantStrings.DefaultFont"/></remarks>
    public string DanmakuFont { get; set; } = ConstantStrings.DefaultFont;

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>default: 1 ∈ [0.5, 2]</remarks>
    public float DanmakuScale { get; set; } = 1;

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableOverlap { get; set; } = true;

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
    /// <remarks>default: 0xFF000000</remarks>
    public uint DanmakuStrokeColor { get; set; } = 0xFF000000;

    /// <summary>
    /// 转换大会员彩色弹幕
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool DanmakuDisableColorful { get; set; } = false;

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

    /// <summary>
    /// B站Cookie
    /// </summary>
    /// <remarks>default: []</remarks>
    public Dictionary<string, string> DanmakuCookie { get; set; } = [];

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

    #endregion

    #region 正则设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuEnableRegex { get; set; } = true;

    /// <summary>
    /// 正则表达式集合
    /// </summary>
    /// <remarks>default: []</remarks>
    [AttributeIgnore(typeof(SettingsViewModelAttribute<>))]
    public string[] RegexPatterns { get; set; } = [];

    #endregion

    #endregion

    #region 网页设置

    /// <summary>
    /// 使用WebView2
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool EnableWebView2 { get; set; } = true;

    /// <summary>
    /// 锁定WebView2
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool LockWebView2 { get; set; } = true;

    /// <summary>
    /// 全屏时自动清理视频样式
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool ClearStyleWhenFullScreen { get; set; } = true;

    #endregion
}

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class SettingsSerializeContext : JsonSerializerContext;
