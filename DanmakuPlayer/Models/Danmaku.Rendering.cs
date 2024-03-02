using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas;
using System.Collections.Generic;
using System.Numerics;
using WinUI3Utilities;

namespace DanmakuPlayer.Models;

public partial record Danmaku
{
    double IDanmakuWidth.LayoutWidth => _layoutWidth;

    private double _layoutHeight;

    private double _layoutWidth;

    /// <summary>
    /// 静止弹幕显示的X坐标
    /// </summary>
    private float _showPositionX;

    /// <summary>
    /// 弹幕显示的Y坐标
    /// </summary>
    private float _showPositionY;

    /// <summary>
    /// 是否需要渲染（取决于是否允许重叠）
    /// </summary>
    public bool NeedRender { get; private set; }

    /// <summary>
    /// 高级弹幕信息
    /// </summary>
    public AdvancedDanmaku? AdvancedInfo { get; private set; }

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
            DanmakuMode.Roll => provider.AppConfig.DanmakuCountRollEnableLimit && CountReachLimit(context.Roll!, provider.AppConfig.DanmakuCountRollLimit),
            DanmakuMode.Bottom => provider.AppConfig.DanmakuCountBottomEnableLimit && CountReachLimit(context.Bottom!, provider.AppConfig.DanmakuCountBottomLimit),
            DanmakuMode.Top => provider.AppConfig.DanmakuCountTopEnableLimit && CountReachLimit(context.Top!, provider.AppConfig.DanmakuCountTopLimit),
            DanmakuMode.Inverse => provider.AppConfig.DanmakuCountInverseEnableLimit && CountReachLimit(context.Inverse!, provider.AppConfig.DanmakuCountInverseLimit),
            DanmakuMode.Advanced => !provider.AppConfig.DanmakuCountM7Enable,
            _ => true
        })
            return false;

        if (Mode is DanmakuMode.Advanced)
        {
            try
            {
                AdvancedInfo = AdvancedDanmaku.Parse(Text);
            }
            catch
            {
                return false;
            }

            NeedRender = true;
            return true;
        }

        var layoutExists = provider.Layouts.TryGetValue(ToString(), out var layout);
        if (!layoutExists)
            layout = provider.GetNewLayout(this);
        _layoutWidth = layout!.LayoutBounds.Width;
        _layoutHeight = layout.LayoutBounds.Height;

        switch (Mode)
        {
            case DanmakuMode.Roll:
                if (!TopDownRollDanmaku(provider, context.RollRoom, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Bottom:
                if (!BottomUpStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDuration + Time, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
                    return false;
                _showPositionX = (float)(provider.ViewWidth - _layoutWidth) / 2;
                break;
            case DanmakuMode.Top:
                if (!TopDownStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDuration + Time, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
                    return false;
                _showPositionX = (float)(provider.ViewWidth - _layoutWidth) / 2;
                break;
            case DanmakuMode.Inverse:
                if (!TopDownRollDanmaku(provider, context.InverseRoom, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
                    return false;
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
        if (!NeedRender)
            return;

        var t = time - Time;

        if (Mode is DanmakuMode.Advanced)
        {
            if (!(0 <= t) || !(t < AdvancedInfo!.Duration))
                return;

            var aPos = AdvancedInfo.GetPosition(t, (float)provider.ViewWidth, (float)provider.ViewHeight);
            var aOpacity = AdvancedInfo.GetOpacity(t);
            using var aBrush = new CanvasSolidColorBrush(renderTarget, Color.GetColor((byte)(aOpacity * 0xFF)));
            using var aFormat = new CanvasTextFormat();
            aFormat.FontFamily = AdvancedInfo.Font;
            aFormat.FontSize = Size;
            using var aLayout = new CanvasTextLayout(renderTarget, AdvancedInfo.Text, aFormat, int.MaxValue, int.MaxValue);

            var lastTransform = renderTarget.Transform;

            if (AdvancedInfo.ZFlip is not 0)
                renderTarget.Transform = Matrix3x2.CreateRotation(AdvancedInfo.ZFlip, aPos);
            // Y轴翻转没有完全实现，Win2D无法实现三维变换效果
            if (AdvancedInfo.YFlip is not 0)
                renderTarget.Transform *= Matrix3x2.CreateSkew(0, AdvancedInfo.YFlip, aPos);

            renderTarget.DrawTextLayout(aLayout, aPos, aBrush);

            if (AdvancedInfo.Outline)
            {
                using var aGeometry = CanvasGeometry.CreateText(aLayout);
                using var aOutlineBrush = new CanvasSolidColorBrush(renderTarget, provider.AppConfig.DanmakuStrokeColor.GetColor((byte)(aOpacity * 0xFF / 2)));
                renderTarget.DrawGeometry(aGeometry, aPos, aOutlineBrush, provider.AppConfig.DanmakuStrokeWidth);
            }
            renderTarget.Transform = lastTransform;

            return;
        }

        if (!(0 <= t) || !(t < provider.AppConfig.DanmakuActualDuration))
            return;

        var layout = provider.Layouts[ToString()];
        var width = layout.LayoutBounds.Width;
        var brush = provider.GetBrush(Color, provider.AppConfig.DanmakuOpacity);
        var outlineBrush = (CanvasSolidColorBrush)null!;
        var geometry = (CanvasGeometry)null!;
        if (provider.AppConfig.DanmakuEnableStrokes)
        {
            outlineBrush = provider.GetBrush(provider.AppConfig.DanmakuStrokeColor, provider.AppConfig.DanmakuOpacity / 2);
            geometry = provider.Geometries[ToString()];
        }

        var pos = Mode switch
        {
            DanmakuMode.Roll => new((float)(provider.ViewWidth - ((provider.ViewWidth + width) * t / provider.AppConfig.DanmakuActualDuration)), _showPositionY),
            DanmakuMode.Bottom or DanmakuMode.Top => new(_showPositionX, _showPositionY),
            DanmakuMode.Inverse => new((float)(((provider.ViewWidth + width) * t / provider.AppConfig.DanmakuActualDuration) - width), _showPositionY),
            _ => ThrowHelper.ArgumentOutOfRange<DanmakuMode, Vector2>(Mode)
        };
        renderTarget.DrawTextLayout(layout, pos, brush);
        if (provider.AppConfig.DanmakuEnableStrokes)
            renderTarget.DrawGeometry(geometry, pos, outlineBrush, provider.AppConfig.DanmakuStrokeWidth);
    }
}
