using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services.DanmakuServices;
using Microsoft.UI.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;
using Windows.System;
using Windows.UI.Core;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public partial class BackgroundPanel
{
    private readonly DanmakuFilter _filter = [DanmakuCombiner.Combine, DanmakuRegex.Match];
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
    private async Task LoadDanmaku(Func<CancellationToken, Task<List<Danmaku>>> action)
    {
        Pause();

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        if (!WebView.HasVideo)
        {
            Vm.TotalTime = 0;
            Vm.Time = 0;
        }
        DanmakuHelper.ClearPool();

        try
        {
            var tempPool = await action(_cancellationTokenSource.Token);

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.ObtainedAndFiltrating, tempPool.Count), TeachingTipSeverity.Information, Emoticon.Okay);

            DanmakuHelper.Pool = await _filter.Filtrate(tempPool, Vm.AppConfig, _cancellationTokenSource.Token);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.FiltratedAndRendering, DanmakuHelper.Pool.Length, filtrateRate), TeachingTipSeverity.Information, Emoticon.Okay);

            var renderedCount = await DanmakuHelper.Render(DanmakuCanvas, RenderMode.RenderInit, _cancellationTokenSource.Token);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            if (!WebView.HasVideo)
                Vm.TotalTime = (DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].Time) + Vm.AppConfig.DanmakuActualDuration;

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
            _ = await DanmakuHelper.Render(DanmakuCanvas, renderType, _cancellationTokenSource.Token);
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

    private async void TimerTick(object? sender, object e)
    {
        var now = DateTime.Now;
        var timeNow = TimeOnly.FromDateTime(now);
        if (Vm.Time < Vm.TotalTime)
        {
            if (Vm.IsPlaying)
            {
                Vm.ActualTime += (now - _lastTime).TotalSeconds;
                DanmakuCanvas.Invalidate();
            }
        }
        else
        {
            Pause();
            Vm.Time = 0;
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

        if (WebView.HasVideo)
        {
            ++_tickCount;
            if (_tickCount is 10)
            {
                _tickCount = 0;
                var lastTime = Vm.Time;
                Vm.Time = await WebView.Operations.CurrentTimeAsync();
                if (Math.Abs(Vm.Time - lastTime) > 0.5)
                    Sync();
            }
        }

        _lastTime = now;
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
        _lastTime = DateTime.Now;
        DanmakuHelper.RenderType = RenderMode.RenderAlways;
        Vm.IsPlaying = true;
        if (WebView.HasVideo)
        {
            await WebView.Operations.PlayAsync();
            Sync();
        }
    }

    private async void Pause()
    {
        DanmakuHelper.RenderType = RenderMode.RenderOnce;
        Vm.IsPlaying = false;
        if (WebView.HasVideo)
        {
            await WebView.Operations.PauseAsync();
            Sync();
        }
    }

    public void TrySetPlaybackRate()
    {
        _ = WebView.Operations?.SetPlaybackRateAsync(Vm.PlaybackRate);
    }

    #endregion

    /// <summary>
    /// 快进
    /// </summary>
    /// <param name="fast">快速快进</param>
    /// <param name="back">向前</param>
    private async void FastForward(bool fast, bool back)
    {
        var fastForwardTime = fast ? 90 : Vm.AppConfig.PlayFastForward;
        if (back)
            fastForwardTime = -fastForwardTime;
        var time = Math.Clamp(Vm.Time + fastForwardTime, 0, Vm.TotalTime);
        if (WebView.HasVideo)
        {
            await WebView.Operations.SetCurrentTimeAsync(time);
            Vm.Time = time;
        }
        else
            Vm.Time = time;
    }

    private void VolumeUp(int volumeUp)
    {
        var volume = Math.Clamp(Vm.Volume + volumeUp, 0, 100);
        // see VolumeChanged()
        Vm.Volume = volume;
    }
}
