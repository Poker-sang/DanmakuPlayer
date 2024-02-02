using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class DanmakuHelper
{
    private static int _renderCount;

    /// <summary>
    /// 弹幕池
    /// </summary>
    public static Danmaku[] Pool { get; set; } = [];

    public static CreatorProvider Current { get; set; } = null!;

    public static RenderMode RenderType { get; set; }

    public static bool IsRendering { get; set; }

    public static void Rendering(CanvasControl sender, CanvasDrawEventArgs e, float time, AppConfig appConfig)
    {
        if ((RenderType & RenderMode.RenderInit) is not 0)
        {
            if ((RenderType & RenderMode.ReloadProvider) is not 0)
            {
                if ((RenderType & RenderMode.ReloadFormats) is not 0)
                {
                    CreatorProvider.DisposeFormats();
                    RenderType &= ~RenderMode.ReloadFormats;
                }

                Current.Dispose();
                Current = new(sender, appConfig);
                RenderType &= ~RenderMode.ReloadProvider;
            }

            var context = new DanmakuContext((float)sender.ActualHeight, appConfig);
            var count = Pool.Count(danmaku => danmaku.RenderInit(context, Current));
            if (appConfig.RenderBefore)
            {
                if (appConfig.DanmakuEnableStrokes)
                    foreach (var (danmakuString, layout) in Current.Layouts)
                        Current.Geometries[danmakuString] = CanvasGeometry.CreateText(layout);
            }
            else
                Current.ClearLayouts();

            _renderCount = count;
            RenderType &= ~RenderMode.RenderInit;
        }

        if ((RenderType & RenderMode.RenderOnce) is not 0 || (RenderType & RenderMode.RenderAlways) is not 0)
        {
            e.DrawingSession.Clear(Colors.Transparent);

            if (appConfig.RenderBefore)
                foreach (var t in DisplayingDanmaku(time, appConfig))
                    t.OnRender(e.DrawingSession, Current, time);
            else
            {
                Current.ClearLayoutRefCount();
                foreach (var t in DisplayingDanmaku(time, appConfig))
                {
                    Current.AddLayoutRef(t);
                    t.OnRender(e.DrawingSession, Current, time);
                }

                Current.ClearUnusedLayoutRef();
            }

            if ((RenderType & RenderMode.RenderOnce) is not 0)
                RenderType &= ~RenderMode.RenderOnce;
        }

        IsRendering = false;
    }

    public static void ClearPool() => Pool = [];

    public static async Task<int> Render(CanvasControl canvas, RenderMode renderType, CancellationToken token)
    {
        RenderType = renderType;
        await WaitForRender(canvas, token);
        return _renderCount;
    }

    private static async Task WaitForRender(CanvasControl canvas, CancellationToken token)
    {
        IsRendering = true;
        canvas.Invalidate();
        while (IsRendering)
            await Task.Delay(500, token);
    }

    public static Danmaku[] DisplayingDanmaku(float time, AppConfig appConfig)
    {
        var actualDuration = Math.Max(10, appConfig.DanmakuDuration) * appConfig.PlaybackRate;

        var firstIndex = Array.FindIndex(Pool, t => t.Time > time - actualDuration);
        if (firstIndex is -1)
            return [];
        var lastIndex = Array.FindLastIndex(Pool, t => t.Time <= time);
#pragma warning disable IDE0046 // 转换为条件表达式
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (lastIndex < firstIndex)
            return [];
        return Pool[firstIndex..(lastIndex + 1)];
#pragma warning restore IDE0046 // 转换为条件表达式
    }
}
