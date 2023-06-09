using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using DanmakuPlayer.Views.Converters;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ProtoBuf;
using WinRT;
using WinUI3Utilities;
using RenderType = DanmakuPlayer.Enums.RenderType;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class BackgroundPanel : SwapChainPanel
{
    public void RaiseForegroundChanged() => _vm.RaiseForegroundChanged();

    private readonly RootViewModel _vm = new();

    public BackgroundPanel()
    {
        AppContext.BackgroundPanel = this;
        InitializeComponent();
        DragMoveAndResizeHelper.RootPanel = this;
        AppContext.DanmakuCanvas = DanmakuCanvas;

        DispatcherTimerHelper.Tick += TimerTick;

        _filter = new()
        {
            DanmakuCombiner.Combine,
            DanmakuRegex.Match
        };
    }

    #region 操作

    private readonly DanmakuFilter _filter;

    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmaku(Func<CancellationToken, Task<List<Danmaku>>> action)
    {
        Pause();

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        _vm.TotalTime = 0;
        _vm.Time = 0;
        DanmakuHelper.ClearPool();

        try
        {
            var tempPool = await action(_cancellationTokenSource.Token);

            SnackBarHelper.ShowAndHide(string.Format(MainPanelResources.ObtainedAndFiltrating, tempPool.Count), SnackBarHelper.Severity.Information, Emoticon.Okay);

            DanmakuHelper.Pool = await _filter.Filtrate(tempPool, _vm.AppConfig, _cancellationTokenSource.Token);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            SnackBarHelper.ShowAndHide(string.Format(MainPanelResources.FiltratedAndRendering, DanmakuHelper.Pool.Length, filtrateRate), SnackBarHelper.Severity.Information, Emoticon.Okay);

            var renderedCount = await DanmakuHelper.Render(DanmakuCanvas, RenderType.RenderInit, _cancellationTokenSource.Token);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            _vm.TotalTime = (DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].Time) + _vm.AppConfig.DanmakuActualDuration;

            SnackBarHelper.ShowAndHide(string.Format(MainPanelResources.DanmakuReady, DanmakuHelper.Pool.Length, filtrateRate, renderRate, totalRate), SnackBarHelper.Severity.Ok, Emoticon.Okay);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            SnackBarHelper.ShowAndHide(Emoticon.Depressed + " " + MainPanelResources.ExceptionThrown, SnackBarHelper.Severity.Error, e.Message);
        }

        if (BannerTextBlock is not null)
            _ = Children.Remove(BannerTextBlock);
        _vm.StartPlaying = true;
    }

    public async void ReloadDanmaku(RenderType renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        TryPause();

        _cancellationTokenSource.Cancel();
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

    #region 播放及暂停

    private DateTime _lastTime;

    private void TimerTick(object? sender, object e)
    {
        var now = DateTime.Now;
        if (_vm.Time < _vm.TotalTime)
        {
            if (_vm.IsPlaying)
            {
                _vm.ActualTime += (now - _lastTime).TotalSeconds;
                DanmakuCanvas.Invalidate();
            }
        }
        else
        {
            Pause();
            _vm.Time = 0;
        }
        _lastTime = now;
    }

    private bool _needResume;

    private void TryPause()
    {
        _needResume = _vm.IsPlaying;
        Pause();
    }

    private void TryResume()
    {
        if (_needResume)
            Resume();
        _needResume = false;
    }

    private void Resume()
    {
        _lastTime = DateTime.Now;
        DanmakuHelper.RenderType = RenderType.RenderAlways;
        _vm.IsPlaying = true;
    }

    private void Pause()
    {
        DanmakuHelper.RenderType = RenderType.RenderOnce;
        _vm.IsPlaying = false;
    }

    #endregion

    #endregion

    #region 事件处理

    #region SwapChainPanel事件

    private void RootDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        switch (CurrentContext.OverlappedPresenter.State)
        {
            case OverlappedPresenterState.Maximized:
                CurrentContext.OverlappedPresenter.Restore();
                break;
            case OverlappedPresenterState.Restored:
                CurrentContext.OverlappedPresenter.Maximize();
                break;
        }
    }

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        ReloadDanmaku(RenderType.ReloadProvider);
    }

    private void RootUnloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    #region 快进快退快捷键

    private void RewindInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (_vm.Time - _vm.AppConfig.PlayFastForward < 0)
            _vm.Time = 0;
        else
            _vm.Time -= _vm.AppConfig.PlayFastForward;
    }

    private void FastForwardInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (_vm.Time + _vm.AppConfig.PlayFastForward > _vm.TotalTime)
            _vm.Time = 0;
        else
            _vm.Time += _vm.AppConfig.PlayFastForward;
    }

    #endregion

    #endregion

    #region Title区按钮

    private void CloseTapped(object sender, RoutedEventArgs e) => CurrentContext.App.Exit();

    private void FrontTapped(object sender, RoutedEventArgs e)
    {
        _vm.TopMost = !CurrentContext.OverlappedPresenter.IsAlwaysOnTop;
        if (_vm.TopMost)
            SnackBarHelper.ShowAndHide(MainPanelResources.TopMostOn, SnackBarHelper.Severity.Information,
                Emoticon.Okay);
        else
            SnackBarHelper.ShowAndHide(MainPanelResources.TopMostOff, SnackBarHelper.Severity.Information,
                Emoticon.Okay);
    }

    private async void SettingTapped(object sender, IWinRTObject e) => await DialogSetting.ShowAsync();

    #endregion

    #region Import区按钮

    private async void ImportTapped(object sender, RoutedEventArgs e)
    {
        if (await DialogInput.ShowAsync() is not { } cId)
            return;

        SnackBarHelper.ShowAndHide(MainPanelResources.DanmakuLoading, SnackBarHelper.Severity.Information, Emoticon.Okay);

        try
        {
            await LoadDanmaku(async token =>
            {
                var tempPool = new List<Danmaku>();
                for (var i = 0; ; ++i)
                {
                    await using var danmaku = await BiliApis.GetDanmaku(cId, i + 1, token);
                    if (danmaku is null)
                        break;
                    var reply = Serializer.Deserialize<DmSegMobileReply>(danmaku);
                    tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
                }

                return tempPool;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            SnackBarHelper.ShowAndHide(Emoticon.Shocked + " " + MainPanelResources.UnknownException, SnackBarHelper.Severity.Error, ex.Message);
        }
    }

    private async void FileTapped(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickSingleFileAsync();
        if (file is not null)
            await LoadDanmaku(async token =>
            {
                await using var stream = File.OpenRead(file.Path);
                return BiliHelper.ToDanmaku(await XDocument.LoadAsync(stream, LoadOptions.None, token)).ToList();
            });
    }

    #endregion

    #region Control区事件

    private void PauseResumeTapped(object sender, IWinRTObject e)
    {
        if (_vm.IsPlaying)
            Pause();
        else
            Resume();
    }

    private void DanmakuCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new(sender, _vm.AppConfig);

    private void DanmakuCanvasDraw(CanvasControl sender, CanvasDrawEventArgs e) => DanmakuHelper.Rendering(sender, e, (float)_vm.Time, _vm.AppConfig);

    // private void TimePointerPressed(object sender, PointerRoutedEventArgs e) => TryPause();

    // private void TimePointerReleased(object sender, PointerRoutedEventArgs e) => TryResume();

    #endregion

    #region 区域显隐触发

    private void ImportPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = true;

    private void ImportPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = false;

    private void TitlePointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = true;

    private void TitlePointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = false;

    private void ControlPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = true;

    private void ControlPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = false;

    #region 进度条时间输入

    private void TimeTextTapped(object sender, TappedRoutedEventArgs e)
    {
        TimeText.Text = DoubleToTimeTextConverter.ToTime(_vm.Time);
        _vm.EditingTime = true;
    }

    private void TimeTextLostFocus(object sender, RoutedEventArgs e) => _vm.EditingTime = false;

    private void TimeTextInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (TimeSpan.TryParse(TimeText.Text/*.ReplaceLineEndings("")*/, out var result))
            _vm.Time = Math.Max(Math.Min(TimeText.Text.Count(c => c is ':') switch
            {
                0 => result.TotalDays,
                1 => result.TotalMinutes,
                2 => result.TotalSeconds,
                _ => 1
            }, _vm.TotalTime), 0);

        _vm.EditingTime = false;
    }

    private void TimeTextIsEditing(object sender, DependencyPropertyChangedEventArgs e)
    {
        var tb = sender.To<TextBox>();
        if (tb.IsEnabled)
        {
            _ = tb.Focus(FocusState.Programmatic);
            tb.SelectAll();
        }
    }

    #endregion

    #endregion

    #endregion
}
