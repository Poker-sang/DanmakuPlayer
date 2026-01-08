using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.UI;
using DanmakuPlayer.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DanmakuPlayer.Services;

public partial class CreatorProvider(ICanvasAnimatedControl creator) : IDisposable
{
    public ICanvasResourceCreator Creator { get; } = creator;

#pragma warning disable CA1822 // 将成员标记为 static
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822 // 将成员标记为 static

    public double ViewWidth { get; } = creator.Size.Width;

    public double ViewHeight { get; } = creator.Size.Height;

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
    public Dictionary<string, CanvasTextLayout> Layouts { get; } = [];

    /// <summary>
    /// 渲染布局描边
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/>、<see cref="Layouts"/></remarks>
    public Dictionary<string, CanvasGeometry> Geometries { get; } = [];

    public CanvasLinearGradientBrush? ColorfulBrush { get; private set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var brush in Brushes)
            brush.Value.Dispose();
        Brushes.Clear();
        ColorfulBrush?.Dispose();
        ColorfulBrush = null;

        ClearLayouts();
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
        foreach (var geometry in Geometries)
            geometry.Value.Dispose();
        LayoutsCounter.Clear();
        Layouts.Clear();
        Geometries.Clear();
    }

    #region 计数器

    /// <summary>
    /// 内容和对应渲染布局的引用计数
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, int> LayoutsCounter { get; } = [];

    public void AddLayoutRef(Danmaku danmaku)
    {
        var danmakuString = danmaku.ToString();
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(LayoutsCounter, danmakuString, out var exists);
        if (!exists)
        {
            var newLayout = Layouts[danmakuString] = GetNewLayout(danmaku);
            if (AppConfig.DanmakuEnableStrokes)
                Geometries[danmakuString] = CanvasGeometry.CreateText(newLayout);
        }
        ++count;
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

    public CanvasSolidColorBrush GetBrush(uint argbColor, float alpha)
    {
        if (!Brushes.TryGetValue(argbColor, out var value))
            Brushes[argbColor] = value = new(Creator, argbColor.GetColor(alpha));
        return value;
    }

    public CanvasLinearGradientBrush GetColorfulBrush(Vector2 position, double width, float alpha)
    {
        var brush = ColorfulBrush ??=
            // B站网页使用的颜色
            new(Creator, Color.FromArgb(0xFF, 0xF2, 0x50, 0x9E), Color.FromArgb(0xFF, 0x30, 0x8B, 0xCD))
            {
                Opacity = alpha
            };
        brush.StartPoint = position;
        brush.EndPoint = position + new Vector2((float)width, 0f);
        return brush;
    }

    #endregion
}
