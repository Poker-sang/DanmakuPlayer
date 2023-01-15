using System;
using System.Threading.Tasks;
using DanmakuPlayer.Models;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System.Linq;
using Vanara.Extensions;
using DanmakuPlayer.Enums;

namespace DanmakuPlayer.Services;

public static class DanmakuHelper
{
    /// <summary>
    /// 弹幕池
    /// </summary>
    public static Danmaku[] Pool { get; set; } = Array.Empty<Danmaku>();

    public static CreatorProvider Current { get; set; } = null!;

    public static RenderType RenderType { get; set; }
    public static bool IsRendering { get; set; }
    private static int _renderCount;

    // public static bool ResetBaseHeight { get; set; }

    public static void Rendering(CanvasControl sender, CanvasDrawEventArgs e, float time, AppConfig appConfig)
    {
        if (RenderType.IsFlagSet(RenderType.RenderInit))
        {
            if (RenderType.IsFlagSet(RenderType.ReloadProvider))
            {
                if (RenderType.IsFlagSet(RenderType.ReloadFormat))
                {
                    CreatorProvider.DisposeFormats();
                    RenderType = RenderType.SetFlags(RenderType.ReloadFormat, false);
                }

                Current.Dispose();
                Current = new CreatorProvider(sender, appConfig);
                RenderType = RenderType.SetFlags(RenderType.ReloadProvider, false);
            }

            var context = new DanmakuContext((float)sender.ActualHeight);
            _renderCount = Pool.Count(danmaku => danmaku.RenderInit(context, Current));
            RenderType = RenderType.SetFlags(RenderType.RenderInit, false);
        }

        if (RenderType.IsFlagSet(RenderType.RenderOnce))
        {
            e.DrawingSession.Clear(Colors.Transparent);

            foreach (var t in DisplayingDanmaku(time, appConfig))
                t.OnRender(e.DrawingSession, Current, time);

            RenderType = RenderType.SetFlags(RenderType.RenderOnce, false);
        }
        else if (RenderType.IsFlagSet(RenderType.RenderAlways))
        {
            e.DrawingSession.Clear(Colors.Transparent);

            foreach (var t in DisplayingDanmaku(time, appConfig))
                t.OnRender(e.DrawingSession, Current, time);
        }

        IsRendering = false;
    }

    public static void ClearPool() => Pool = Array.Empty<Danmaku>();

    public static async Task<int> RenderInitPool(CanvasControl canvas)
    {
        RenderType = RenderType.RenderInit;
        await WaitForRender(canvas);
        return _renderCount;
    }

    public static async Task ResetFormat(CanvasControl canvas)
    {
        RenderType = RenderType.ReloadFormat | RenderType.RenderInit;
        await WaitForRender(canvas);
    }

    public static async Task ResetProvider(CanvasControl canvas)
    {
        RenderType = RenderType.ReloadProvider | RenderType.ReloadFormat | RenderType.RenderInit;
        await WaitForRender(canvas);
    }

    private static async Task WaitForRender(CanvasControl canvas)
    {
        IsRendering = true;
        canvas.Invalidate();
        while (IsRendering)
            await Task.Delay(500);
    }

    public static Danmaku[] DisplayingDanmaku(float time, AppConfig appConfig)
    {
        var firstIndex = Array.FindIndex(Pool, t => t.Time > time - appConfig.DanmakuDuration);
        if (firstIndex is -1)
            return Array.Empty<Danmaku>();
        var lastIndex = Array.FindLastIndex(Pool, t => t.Time <= time);
        if (lastIndex < firstIndex)
            return Array.Empty<Danmaku>();
        return Pool[firstIndex..(lastIndex + 1)];
    }
}
