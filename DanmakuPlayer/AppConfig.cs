using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AutoSettingsPage;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Resources;
using FluentIcons.Common;
using Microsoft.UI.Xaml;
using WinUI3Utilities.Attributes;
using static DanmakuPlayer.SettingsDialogResources;

namespace DanmakuPlayer;

[GenerateConstructor(CallParameterlessConstructor = true)]
public partial record AppConfig()
{
    #region 应用设置

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 2]</remarks>
    [SettingsEntry(Symbol.PaintBrush, nameof(AppThemeExpanderHeader), nameof(AppThemeExpanderDescription))]
    public ElementTheme Theme { get; set; }

    /// <summary>
    /// 固定最前
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    [SettingsEntry(Symbol.Pin, nameof(TopMostCardHeader), nameof(TopMostCardDescription))]
    public bool TopMost { get; set; } = false;

    #endregion

    #region 播放设置

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    /// <remarks>default: 5 ∈ [1, 20]</remarks>
    [SettingsEntry(Symbol.FastForward, nameof(PlayFastForwardCardHeader), nameof(PlayFastForwardCardDescription))]
    public int PlayFastForward { get; set; } = 5;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    /// <remarks>default: 1 ∈ [0.5, 3]</remarks>
    [SettingsEntry(Symbol.Multiplier2x, nameof(PlaybackRateCardHeader), nameof(PlaybackRateCardDescription))]
    public double PlaybackRate { get; set; } = 1;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>default: 30 ∈ [5, 120]</remarks>
    [SettingsEntry(Symbol.Fps30, nameof(PlayFramePerSecondCardHeader), nameof(PlayFramePerSecondCardDescription))]
    public int PlayFramePerSecond { get; set; } = 30;

    #endregion

    #region 弹幕设置

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>default: 15 ∈ [5, 20]</remarks>
    [SettingsEntry(Symbol.TopSpeed, nameof(DanmakuDurationCardHeader), nameof(DanmakuDurationCardDescription))]
    public int DanmakuDuration { get; set; } = 15;

    /// <inheritdoc cref="DanmakuDuration"/>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public int DanmakuActualDurationMs => (int)(DanmakuDuration * PlaybackRate * 1000);

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>default: 0.6 ∈ [0.2, 1]</remarks>
    [SettingsEntry(Symbol.TransparencySquare, nameof(DanmakuOpacityCardHeader), nameof(DanmakuOpacityCardDescription))]
    public double DanmakuOpacity { get; set; } = 0.6f;

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: <see cref="ConstantStrings.DefaultFont"/></remarks>
    [SettingsEntry(Symbol.TextFont, nameof(DanmakuFontCardHeader), nameof(DanmakuFontCardDescription))]
    public string DanmakuFont { get; set; } = ConstantStrings.DefaultFont;

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>default: 1 ∈ [0.5, 2]</remarks>
    [SettingsEntry(Symbol.TextFontSize, nameof(DanmakuScaleCardHeader), nameof(DanmakuScaleCardDescription))]
    public double DanmakuScale { get; set; } = 1;

    #region 描边设置

    /// <summary>
    /// 弹幕是否允许描边
    /// </summary>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuEnableStrokes => DanmakuStrokeWidth is not 0;

    /// <summary>
    /// 转换大会员彩色弹幕
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    [SettingsEntry(Symbol.Premium, nameof(DanmakuDisableColorfulCardHeader), nameof(DanmakuDisableColorfulCardDescription))]
    public bool DanmakuDisableColorful { get; set; }

    /// <summary>
    /// 弹幕描边宽度
    /// </summary>
    /// <remarks>default: 1 ∈ [0, 5]（0时禁用）</remarks>
    [SettingsEntry(Symbol.TextColor, nameof(DanmakuStrokeExpanderHeader), nameof(DanmakuStrokeExpanderDescription))]
    public int DanmakuStrokeWidth { get; set; } = 1;

    /// <summary>
    /// 弹幕描边颜色
    /// </summary>
    /// <remarks>default: 0xFF000000</remarks>
    [SettingsEntry(Symbol.Eyedropper, nameof(DanmakuStrokeColorCardHeader), nameof(DanmakuStrokeColorCardDescription))]
    public uint DanmakuStrokeColor { get; set; } = 0xFF000000;

    /// <summary>
    /// B站Cookie
    /// </summary>
    /// <remarks>default: []</remarks>
    [SettingsEntry(Symbol.Cookies, nameof(DanmakuCookieExpanderHeader), nameof(DanmakuCookieExpanderDescription))]
    public Dictionary<string, string> DanmakuCookie { get; set; } = [];

    #endregion

    #region 数量设置

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.Stack, nameof(DanmakuEnableOverlapCardHeader), nameof(DanmakuEnableOverlapCardDescription))]
    public bool DanmakuEnableOverlap { get; set; } = true;

    /// <summary>
    /// 是否限制滚动弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    // SettingsEntry仅显示设置项用
    [SettingsEntry(Symbol.TextWordCount, nameof(DanmakuCountLimitExpanderHeader), nameof(DanmakuCountLimitExpanderDescription))]
    public bool DanmakuCountRollEnableLimit => DanmakuCountRollLimit is not -1;

    /// <summary>
    /// 限制滚动弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    [SettingsEntry(Symbol.TextBoxAlignTopRight, nameof(DanmakuCountRollLimitCardHeader),  nameof(DanmakuCountRollLimitCardDescription))]
    public int DanmakuCountRollLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制底端弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountBottomEnableLimit => DanmakuCountBottomLimit is not -1;

    /// <summary>
    /// 限制底端弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    [SettingsEntry(Symbol.TextBoxAlignBottom, nameof(DanmakuCountBottomLimitCardHeader),  nameof(DanmakuCountBottomLimitCardDescription))]
    public int DanmakuCountBottomLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制顶端弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountTopEnableLimit => DanmakuCountTopLimit is not -1;

    /// <summary>
    /// 限制顶端弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    [SettingsEntry(Symbol.TextBoxAlignTop, nameof(DanmakuCountTopLimitCardHeader), nameof(DanmakuCountTopLimitCardDescription))]
    public int DanmakuCountTopLimit { get; set; } = -1;

    /// <summary>
    /// 是否限制逆向弹幕数量
    /// </summary>
    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public bool DanmakuCountInverseEnableLimit => DanmakuCountInverseLimit is not -1;

    /// <summary>
    /// 限制逆向弹幕的数量
    /// </summary>
    /// <remarks>default: -1 ∈ [-1, 100]（-1时禁用）</remarks>
    [SettingsEntry(Symbol.TextBoxAlignTopLeft, nameof(DanmakuCountInverseLimitCardHeader), nameof(DanmakuCountInverseLimitCardDescription))]
    public int DanmakuCountInverseLimit { get; set; } = -1;

    /// <summary>
    /// 允许M7高级弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.SlideTextSparkle, nameof(DanmakuCountM7EnableCardHeader), null)]
    public bool DanmakuCountM7Enable { get; set; } = true;

    /// <summary>
    /// 允许字幕弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.Subtitles, nameof(DanmakuCountSubtitleEnableCardHeader), null)]
    public bool DanmakuCountSubtitleEnable { get; set; } = true;

    #endregion

    #region 合并设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.Merge, nameof(DanmakuMergeExpanderHeader), nameof(DanmakuMergeExpanderDescription))]
    public bool DanmakuEnableMerge { get; set; } = true;

    /// <summary>
    /// 弹幕合并最大余弦距离
    /// </summary>
    /// <remarks>default: 5 ∈ [0, 10]</remarks>
    [SettingsEntry(Symbol.AutoFitWidth, nameof(DanmakuMergeMaxEditDistanceCardHeader), nameof(DanmakuMergeMaxEditDistanceCardDescription))]
    public int DanmakuMergeMaxCosineDistance { get; set; } = 6;

    /// <summary>
    /// 弹幕合并最大编辑距离
    /// </summary>
    /// <remarks>default: 6 ∈ [0, 10]</remarks>
    [SettingsEntry(Symbol.DataBarHorizontal, nameof(DanmakuMergeMaxCosineDistanceCardHeader), nameof(DanmakuMergeMaxCosineDistanceCardDescription))]
    public int DanmakuMergeMaxEditDistance { get; set; } = 5;

    /// <summary>
    /// 弹幕合并时间间隔（秒）
    /// </summary>
    /// <remarks>default: 20 ∈ [0, 60]</remarks>
    [SettingsEntry(Symbol.Timer10, nameof(DanmakuMergeTimeSpanCardHeader), nameof(DanmakuMergeTimeSpanCardDescription))]
    public int DanmakuMergeTimeSpan { get; set; } = 20;

    /// <summary>
    /// 弹幕合并跨类型合并
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    [SettingsEntry(Symbol.ArrowMoveInward, nameof(DanmakuMergeCrossModeCardHeader), nameof(DanmakuMergeCrossModeCardDescription))]
    public bool DanmakuMergeCrossMode { get; set; }

    // 文本处理参数
    /// <summary>
    /// 移除结尾符号
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeTrimEnding { get; set; } = true;

    /// <summary>
    /// 整理空格
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeTrimSpace { get; set; } = true;

    /// <summary>
    /// 全角转半角
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeTrimWidth { get; set; } = true;

    // 过滤选项
    /// <summary>
    /// 处理字幕弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeProcessSubtitle { get; set; } = true;

    /// <summary>
    /// 处理特殊弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeProcessAdvanced { get; set; } = true;

    /// <summary>
    /// 处理底部弹幕
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeProcessBottom { get; set; } = true;

    /// <summary>
    /// 拼音谐音识别
    /// </summary>
    public bool DanmakuMergeEnablePinYin { get; set; } = true;

    // 显示选项
    /// <summary>
    /// 代表弹幕选择百分比
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 100]</remarks>
    public int DanmakuMergeRepresentativePercent { get; set; }

    /// <summary>
    /// 启用模式提升
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool DanmakuMergeModeElevation { get; set; }

    /// <summary>
    /// 启用字号放大
    /// </summary>
    /// <remarks>default: <see langword="false"/></remarks>
    public bool DanmakuMergeEnlarge { get; set; }

    // 文本标记
    /// <summary>
    /// 合并弹幕标记样式
    /// </summary>
    /// <remarks>default: <see cref="DanmakuMergeMarkStyle.Suffix"/></remarks>
    public DanmakuMergeMarkStyle DanmakuMergeMarkStyle { get; set; } = DanmakuMergeMarkStyle.Suffix;

    /// <summary>
    /// 使用下标数字
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuMergeUseSubscript { get; set; } = true;

    /// <summary>
    /// 标记阈值
    /// </summary>
    /// <remarks>default: 5 ∈ [1, 100]</remarks>
    public int DanmakuMergeMarkThreshold { get; set; } = 5;

    // 密度管理
    /// <summary>
    /// 缩小阈值
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 150]（0时禁用）</remarks>
    public int DanmakuMergeShrinkThreshold { get; set; }

    /// <summary>
    /// 丢弃阈值
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 150]（0时禁用）</remarks>
    public int DanmakuMergeDropThreshold { get; set; }

    /// <summary>
    /// 滚动转换阈值（屏幕宽度）
    /// </summary>
    /// <remarks>default: 0 ∈ [0, 5000]（0时禁用）</remarks>
    public int DanmakuMergeScrollThreshold { get; set; }

    #endregion

    #region 正则设置

    /// <summary>
    /// 弹幕合并
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.Filter, nameof(DanmakuRegexExpanderHeader), nameof(DanmakuRegexExpanderDescription))]
    public bool DanmakuEnableRegex { get; set; } = true;

    /// <summary>
    /// 正则表达式集合
    /// </summary>
    /// <remarks>default: []</remarks>
    [SettingsEntry(default, null, null, nameof(AddRegexPatternAutoSuggestBoxPlaceholderText))]
    public ObservableCollection<string> RegexPatterns
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            Regexes = [.. field.Select(p => new Regex(p, RegexOptions.Compiled))];
        }
    } = [];

    [AttributeIgnore(typeof(GenerateConstructorAttribute), typeof(AppContextAttribute<>))]
    public IReadOnlyList<Regex> Regexes { get; private set; } = [];

    #endregion

    #endregion

    #region 网页设置

    /// <summary>
    /// 使用WebView2
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.Globe, nameof(EnableWebView2CardHeader), nameof(EnableWebView2CardDescription))]
    public bool EnableWebView2 { get; set; } = true;

    /// <summary>
    /// 锁定WebView2
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.CursorHoverOff, nameof(LockWebView2CardHeader), nameof(LockWebView2CardDescription))]
    public bool LockWebView2 { get; set; } = true;

    /// <summary>
    /// 全屏时自动清理视频样式
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    [SettingsEntry(Symbol.ArrowMaximize, nameof(ClearStyleWhenFullScreenCardHeader), nameof(ClearStyleWhenFullScreenCardDescription))]
    public bool ClearStyleWhenFullScreen { get; set; } = true;

    #endregion

    #region 同步设置

    /// <summary>
    /// 同步服务器地址
    /// </summary>
    /// <remarks>default: ""</remarks>
    public string ServerUrl { get; set; } = "";

    #endregion
}

[JsonSerializable(typeof(ObservableCollection<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class SettingsSerializeContext : JsonSerializerContext;
