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
    public Dictionary<string, CanvasCachedGeometry> Strokes { get; } = [];

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, CanvasCachedGeometry> Fills { get; } = [];

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
        foreach (var fill in Fills)
            fill.Value.Dispose();
        foreach (var stroke in Strokes)
            stroke.Value.Dispose();
        FillsCounter.Clear();
        Fills.Clear();
        Strokes.Clear();
    }

    #region 计数器

    /// <summary>
    /// 内容和对应渲染布局的引用计数
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, int> FillsCounter { get; } = [];

    public void AddFillRef(Danmaku danmaku, CanvasTextLayout? textLayout = null)
    {
        var danmakuString = danmaku.ToString();
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(FillsCounter, danmakuString, out var exists);
        if (!exists)
        {
            using var newLayout = textLayout ?? GetNewLayout(danmaku);
            using var newGeometry = CanvasGeometry.CreateText(newLayout);
            Fills.Add(danmakuString, CanvasCachedGeometry.CreateFill(newGeometry));
            if (AppConfig.DanmakuEnableStrokes)
                Strokes.Add(danmakuString, CanvasCachedGeometry.CreateStroke(newGeometry, AppConfig.DanmakuStrokeWidth));
        }
        ++count;
    }

    public void ClearLayoutRefCount()
    {
        foreach (var counter in FillsCounter)
            FillsCounter[counter.Key] = 0;
    }

    public void ClearUnusedLayoutRef()
    {
        var list = FillsCounter.Where(pair => pair.Value < 1).Select(pair => pair.Key);

        foreach (var danmakuString in list)
        {
            Fills[danmakuString].Dispose();
            Strokes[danmakuString].Dispose();
            _ = Fills.Remove(danmakuString);
            _ = Strokes.Remove(danmakuString);
            _ = FillsCounter.Remove(danmakuString);
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

    public CanvasSolidColorBrush GetBrush(uint color, float alpha = 1)
    {
        if (!Brushes.TryGetValue(color, out var value))
            Brushes[color] = value = new(Creator, color.GetColor((byte)(0xFF * alpha)));
        return value;
    }

    #endregion
}
