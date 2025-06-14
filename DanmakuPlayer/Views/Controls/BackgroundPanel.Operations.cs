using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public partial class BackgroundPanel
{
    private readonly DanmakuFilter _filter = [DanmakuCombiner.CombineAsync, DanmakuRegex.MatchAsync];
    private CancellationTokenSource _cancellationTokenSource = new();
    private DateTime _lastTime;
    private int _tickCount;
    private bool _isRightPressing;
    private TimeOnly _lastPressTime;
    private bool _needResume;

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmakuAsync(Func<CancellationToken, Task<List<Danmaku>>> action)
    {
        Pause();

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

        Vm.StartPlaying = true;
    }

    public async void ReloadDanmaku(RenderMode renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        TryPause();

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

        TryResume();
    }

    public void ResetProvider() => ReloadDanmaku(RenderMode.ReloadProvider);

    public void DanmakuFontChanged() => ReloadDanmaku(RenderMode.ReloadFormats);

    #region 播放及暂停

    private async void TimerTick()
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
                if (Vm.IsPlaying)
                    Vm.ActualTime += timeSpan;
            }
            else
            {
                Pause();
                Vm.Time = TimeSpan.Zero;
            }

            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Right) & CoreVirtualKeyStates.Down) is 0)
            {
                // 如果是 按下->抬起
                if (_isRightPressing)
                    // 之前改变了倍速，则恢复
                    if (Vm.PlaybackRate is 3)
                        Vm.PlaybackRate = -1;
                    // 之前按下不超过500ms，认为是单击
                    else
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
                        Vm.PlaybackRate = 3;
                }
                // 如果是 抬起->按下
                else
                    // 记录按下的时间点
                    _lastPressTime = timeNow;

                _isRightPressing = true;
            }

            if (Vm.EnableWebView2 && WebView.HasVideo)
            {
                ++_tickCount;
                if (_tickCount >= 10)
                {
                    _tickCount = 0;
                    var lastTime = Vm.Time;
                    await WebView.LockOperationsAsync(async operations =>
                        Vm.Time = TimeSpan.FromSeconds(await operations.CurrentTimeAsync()));
                    if (Math.Abs((Vm.Time - lastTime).TotalSeconds) > 0.5)
                        Sync();
                }
            }
        }
        finally
        {
            _lastTime = now;
        }
    }

    private void TryPause()
    {
        _needResume = Vm.IsPlaying;
        Pause();
    }

    private void TryResume()
    {
        if (_needResume)
            Resume();
        _needResume = false;
    }

    private async void Resume()
    {
        DanmakuHelper.RenderType = RenderMode.RenderAlways;
        Vm.IsPlaying = true;
        await WebView.LockOperationsAsync(async operations => await operations.PlayAsync());
        Sync();
    }

    private async void Pause()
    {
        DanmakuHelper.RenderType = RenderMode.RenderOnce;
        Vm.IsPlaying = false;
        await WebView.LockOperationsAsync(async operations => await operations.PauseAsync());
        Sync();
    }

    public void TrySetPlaybackRate()
    {
        _ = WebView.LockOperationsAsync(async operations => await operations.SetPlaybackRateAsync(Vm.PlaybackRate));
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
        {
            await WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(time.TotalSeconds));
        }

        Vm.Time = time;
    }

    private void VolumeUp(int volumeUp)
    {
        var volume = Math.Clamp(Vm.Volume + volumeUp, 0, 100);
        // see VolumeChanged()
        Vm.Volume = volume;
    }
}
