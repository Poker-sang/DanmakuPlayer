using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bilibili.Community.Service.Dm.V1;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Models.Remote;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Windows.System;
using Windows.UI.Core;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public partial class BackgroundPanel
{
    private readonly DanmakuFilter _filter = [DanmakuCombiner.CombineAsync, DanmakuRegex.MatchAsync];
    private CancellationTokenSource _cancellationTokenSource = new();
    private DateTime _lastTime;
    private bool _isRightPressing;
    private TimeOnly _lastPressTime;
    private readonly DispatcherTimer _webViewSyncTimer = new()
    {
        Interval = TimeSpan.FromSeconds(0.25)
    };

    private async Task OnCIdChangedAsync()
    {
        try
        {
            Vm.LoadingDanmaku = true;

            RootTeachingTip.ShowAndHide(MainPanelResources.DanmakuLoading, TeachingTipSeverity.Information, Emoticon.Okay);

            await LoadDanmakuAsync(async token =>
            {
                var tempPool = new List<Danmaku>();
                var danmakuCount = 0;
                var testCount = 0;
                for (var i = 0; ; ++i)
                {
                    try
                    {
                        if (await GetDanmakuAsync(tempPool, Vm.CId, i, token, BiliApis.GetWebDanmakuAsync))
                            break;
                    }
                    catch
                    {
                        if (await GetDanmakuAsync(tempPool, Vm.CId, i, token, BiliApis.GetMobileDanmaku))
                            break;
                    }

                    // 连续5次获取不到新弹幕（30min）也结束
                    if (tempPool.Count == danmakuCount)
                        testCount += 1;
                    if (testCount >= 5)
                        break;
                    danmakuCount = tempPool.Count;
                }

                return tempPool;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            RootTeachingTip.ShowAndHide(Emoticon.Shocked + " " + MainPanelResources.UnknownException, TeachingTipSeverity.Error, ex.Message);
        }
        finally
        {
            Vm.LoadingDanmaku = false;
        }

        return;

        static async Task<bool> GetDanmakuAsync(List<Danmaku> tempPool, ulong cId, int i, CancellationToken token, Func<ulong, int, CancellationToken, Task<Stream?>> getDanmakuAsync)
        {
            await using var danmaku = await getDanmakuAsync(cId, i + 1, token);
            if (danmaku is null)
                return true;
            var reply = DmSegMobileReply.Parser.ParseFrom(danmaku);
            tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
            return false;
        }
    }

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmakuAsync(Func<CancellationToken, Task<List<Danmaku>>> action)
    {
        Vm.TempConfig.IsPlaying = false;

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        if (!Vm.EnableWebView2 || !WebView.HasVideo)
        {
            Vm.TotalTime = TimeSpan.Zero;
            Vm.Time = TimeSpan.Zero;
        }

        DanmakuHelper.ClearPool();

        try
        {
            var tempPool = await action(_cancellationTokenSource.Token);

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.ObtainedAndFiltrating, tempPool.Count), TeachingTipSeverity.Information, Emoticon.Okay);

            DanmakuHelper.Pool = await _filter.FiltrateAsync(tempPool, Vm.AppConfig, _cancellationTokenSource.Token);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.FiltratedAndRendering, DanmakuHelper.Pool.Length, filtrateRate), TeachingTipSeverity.Information, Emoticon.Okay);

            var renderedCount = await DanmakuHelper.RenderAsync(DanmakuCanvas, RenderMode.RenderInit, _cancellationTokenSource.Token);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            if (!Vm.EnableWebView2 || !WebView.HasVideo)
                Vm.TotalTime = TimeSpan.FromMilliseconds((DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].TimeMs) + Vm.AppConfig.DanmakuActualDurationMs);

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.DanmakuReady, DanmakuHelper.Pool.Length, filtrateRate, renderRate, totalRate), TeachingTipSeverity.Ok, Emoticon.Okay);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            RootTeachingTip.ShowAndHide(Emoticon.Depressed + " " + MainPanelResources.ExceptionThrown, TeachingTipSeverity.Error, e.Message);
        }

        Vm.TempConfig.IsPlaying = true;
    }

    public async void ReloadDanmaku(RenderMode renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        Vm.TempConfig.IsPlaying = false;

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        try
        {
            _ = await DanmakuHelper.RenderAsync(DanmakuCanvas, renderType, _cancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Vm.TempConfig.IsPlaying = true;
    }

    public void ResetProvider() => ReloadDanmaku(RenderMode.ReloadProvider);

    public void DanmakuFontChanged() => ReloadDanmaku(RenderMode.ReloadFormats);

    #region 播放及暂停

    private async void WebViewSyncTimerTick(object? sender, object e)
    {
        if (!Vm.EnableWebView2 || !WebView.HasVideo)
            return;

        var lastTime = Vm.Time;
        await WebView.LockOperationsAsync(async operations =>
        {
            Vm.Time = TimeSpan.FromSeconds(await operations.CurrentTimeAsync());
            Vm.IsPlaying = await operations.IsPlayingAsync();
        });
        if (Math.Abs((Vm.Time - lastTime).TotalSeconds) > 0.5)
            await SyncAsync();
    }

    private void TimerTick()
    {
        var now = DateTime.UtcNow;
        var timeNow = TimeOnly.FromDateTime(now);
        var timeSpan = now - _lastTime;
        try
        {
            if (timeSpan > TimeSpan.FromSeconds(0.5))
                return;

            if (Vm.Time < Vm.TotalTime)
            {
                if (Vm.ActualIsPlaying)
                    Vm.ActualTime += timeSpan;
            }
            else
            {
                _ = PauseAsync();
                Vm.Time = TimeSpan.Zero;
            }

            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Right) & CoreVirtualKeyStates.Down) is 0)
            {
                // 如果是 按下->抬起
                // 之前按下不超过500ms，认为是单击
                if (Vm.TempConfig.UsePlaybackRate3)
                {
                    // 恢复倍速
                    Vm.TempConfig.UsePlaybackRate3 = false;
                    TrySetPlaybackRate();
                    StatusChanged();
                }
                else if (_isRightPressing)
                    FastForward(false, false);

                _isRightPressing = false;
            }
            else
            {
                // 如果是 按下->按下
                if (_isRightPressing)
                {
                    // 按下超过500ms，设为3倍速
                    if ((timeNow - _lastPressTime).TotalMilliseconds > 500)
                    {
                        Vm.TempConfig.UsePlaybackRate3 = true;
                        TrySetPlaybackRate();
                    }
                }
                // 如果是 抬起->按下
                else
                    // 记录按下的时间点
                    _lastPressTime = timeNow;

                _isRightPressing = true;
            }
        }
        finally
        {
            _lastTime = now;
        }
    }

    private async Task ResumeAsync()
    {
        DanmakuHelper.RenderType = RenderMode.RenderAlways;
        Vm.IsPlaying = true;
        await WebView.LockOperationsAsync(async operations => await operations.PlayAsync());
        // 傻逼WebView的视频播放后一段时间内IsPlaying仍然为false
        await Task.Delay(500);
        await SyncAsync();
    }

    private async Task PauseAsync()
    {
        DanmakuHelper.RenderType = RenderMode.RenderOnce;
        Vm.IsPlaying = false;
        await WebView.LockOperationsAsync(async operations => await operations.PauseAsync());
        await Task.Delay(500);
        await SyncAsync();
    }

    public void TrySetPlaybackRate()
    {
        _ = WebView.LockOperationsAsync(async operations => await operations.SetPlaybackRateAsync(Vm.ActualPlaybackRate));
    }

    #endregion

    /// <summary>
    /// 快进
    /// </summary>
    /// <param name="fast">快速快进</param>
    /// <param name="back">是否后退</param>
    private async void FastForward(bool fast, bool back)
    {
        var fastForwardTime = fast ? _LargeStep : TimeSpan.FromSeconds(Vm.AppConfig.PlayFastForward);
        if (back)
            fastForwardTime = -fastForwardTime;
        var time = TimeSpan.FromTicks(Math.Clamp(
            (Vm.Time + fastForwardTime).Ticks,
            0,
            Vm.TotalTime.Ticks));
        if (Vm.EnableWebView2 && WebView.HasVideo) 
            await WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(time.TotalSeconds));

        Vm.Time = time;
        StatusChanged();
    }

    private void VolumeUp(int volumeUp)
    {
        var volume = Math.Clamp(Vm.Volume + volumeUp, 0, 100);
        // see VolumeChanged()
        Vm.Volume = volume;
    }

    private void StatusChanged(string name, string value)
    {
        if (!RemoteService.IsCurrentConnected)
            return;

        var current = Status;
        current.ChangedValues[name] = value;

        _ = RemoteService.Current.SendStatusAsync(current);
    }

    private void StatusChanged()
    {
        if (!RemoteService.IsCurrentConnected)
            return;

        _ = RemoteService.Current.SendStatusAsync(Status);
    }

    public RemoteStatus Status
    {
        get => new()
        {
            IsPlaying = Vm.IsPlaying,
            CurrentTime = DateTime.UtcNow,
            VideoTime = Vm.Time,
            PlaybackRate = Vm.PlaybackRate,
            DanmakuDelayTime = Vm.DanmakuDelayTime,
        };
        set
        {
            var videoTime = DateTime.UtcNow - value.CurrentTime + value.VideoTime;
            _ = value.IsPlaying ? ResumeAsync() : PauseAsync();
            Vm.Time = videoTime;
            Vm.PlaybackRate = value.PlaybackRate;
            Vm.DanmakuDelayTime = value.DanmakuDelayTime;
            foreach (var (name, changedValue) in value.ChangedValues)
                switch (name)
                {
                    case nameof(Vm.CId):
                        Vm.CId = ulong.Parse(changedValue);
                        break;
                    case nameof(Vm.Url):
                        if (!Vm.EnableWebView2)
                            break;
                        _ = WebView.GotoAsync(changedValue);
                        break;
                    case nameof(Vm.Duration):
                        if (!Vm.EnableWebView2)
                            break;
                        var duration = double.Parse(changedValue);
                        if (WebView.Videos.FirstOrDefault(t => Math.Abs(t.Duration - duration) < 0.1) is { } video)
                            WebView.CurrentVideo = video;
                        break;
                }
            if (WebView.HasVideo)
            {
                _ = WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(videoTime.TotalSeconds));
                TrySetPlaybackRate();
            }
        }
    }
}
