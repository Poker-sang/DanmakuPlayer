using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text;
using AssParser;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;

namespace DanmakuPlayer.Services;

public static class RenderHelper
{
    private static int _RenderCount;

    /// <summary>
    /// 弹幕池
    /// </summary>
    public static IReadOnlyList<Danmaku> Pool { get; set; } = [];

    public static AssSubtitleModel? Model { get; set; }

    public static DanmakuCreatorProvider DanmakuProvider { get; set; } = null!;

    public static SubtitleCreatorProvider SubtitleProvider { get; set; } = null!;

    public static RenderMode RenderType { get; set; }

    public static bool IsRendering { get; set; }

    public static void CreateResource(ICanvasAnimatedControl sender)
    {
        DanmakuProvider = new(sender);
        SubtitleProvider = new(sender);
    }

    public static void Rendering(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs e, TimeSpan timeSpan, TempConfig tempConfig, AppConfig appConfig)
    {
        try
        {
            var renderInit = (RenderType & RenderMode.RenderInit) is not 0;
            var subtitleRenderInit = (RenderType & RenderMode.SubtitleRenderInit) is not 0;

            if (renderInit || subtitleRenderInit)
                if ((RenderType & RenderMode.ReloadProvider) is not 0)
                {
                    if ((RenderType & RenderMode.ReloadFormats) is not 0)
                    {
                        DanmakuCreatorProvider.DisposeFormats();
                        RenderType &= ~RenderMode.ReloadFormats;
                    }

                    DisposeProviders();
                    CreateResource(sender);
                    RenderType &= ~RenderMode.ReloadProvider;
                }

            // None时无操作
            var timeMs = (int) timeSpan.TotalMilliseconds;
            if (renderInit && Pool.Count > 0)
            {
                var context = new DanmakuContext(DanmakuProvider.ViewHeight, appConfig);
                var count = Pool.Count(danmaku => danmaku.RenderInit(context, DanmakuProvider));
                DanmakuProvider.DisposeLayouts();

                _RenderCount = count;
            }
            RenderType &= ~RenderMode.RenderInit;

            if (subtitleRenderInit && Model is not null)
            {
                foreach (var style in Model.Styles.Values)
                {
                    var format = new CanvasTextFormat
                    {
                        FontFamily = style.Fontname,
                        FontSize = style.Fontsize,
                        FontWeight = style.Bold ? new(700) : new(400),
                        FontStyle = style.Italic ? FontStyle.Italic : FontStyle.Normal,
                    };

                    SubtitleProvider.Formats[style.Name] = format;
                    SubtitleProvider.SetBrushColor(style.Name, style.PrimaryColour);
                }
            }
            RenderType &= ~RenderMode.SubtitleRenderInit;

            if ((RenderType & RenderMode.RenderOnce) is 0)
            {
                if ((RenderType & RenderMode.RenderAlways) is 0)
                    return;
            }
            else
                RenderType &= ~RenderMode.RenderOnce;

            using (e.DrawingSession)
            {
                e.DrawingSession.Clear(Colors.Transparent);

                DanmakuProvider.ClearLayoutRefCount();
                foreach (var t in DisplayingDanmaku(timeMs, appConfig, tempConfig))
                {
                    DanmakuProvider.AddLayoutRef(t);
                    t.OnRender(e.DrawingSession, DanmakuProvider, timeMs);
                }

                DanmakuProvider.ClearUnusedLayoutRef();

                DisplayingSubtitle(e.DrawingSession, timeMs, appConfig, tempConfig);
            }
        }
        finally
        {
            IsRendering = false;
        }
    }

    public static void ClearSubtitle() => Model = null;

    public static void ClearPool() => Pool = [];

    public static async Task<int> RenderAsync(CanvasAnimatedControl canvas, RenderMode renderType, CancellationToken token)
    {
        RenderType = renderType;
        IsRendering = true;
        canvas.Invalidate();
        while (IsRendering)
            await Task.Delay(500, token);
        return _RenderCount;
    }

    public static IReadOnlyList<Danmaku> DisplayingDanmaku(int timeMs, AppConfig appConfig, TempConfig tempConfig)
    {
        if (Pool is [])
            return [];

        var duration = tempConfig.UsePlaybackRate3 ? appConfig.DanmakuDuration / 3 : appConfig.DanmakuDuration;
        var playbackRate = tempConfig.UsePlaybackRate3 ? 3 : appConfig.PlaybackRate;
        var actualDurationMs = (int) (Math.Max(10, duration) * playbackRate * 1000);

        if (Pool is not List<Danmaku> list)
            throw new NotSupportedException(nameof(Pool));

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

    public static void DisplayingSubtitle(CanvasDrawingSession session, int timeMs, AppConfig appConfig, TempConfig tempConfig)
    {
        if (Model is null)
            return;

        var scaleX = SubtitleProvider.ViewWidth / Model.ScriptInfo.PlayResX;
        var scaleY = SubtitleProvider.ViewHeight / Model.ScriptInfo.PlayResY;
        var transform = System.Numerics.Matrix3x2.CreateScale(scaleX, scaleY)
                        * System.Numerics.Matrix3x2.CreateTranslation(SubtitleCreatorProvider.RenderPadding, SubtitleCreatorProvider.RenderPadding);
        session.Transform = transform;

        var renderWidth = Model.ScriptInfo.PlayResX - (2 * SubtitleCreatorProvider.RenderPadding);
        var renderHeight = Model.ScriptInfo.PlayResY - (2 * SubtitleCreatorProvider.RenderPadding);

        foreach (var item in Model.Events)
        {
            if (item.Type is not EventType.Dialogue || item.Start.TotalMilliseconds > timeMs || timeMs > item.End.TotalMilliseconds)
                continue;

            if (!SubtitleProvider.Formats.TryGetValue(item.Style, out var format) || !Model.Styles.TryGetValue(item.Style, out var style))
                continue;
            
            var textLayout = new CanvasTextLayout(session, item.Text, format, int.MaxValue, int.MaxValue);

            var width = (float) textLayout.LayoutBounds.Width;
            var height = (float) textLayout.LayoutBounds.Height;

            float positionX;
            float positionY;

            if (item.MarginL is 0 && item.MarginR is 0 && item.MarginV is 0)
            {
                // TODO 字幕碰撞检测

                //    [-------2--------]   //
                // [-----1---]  [----3---] //
                positionX = style.Alignment switch
                {
                    { IsLeft: true } => 0,
                    { IsRight: true } => renderWidth - width,
                    { IsCenter: true } or _ => (renderWidth - width) / 2,
                };
                positionY = style.Alignment switch
                {
                    { IsTop: true } => 0,
                    { IsSub: true } => renderHeight - height,
                    { IsMid: true } or _ => (renderHeight - height) / 2,
                };
            }
            else
            {
                positionX = item.MarginL;
                positionY = renderHeight - item.MarginV - height;
            }

            session.DrawTextLayout(textLayout, positionX, positionY, SubtitleProvider.GetBrush(item.Style));
        }
    }

    public static void DisposeProviders()
    {
        DanmakuCreatorProvider.DisposeFormats();
        DanmakuProvider?.Dispose();
        SubtitleProvider?.Dispose();
        DanmakuProvider = null!;
        SubtitleProvider = null!;
    }
}
