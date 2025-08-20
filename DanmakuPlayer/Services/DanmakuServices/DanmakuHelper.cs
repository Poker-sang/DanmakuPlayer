using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using WinUI3Utilities;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class DanmakuHelper
{
    private static int _RenderCount;

    /// <summary>
    /// 弹幕池
    /// </summary>
    public static IReadOnlyList<Danmaku> Pool { get; set; } = [];

    public static CreatorProvider Current { get; set; } = null!;

    public static RenderMode RenderType { get; set; }

    public static bool IsRendering { get; set; }

    public static void Rendering(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs e, TimeSpan timeSpan, TempConfig tempConfig, AppConfig appConfig)
    {
        var timeMs = (int)timeSpan.TotalMilliseconds;
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
                Current = new(sender);
                RenderType &= ~RenderMode.ReloadProvider;
            }

            var context = new DanmakuContext((float)Current.ViewHeight, appConfig);
            var count = Pool.Count(danmaku => danmaku.RenderInit(context, Current));
            if (appConfig.RenderBefore)
            {
                if (appConfig.DanmakuEnableStrokes)
                    foreach (var (danmakuString, layout) in Current.Layouts)
                        Current.Geometries[danmakuString] = CanvasGeometry.CreateText(layout);
            }
            else
                Current.ClearLayouts();

            _RenderCount = count;
            RenderType &= ~RenderMode.RenderInit;
        }

        if ((RenderType & RenderMode.RenderOnce) is not 0 || (RenderType & RenderMode.RenderAlways) is not 0)
            using (e.DrawingSession)
            {
                e.DrawingSession.Clear(Colors.Transparent);

                if (appConfig.RenderBefore)
                    foreach (var t in DisplayingDanmaku(timeMs, appConfig, tempConfig))
                        t.OnRender(e.DrawingSession, Current, timeMs);
                else
                {
                    Current.ClearLayoutRefCount();
                    foreach (var t in DisplayingDanmaku(timeMs, appConfig, tempConfig))
                    {
                        Current.AddLayoutRef(t);
                        t.OnRender(e.DrawingSession, Current, timeMs);
                    }

                    Current.ClearUnusedLayoutRef();
                }

                if ((RenderType & RenderMode.RenderOnce) is not 0)
                    RenderType &= ~RenderMode.RenderOnce;
            }

        IsRendering = false;
    }

    public static void ClearPool() => Pool = [];

    public static async Task<int> RenderAsync(CanvasAnimatedControl canvas, RenderMode renderType, CancellationToken token)
    {
        RenderType = renderType;
        await WaitForRenderAsync(canvas, token);
        return _RenderCount;
    }

    private static async Task WaitForRenderAsync(CanvasAnimatedControl canvas, CancellationToken token)
    {
        IsRendering = true;
        canvas.Invalidate();
        while (IsRendering)
            await Task.Delay(500, token);
    }

    public static IReadOnlyList<Danmaku> DisplayingDanmaku(int timeMs, AppConfig appConfig, TempConfig tempConfig)
    {
        if (Pool is [])
            return [];

        var duration = tempConfig.UsePlaybackRate3 ? appConfig.DanmakuDuration / 3 : appConfig.DanmakuDuration;
        var playbackRate = tempConfig.UsePlaybackRate3 ? 3 : appConfig.PlaybackRate;
        var actualDurationMs = (int) (Math.Max(10, duration) * playbackRate * 1000);

        if (Pool is not List<Danmaku> list)
            return ThrowHelper.NotSupported<Danmaku[]>(nameof(Pool));

        var firstIndex = list.FindIndex(t => t.TimeMs > timeMs - actualDurationMs);
        if (firstIndex is -1)
            return [];
        var lastIndex = list.FindLastIndex(t => t.TimeMs <= timeMs);
#pragma warning disable IDE0046 // 转换为条件表达式
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (lastIndex < firstIndex)
            return [];
        return list[firstIndex..(lastIndex + 1)];
#pragma warning restore IDE0046 // 转换为条件表达式
    }
}
