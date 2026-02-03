using System;
using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DanmakuPlayer.Services;

public partial class SubtitleCreatorProvider(ICanvasAnimatedControl creator) : IDisposable
{
    public ICanvasResourceCreator Creator { get; } = creator;

#pragma warning disable CA1822 // 将成员标记为 static
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822 // 将成员标记为 static

    public float ViewWidth { get; } = (float) creator.Size.Width;

    public float ViewHeight { get; } = (float) creator.Size.Height;

    public const float RenderPadding = 50;

    /// <summary>
    /// 颜色和对应笔刷
    /// </summary>
    public Dictionary<string, uint> Color { get; } = [];

    /// <summary>
    /// 颜色和对应笔刷
    /// </summary>
    public Dictionary<uint, CanvasSolidColorBrush> Brushes { get; } = [];

    /// <summary>
    /// Style和对应字体格式
    /// </summary>
    public Dictionary<string, CanvasTextFormat> Formats { get; } = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var brush in Brushes)
            brush.Value.Dispose();
        foreach (var format in Formats)
            format.Value.Dispose();
        Color.Clear();
        Brushes.Clear();
        Formats.Clear();
    }

    public CanvasSolidColorBrush GetBrush(string style) => Brushes[Color[style]];

    public void SetBrushColor(string style, System.Drawing.Color color)
    {
        var argbColor = color.GetAlphaUInt();
        Color.TryAdd(style, argbColor);
        if (!Brushes.ContainsKey(argbColor))
            Brushes[argbColor] = new(Creator, argbColor.GetAlphaColor());
    }
}
