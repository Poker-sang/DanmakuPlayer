using System;
using DanmakuPlayer.Models;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
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
    public Dictionary<int, CanvasSolidColorBrush> Brushes { get; } = new();

    /// <summary>
    /// 字号和对应字体格式
    /// </summary>
    /// <remarks>依赖于<see cref="AppConfig.DanmakuFont"/></remarks>
    public static Dictionary<float, CanvasTextFormat> Formats { get; } = new();

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="Creator"/>、<see cref="Formats"/></remarks>
    public Dictionary<string, CanvasTextLayout> Layouts { get; } = new();

    #region Get类方法

    public static CanvasTextFormat GetTextFormat(float size)
    {
        if (!Formats.TryGetValue(size, out var value))
            Formats[size] = value = new CanvasTextFormat
            {
                FontFamily = AppContext.AppConfig.DanmakuFont,
                FontSize = size
            };
        return value;
    }

    public CanvasTextLayout GetNewLayout(Danmaku danmaku) => new(Creator, danmaku.Text, GetTextFormat(danmaku.Size * AppConfig.DanmakuScale), int.MaxValue, int.MaxValue);

    public unsafe CanvasSolidColorBrush GetBrush(in uint color)
    {
        fixed (uint* ptr = &color)
        {
            var c = (byte*)ptr;
            if (!Brushes.TryGetValue((int)color, out var value))
                Brushes[(int)color] = value = new CanvasSolidColorBrush(Creator,
                    Color.FromArgb(0xFF, c[2], c[1], c[0]));
            return value;
        }
    }

    #endregion

    public static void DisposeFormats()
    {
        foreach (var format in Formats)
            format.Value.Dispose();
        Formats.Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var brush in Brushes)
            brush.Value.Dispose();
        Brushes.Clear();

        foreach (var layout in Layouts)
            layout.Value.Dispose();
        Layouts.Clear();
    }
}
