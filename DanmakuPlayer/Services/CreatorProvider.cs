using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using DanmakuPlayer.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUI3Utilities;

namespace DanmakuPlayer.Services;

public class CreatorProvider(CanvasControl creator, AppConfig appConfig) : IDisposable
{
    public ICanvasResourceCreatorWithDpi Creator { get; } = creator;

    public AppConfig AppConfig { get; } = appConfig;

    public double ViewWidth { get; } = creator.ActualWidth;

    public double ViewHeight { get; } = creator.ActualHeight;

    /// <summary>
    /// 颜色和对应笔刷
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/></remarks>
    public Dictionary<uint, CanvasSolidColorBrush> Brushes { get; } = [];

    /// <summary>
    /// 字号和对应字体格式
    /// </summary>
    /// <remarks>依赖于<see cref="AppConfig.DanmakuFont"/></remarks>
    public static Dictionary<float, CanvasTextFormat> Formats { get; } = [];

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, CanvasRenderTarget[]> RenderTargets { get; } = [];

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<Danmaku, CanvasTextLayout> Layouts { get; } = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var brush in Brushes)
            brush.Value.Dispose();
        Brushes.Clear();
        ClearLayouts();
        ClearRenderTargets();
    }

    public static void DisposeFormats()
    {
        foreach (var format in Formats)
            format.Value.Dispose();
        Formats.Clear();
    }

    public void ClearLayouts()
    {
        foreach (var layout in Layouts)
            layout.Value.Dispose();
        Layouts.Clear();
    }

    public void ClearRenderTargets()
    {
        foreach (var renderTargets in RenderTargets)
            foreach (var renderTarget in renderTargets.Value)
                renderTarget.Dispose();
        RenderTargetsCounter.Clear();
        RenderTargets.Clear();
    }

    #region 计数器

    /// <summary>
    /// 内容和对应渲染布局的引用计数
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, int> RenderTargetsCounter { get; } = [];

    public void AddRenderTargetRef(Danmaku danmaku, CanvasTextLayout? textLayout = null)
    {
        var danmakuString = danmaku.ToString();
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(RenderTargetsCounter, danmakuString, out var exists);
        if (!exists)
        {
            using var newLayout = textLayout ?? GetNewLayout(danmaku);
            var newBrush = GetBrush(danmaku.Color, AppConfig.DanmakuOpacity);
            var newGeometry = null as CanvasGeometry;
            var outlineBrush = null as CanvasSolidColorBrush;
            if (AppConfig.DanmakuEnableStrokes)
            {
                newGeometry = CanvasGeometry.CreateText(newLayout);

                outlineBrush = GetBrush(AppConfig.DanmakuStrokeColor, AppConfig.DanmakuOpacity / 2);
            }

            var context = new Context(newLayout, newBrush, newGeometry, outlineBrush);
            RenderTargets.Add(danmakuString, [.. GetRenderTargets(context, [])]);
            newGeometry?.Dispose();
        }
        ++count;
    }

    private List<CanvasRenderTarget> GetRenderTargets(Context context, List<CanvasRenderTarget> list)
    {
        var maxWidth = Creator.Device.MaximumBitmapSizeInPixels;
        var layoutBoundsWidth = context.Layout.LayoutBounds.Width;
        // Math.Floor
        var total = (int)(layoutBoundsWidth / maxWidth);
        CanvasRenderTarget canvasRenderTarget;
        if (total > list.Count)
            canvasRenderTarget = new(Creator, maxWidth, (float)context.Layout.LayoutBounds.Height);
        else if (total == list.Count)
            canvasRenderTarget = new(Creator, (float)layoutBoundsWidth % maxWidth, (float)context.Layout.LayoutBounds.Height);
        else
            return list;
        using var canvasDrawingSession = canvasRenderTarget.CreateDrawingSession();
        canvasDrawingSession.DrawTextLayout(context.Layout, -maxWidth * list.Count, 0, context.Brush);
        if (context is { Geometry: { } geometry, GeometryBrush: { } geometryBrush })
            canvasDrawingSession.DrawGeometry(geometry, -maxWidth * list.Count, 0, geometryBrush, AppConfig.DanmakuStrokeWidth);
        list.Add(canvasRenderTarget);
        return GetRenderTargets(context, list);
    }

    private record Context(
        CanvasTextLayout Layout,
        CanvasSolidColorBrush Brush,
        CanvasGeometry? Geometry,
        CanvasSolidColorBrush? GeometryBrush);

    public void ClearLayoutRefCount()
    {
        foreach (var counter in RenderTargetsCounter)
            RenderTargetsCounter[counter.Key] = 0;
    }

    public void ClearUnusedLayoutRef()
    {
        var list = RenderTargetsCounter.Where(pair => pair.Value < 1).Select(pair => pair.Key);

        foreach (var danmakuString in list)
        {
            foreach (var renderTarget in RenderTargets[danmakuString])
                renderTarget.Dispose();
            _ = RenderTargets.Remove(danmakuString);
            _ = RenderTargetsCounter.Remove(danmakuString);
        }
    }

    #endregion

    #region Get类方法

    public static CanvasTextFormat GetTextFormat(float size)
    {
        if (!Formats.TryGetValue(size, out var value))
            Formats[size] = value = new()
            {
                FontFamily = AppContext.AppConfig.DanmakuFont,
                FontSize = size
            };
        return value;
    }

    public CanvasTextLayout GetNewLayout(Danmaku danmaku) => new(Creator, danmaku.Text, GetTextFormat(danmaku.Size * AppConfig.DanmakuScale), int.MaxValue, int.MaxValue);

    public CanvasSolidColorBrush GetBrush(uint color, float alpha)
    {
        if (!Brushes.TryGetValue(color, out var value))
            Brushes[color] = value = new(Creator, color.GetColor((byte)(0xFF * alpha)));
        return value;
    }

    #endregion
}
