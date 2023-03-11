using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
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

        AppContext.Timer.Tick += (sender, _) =>
        {
            if (_vm.Time < _vm.TotalTime)
            {
                if (_vm.IsPlaying)
                {
                    _vm.Time += sender.To<DispatcherTimer>().Interval.TotalSeconds;
                    DanmakuCanvas.Invalidate();
                }
            }
            else
            {
                Pause();
                _vm.Time = 0;
            }
        };
        _filter = new DanmakuFilter
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

            SnackBarHelper.ShowAndHide($"已获取{tempPool.Count}条弹幕，正在过滤", SnackBarHelper.Severity.Information, "✧(≖ ◡ ≖✿)");

            DanmakuHelper.Pool = await _filter.Filtrate(tempPool, _vm.AppConfig, _cancellationTokenSource.Token);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            SnackBarHelper.ShowAndHide($"已过滤为{DanmakuHelper.Pool.Length}条弹幕，剩余{filtrateRate}%，正在渲染", SnackBarHelper.Severity.Information, "('ヮ')");

            var renderedCount = await DanmakuHelper.PoolRenderInit(DanmakuCanvas, _cancellationTokenSource.Token);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            _vm.TotalTime = (DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].Time) + _vm.AppConfig.DanmakuDuration;

            SnackBarHelper.ShowAndHide($"{DanmakuHelper.Pool.Length}条弹幕已装载，渲染率{filtrateRate}%*{renderRate}%={totalRate}%", SnackBarHelper.Severity.Ok, "(/・ω・)/");
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            SnackBarHelper.ShowAndHide("​( ´･_･)ﾉ(._.`) 发生异常了", SnackBarHelper.Severity.Error, e.Message);
        }

        if (TbBanner is not null)
            _ = Children.Remove(TbBanner);
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
            switch (renderType)
            {
                case RenderType.RenderInit:
                    _ = await DanmakuHelper.PoolRenderInit(DanmakuCanvas, _cancellationTokenSource.Token);
                    break;
                case RenderType.ReloadProvider:
                    await DanmakuHelper.ResetProvider(DanmakuCanvas, _cancellationTokenSource.Token);
                    break;
                case RenderType.ReloadFormats:
                    await DanmakuHelper.ResetFormat(DanmakuCanvas, _cancellationTokenSource.Token);
                    break;
                default:
                    ThrowHelper.ArgumentOutOfRange(renderType);
                    break;
            }
        }
        catch (TaskCanceledException)
        {
            return;
        }

        TryResume();
    }

    #region 播放及暂停

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
        _vm.IsPlaying = true;
        DanmakuHelper.RenderType = RenderType.RenderAlways;
        AppContext.Timer.Start();
    }

    private void Pause()
    {
        _vm.IsPlaying = false;
        DanmakuHelper.RenderType = RenderType.RenderOnce;
        AppContext.Timer.Stop();
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
            default:
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
        DanmakuCanvas.RemoveFromVisualTree();
        DanmakuCanvas = null;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    #region 快进快退快捷键

    private void RewindInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_vm.Time - _vm.AppConfig.PlayFastForward < 0)
            _vm.Time = 0;
        else
            _vm.Time -= _vm.AppConfig.PlayFastForward;
    }

    private void FastForwardInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
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
        if (CurrentContext.OverlappedPresenter.IsAlwaysOnTop)
        {
            _vm.TopMost = CurrentContext.OverlappedPresenter.IsAlwaysOnTop = false;
            SnackBarHelper.ShowAndHide("固定上层：关闭", SnackBarHelper.Severity.Information, "(°∀°)ﾉ");
        }
        else
        {
            _vm.TopMost = CurrentContext.OverlappedPresenter.IsAlwaysOnTop = true;
            SnackBarHelper.ShowAndHide("固定上层：开启", SnackBarHelper.Severity.Information, "(・ω< )★");
        }
    }

    private async void SettingTapped(object sender, IWinRTObject e) => await DialogSetting.ShowAsync();

    #endregion

    #region Import区按钮

    private async void ImportTapped(object sender, RoutedEventArgs e)
    {
        if (await DialogInput.ShowAsync() is not { } cId)
            return;

        SnackBarHelper.ShowAndHide("弹幕装填中...", SnackBarHelper.Severity.Information, "(｀・ω・´)");

        try
        {
            await LoadDanmaku(async token =>
            {
                var tempPool = new List<Danmaku>();
                for (var i = 0; ; ++i)
                {
                    await using var danmaku = await DanmakuPlayer.Resources.BiliApis.GetDanmaku(cId, i + 1, token);
                    if (danmaku is null)
                        break;
                    var reply = Serializer.Deserialize<Resources.DmSegMobileReply>(danmaku);
                    tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
                }

                return tempPool;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            SnackBarHelper.ShowAndHide("━━Σ(ﾟДﾟ川)━ 未知的异常", SnackBarHelper.Severity.Error, ex.Message);
        }
    }

    private async void FileTapped(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickSingleFileAsync();
        if (file is not null)
            await LoadDanmaku(async token => BiliHelper.ToDanmaku(await XDocument.LoadAsync(File.OpenRead(file.Path), LoadOptions.None, token)).ToList());
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

    private void DanmakuCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new CreatorProvider(sender, _vm.AppConfig);

    private void DanmakuCanvasDraw(CanvasControl sender, CanvasDrawEventArgs e) => DanmakuHelper.Rendering(sender, e, (float)_vm.Time, _vm.AppConfig);

    private void TimePointerPressed(object sender, PointerRoutedEventArgs e) => TryPause();

    private void TimePointerReleased(object sender, PointerRoutedEventArgs e) => TryResume();

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
        _vm.DefaultInputTime = _vm.Time;
        _vm.InputtingTime = true;
    }

    private void TimeTextLostFocus(object sender, RoutedEventArgs e) => _vm.InputtingTime = false;

    private void TimeTextBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs e)
    {
        if (e.NewText is "")
            return;
        if (e.NewText[^1] is '\r' or '\n')
        {
            if (TimeSpan.TryParse(e.NewText.Trim(), out var result))
                _vm.Time = result.TotalMinutes;

            _vm.InputtingTime = false;
            e.Cancel = true;
        }
        else if (e.NewText.Contains('\r') || e.NewText.Contains('\n'))
        {
            e.Cancel = true;
        }
    }

    private void TimeTextIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var tb = sender.To<TextBox>();
        if (tb.IsEnabled)
            _ = tb.Focus(FocusState.Programmatic);
    }

    #endregion

    #endregion

    private void TeachingTipOnLoaded(object sender, RoutedEventArgs e)
    {
        SnackBarHelper.RootSnackBar = sender.To<TeachingTip>();
    }

    #endregion
}
