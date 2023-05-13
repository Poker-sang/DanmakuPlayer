using System;
using System.Collections.Generic;
using System.Linq;
using DanmakuPlayer.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DanmakuPlayer.Services;

public class CreatorProvider : IDisposable
{
    public CreatorProvider(CanvasControl creator, AppConfig appConfig)
    {
        Creator = creator;
        AppConfig = appConfig;
        ViewWidth = creator.ActualWidth;
        ViewHeight = creator.ActualHeight;
    }

    public ICanvasResourceCreator Creator { get; }

    public AppConfig AppConfig { get; }

    public double ViewWidth { get; }

    public double ViewHeight { get; }

    /// <summary>
    /// 颜色和对应笔刷
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/></remarks>
    public Dictionary<uint, CanvasSolidColorBrush> Brushes { get; } = new();

    /// <summary>
    /// 字号和对应字体格式
    /// </summary>
    /// <remarks>依赖于<see cref="DanmakuPlayer.AppConfig.DanmakuFont"/></remarks>
    public static Dictionary<float, CanvasTextFormat> Formats { get; } = new();

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, CanvasTextLayout> Layouts { get; } = new();

    /// <summary>
    /// 渲染布局描边
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/>、<see cref="Layouts"/></remarks>
    public Dictionary<string, CanvasGeometry> Geometries { get; } = new();

    #region 计数器

    /// <summary>
    /// 内容和对应渲染布局的引用计数
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, int> LayoutsCounter { get; } = new();

    public void AddLayoutRef(Danmaku danmaku)
    {
        var danmakuString = danmaku.ToString();
        if (!LayoutsCounter.ContainsKey(danmakuString))
        {
            LayoutsCounter[danmakuString] = 0;
            Layouts[danmakuString] = GetNewLayout(danmaku);
            if (AppConfig.DanmakuEnableStrokes)
                Geometries[danmakuString] = CanvasGeometry.CreateText(Layouts[danmakuString]);
        }
        ++LayoutsCounter[danmakuString];
    }

    public void ClearLayoutRefCount()
    {
        foreach (var counter in LayoutsCounter)
            LayoutsCounter[counter.Key] = 0;
    }

    public void ClearUnusedLayoutRef()
    {
        var list = LayoutsCounter.Where(pair => pair.Value < 1).Select(pair => pair.Key);

        foreach (var danmakuString in list)
        {
            Layouts[danmakuString].Dispose();
            if (Geometries.TryGetValue(danmakuString, out var geometry))
            {
                geometry.Dispose();
                _ = Layouts.Remove(danmakuString);
            }
            _ = Geometries.Remove(danmakuString);
            _ = LayoutsCounter.Remove(danmakuString);
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
        foreach (var geometry in Geometries)
            geometry.Value.Dispose();
        LayoutsCounter.Clear();
        Layouts.Clear();
        Geometries.Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var brush in Brushes)
            brush.Value.Dispose();
        Brushes.Clear();

        ClearLayouts();
    }
}
