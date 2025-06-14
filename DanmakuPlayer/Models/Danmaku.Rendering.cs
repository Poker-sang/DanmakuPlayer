using System;
using System.Collections.Generic;
using System.Numerics;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
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
                if (!BottomUpStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDurationMs + TimeMs, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
                    return false;
                _showPositionX = (float)(provider.ViewWidth - _layoutWidth) / 2;
                break;
            case DanmakuMode.Top:
                if (!TopDownStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuActualDurationMs + TimeMs, provider.AppConfig.DanmakuEnableOverlap, OverlapPredicate))
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
                if (danmaku.TimeMs + provider.AppConfig.DanmakuActualDurationMs < TimeMs)
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

    public void OnRender(CanvasDrawingSession renderTarget, CreatorProvider provider, int timeMs)
    {
        if (!NeedRender)
            return;

        var ms = timeMs - TimeMs;

        if (Mode is DanmakuMode.Advanced)
        {
            if (!(0 <= ms) || !(ms < AdvancedInfo!.DurationMs))
                return;

            CanvasDrawingSession drawingSession;
            CanvasCommandList? commandList = null;
            if (AdvancedInfo.YFlip is 0)
                drawingSession = renderTarget;
            else
            {
                commandList = new(renderTarget);
                drawingSession = commandList.CreateDrawingSession();
            }

            var aPos = AdvancedInfo.GetPosition(ms, (float) provider.ViewWidth, (float) provider.ViewHeight);
            var aOpacity = AdvancedInfo.GetOpacity(ms);
            using var aBrush = new CanvasSolidColorBrush(drawingSession, Color.GetColor(aOpacity));
            using var aFormat = new CanvasTextFormat();
            aFormat.FontFamily = AdvancedInfo.Font;
            aFormat.FontSize = Size;
            using var aLayout = new CanvasTextLayout(drawingSession, AdvancedInfo.Text, aFormat, int.MaxValue,
                int.MaxValue);

            var lastTransform = drawingSession.Transform;

            if (AdvancedInfo.ZFlip is not 0)
                drawingSession.Transform = Matrix3x2.CreateRotation(AdvancedInfo.ZFlip, aPos);

            drawingSession.DrawTextLayout(aLayout, aPos, aBrush);

            if (AdvancedInfo.Outline)
            {
                using var aGeometry = CanvasGeometry.CreateText(aLayout);
                using var aOutlineBrush = new CanvasSolidColorBrush(drawingSession,
                    provider.AppConfig.DanmakuStrokeColor.GetColor(aOpacity / 2));
                drawingSession.DrawGeometry(aGeometry, aPos, aOutlineBrush, provider.AppConfig.DanmakuStrokeWidth);
            }

            drawingSession.Transform = lastTransform;

            // Y轴翻转需使用3D变换
            if (commandList is not null)
            {
                // reference: https://github.com/cotaku/DanmakuFrostMaster/blob/main/DanmakuFrostMaster/DanmakuRender.cs
                using var effect = new Transform3DEffect();
                var rotation = Matrix4x4.CreateRotationY(AdvancedInfo.YFlip, new(aPos, 0));
                rotation.M14 = (float) -(1 / aLayout.LayoutBounds.Width * Math.Sin(AdvancedInfo.YFlip)); // Perspective transform
                rotation *= Matrix4x4.CreateTranslation((float) (aLayout.LayoutBounds.Width / 2), (float)
                    (aLayout.LayoutBounds.Height / 2), 0); // Move back to original position
                effect.TransformMatrix = rotation;
                effect.Source = commandList;
                renderTarget.DrawImage(effect);
                drawingSession.Dispose();
                commandList.Dispose();
            }

            return;
        }

        if (!(0 <= ms) || !(ms < provider.AppConfig.DanmakuActualDurationMs))
            return;

        var layout = provider.Layouts[ToString()];
        var width = layout.LayoutBounds.Width;
        var brush = provider.GetBrush(Color, provider.AppConfig.DanmakuOpacity);

        var pos = Mode switch
        {
            DanmakuMode.Roll => new((float)(provider.ViewWidth - ((provider.ViewWidth + width) * ms / provider.AppConfig.DanmakuActualDurationMs)), _showPositionY),
            DanmakuMode.Bottom or DanmakuMode.Top => new(_showPositionX, _showPositionY),
            DanmakuMode.Inverse => new((float)(((provider.ViewWidth + width) * ms / provider.AppConfig.DanmakuActualDurationMs) - width), _showPositionY),
            _ => ThrowHelper.ArgumentOutOfRange<DanmakuMode, Vector2>(Mode)
        };
        renderTarget.DrawTextLayout(layout, pos, brush);

        if (provider.AppConfig.DanmakuEnableStrokes)
        {
            var geometry = provider.Geometries[ToString()];
            ICanvasBrush outlineBrush = !provider.AppConfig.DanmakuDisableColorful && Colorful
                ? provider.GetColorfulBrush(pos, width, provider.AppConfig.DanmakuOpacity / 2)
                : provider.GetBrush(provider.AppConfig.DanmakuStrokeColor, provider.AppConfig.DanmakuOpacity / 2);
            renderTarget.DrawGeometry(geometry, pos, outlineBrush, provider.AppConfig.DanmakuStrokeWidth);
        }
    }
}
