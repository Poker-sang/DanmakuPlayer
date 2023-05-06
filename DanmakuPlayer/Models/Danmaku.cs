using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
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
    ulong UnixTimeStamp,
    DanmakuPool Pool,
    string UserHash) : IDanmakuWidth
{
    public double LayoutWidth { get; set; }

    private double _layoutHeight;

    /// <summary>
    /// 静止弹幕显示的位置
    /// </summary>
    private Vector2 _staticPosition;

    /// <summary>
    /// 弹幕显示的Y坐标
    /// </summary>
    private float _showPositionY;

    /// <summary>
    /// 是否需要渲染（取决于是否允许重叠）
    /// </summary>
    public bool NeedRender { get; private set; }

    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <returns>是否需要渲染</returns>
    public bool RenderInit(DanmakuContext context, CreatorProvider provider)
    {
        NeedRender = false;

        // 是否超过弹幕数量限制
        if (Mode switch
        {
            DanmakuMode.Roll => provider.AppConfig.DanmakuCountRollEnableLimit && CountReachLimit(context.Roll, provider.AppConfig.DanmakuCountRollLimit),
            DanmakuMode.Bottom => provider.AppConfig.DanmakuCountBottomEnableLimit && CountReachLimit(context.Bottom, provider.AppConfig.DanmakuCountBottomLimit),
            DanmakuMode.Top => provider.AppConfig.DanmakuCountTopEnableLimit && CountReachLimit(context.Top, provider.AppConfig.DanmakuCountTopLimit),
            DanmakuMode.Inverse => provider.AppConfig.DanmakuCountInverseEnableLimit && CountReachLimit(context.Inverse, provider.AppConfig.DanmakuCountInverseLimit),
            _ => false
        })
            return false;

        var layoutExists = provider.Layouts.TryGetValue(ToString(), out var layout);
        if (!layoutExists)
            layout = provider.GetNewLayout(this);
        LayoutWidth = layout!.LayoutBounds.Width;
        _layoutHeight = layout.LayoutBounds.Height;

        switch (Mode)
        {
            case DanmakuMode.Roll:
                if (!TopDownRollDanmaku(provider, context.RollRoom, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Bottom:
                if (!BottomUpStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(provider.ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Top:
                if (!TopDownStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(provider.ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Inverse:
                if (!TopDownRollDanmaku(provider, context.InverseRoom, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }

        if (!layoutExists)
            provider.Layouts.Add(ToString(), layout);
        switch (Mode)
        {
            case DanmakuMode.Roll:
                context.Roll?.Enqueue(this);
                break;
            case DanmakuMode.Bottom:
                context.Bottom?.Enqueue(this);
                break;
            case DanmakuMode.Top:
                context.Top?.Enqueue(this);
                break;
            case DanmakuMode.Inverse:
                context.Inverse?.Enqueue(this);
                break;
        }
        NeedRender = true;
        return true;

        // 达到限制数返回true，否则false
        bool CountReachLimit(Queue<Danmaku> queue, int limit)
        {
            while (queue.Count is not 0)
            {
                var danmaku = queue.Peek();
                if (danmaku.Time + provider.AppConfig.DanmakuActualDuration < Time)
                    _ = queue.Dequeue();
                else
                    break;
            }

            return queue.Count >= limit;
        }

        // 如果覆盖了，并且不允许覆盖，则返回true，否则false
        // 本函数所有分支都会调用本本地函数
        bool OverlapPredicate(bool overlap)
        {
            // 是否覆盖
            if (!overlap || provider.AppConfig.DanmakuEnableOverlap)
                return false;
            if (!layoutExists)
                layout.Dispose();
            return true;
        }
    }

    public void OnRender(CanvasDrawingSession renderTarget, CreatorProvider provider, float time)
    {
        // 外部实现逻辑：if (Time <= time && time - provider.AppConfig.Speed < Time)
        if (!NeedRender)
            return;

        var layout = provider.Layouts[ToString()];
        var width = layout.LayoutBounds.Width;
        var brush = provider.GetBrush(Color);
        var outlineBrush = (CanvasSolidColorBrush)null!;
        var geometry = (CanvasGeometry)null!;
        if (provider.AppConfig.DanmakuEnableStrokes)
        {
            outlineBrush = provider.GetBrush(provider.AppConfig.DanmakuStrokeColor);
            geometry = CanvasGeometry.CreateText(layout);
        }

        if (Mode is DanmakuMode.Advanced or DanmakuMode.Code or DanmakuMode.Bas)
            return;

        var pos = Mode switch
        {
            DanmakuMode.Roll => new((float)(provider.ViewWidth - ((provider.ViewWidth + width) * (time - Time) / provider.AppConfig.DanmakuActualDuration)), _showPositionY),
            DanmakuMode.Bottom or DanmakuMode.Top => _staticPosition,
            DanmakuMode.Inverse => new((float)(((provider.ViewWidth + width) * (time - Time) / provider.AppConfig.DanmakuActualDuration) - width), _showPositionY),
            _ => ThrowHelper.ArgumentOutOfRange<DanmakuMode, Vector2>(Mode)
        };
        renderTarget.DrawTextLayout(layout, pos, brush);
        if (provider.AppConfig.DanmakuEnableStrokes)
            renderTarget.DrawGeometry(geometry, pos, outlineBrush, provider.AppConfig.DanmakuStrokeWidth);
    }

    public override string ToString() => $"{Text},{Color},{Size}";
}
