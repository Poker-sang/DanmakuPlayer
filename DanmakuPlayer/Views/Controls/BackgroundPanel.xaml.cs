using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ProtoBuf;
using Windows.System;
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
        AppContext.DanmakuCanvas = DanmakuCanvas;
        DragMoveHelper.RootPanel = this;

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
    }

    #region 操作

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmaku(Func<Task<List<Danmaku>>> action)
    {
        try
        {
            Pause();
            _vm.TotalTime = 0;
            _vm.Time = 0;
            DanmakuHelper.ClearPool();

            var tempPool = await action();

            FadeOut($"已获取{tempPool.Count}条弹幕，正在合并", false, "✧(≖ ◡ ≖✿)");

            DanmakuHelper.Pool = (await tempPool.Combine(_vm.AppConfig)).OrderBy(t => t.Time).ToArray();
            var combineRate = DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            FadeOut($"已合并为{DanmakuHelper.Pool.Length}条弹幕，合并率{combineRate}%，正在渲染", false, "('ヮ')");

            var renderedCount = await DanmakuHelper.PoolRenderInit(DanmakuCanvas);
            var renderRate = renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = renderedCount * 100 / tempPool.Count;
            _vm.TotalTime = DanmakuHelper.Pool[^1].Time + _vm.AppConfig.DanmakuDuration;

            FadeOut($"{DanmakuHelper.Pool.Length}条弹幕已装载，渲染率{combineRate}%*{renderRate}%={totalRate}%", false, "(/・ω・)/");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            FadeOut("​( ´･_･)ﾉ(._.`) 发生异常了", true, e.Message);
        }

        if (TbBanner is not null)
            _ = Children.Remove(TbBanner);
        _vm.StartPlaying = true;
    }

    private DateTime _closeSnakeBarTime;

    /// <summary>
    /// 出现信息并消失
    /// </summary>
    /// <param name="message">提示信息</param>
    /// <param name="isError"><see langword="true"/>为错误信息，<see langword="false"/>为提示信息</param>
    /// <param name="hint">信息附加内容</param>
    /// <param name="mSec">信息持续时间</param>
    private async void FadeOut(string message, bool isError, string hint, int mSec = 3000)
    {
        _closeSnakeBarTime = DateTime.Now + TimeSpan.FromMicroseconds(mSec - 100);

        RootSnackBar.Title = message;
        RootSnackBar.Subtitle = hint;
        RootSnackBar.IconSource.To<SymbolIconSource>().Symbol = isError ? Symbol.Important : Symbol.Accept;

        _ = RootSnackBar.IsOpen = true;
        await Task.Delay(mSec);
        if (DateTime.Now > _closeSnakeBarTime)
            _ = RootSnackBar.IsOpen = false;
    }

    public async void DanmakuReload(RenderType renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        TryPause();

        switch (renderType)
        {
            case RenderType.RenderInit:
                _ = await DanmakuHelper.PoolRenderInit(DanmakuCanvas);
                break;
            case RenderType.ReloadProvider:
                await DanmakuHelper.ResetProvider(DanmakuCanvas);
                break;
            case RenderType.ReloadFormats:
                await DanmakuHelper.ResetFormat(DanmakuCanvas);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(renderType), renderType, null);
        }

        TryResume();
    }

    private async void Import(int cId)
    {
        FadeOut("弹幕装填中...", false, "(｀・ω・´)");
        try
        {
            await LoadDanmaku(async () =>
            {
                var tempPool = new List<Danmaku>();
                for (var i = 0; ; ++i)
                {
                    await using var danmaku = await DanmakuPlayer.Resources.BiliApis.GetDanmaku(cId, i + 1);
                    if (danmaku is null)
                        break;
                    var reply = Serializer.Deserialize<Resources.DmSegMobileReply>(danmaku);
                    tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
                }

                return tempPool;
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            FadeOut("━━Σ(ﾟДﾟ川)━ 未知的异常", true, e.Message);
        }
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

        DanmakuReload(RenderType.ReloadProvider);
    }

    public async void RootKeyUp(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Left:
            {
                if (_vm.Time - _vm.AppConfig.PlayFastForward < 0)
                    _vm.Time = 0;
                else
                    _vm.Time -= _vm.AppConfig.PlayFastForward;
                break;
            }
            case VirtualKey.Right:
            {
                if (_vm.Time + _vm.AppConfig.PlayFastForward > _vm.TotalTime)
                    _vm.Time = 0;
                else
                    _vm.Time += _vm.AppConfig.PlayFastForward;
                break;
            }
            case VirtualKey.Space:
            {
                PauseResumeTapped(null!, null!);
                break;
            }
            case VirtualKey.Tab:
            {
                await DialogSetting.ShowAsync();
                break;
            }
            default: break;
        }
    }

    private void RootKeyDown(object sender, KeyRoutedEventArgs e)
    {

    }

    private async void ImportTapped(object sender, RoutedEventArgs e)
    {
        if ((await DialogInput.ShowAsync()) is { } cId)
            Import(cId);
    }

    private async void FileTapped(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickSingleFileAsync();
        if (file is not null)
            await LoadDanmaku(() => Task.FromResult(BiliHelper.ToDanmaku(XDocument.Load(file.Path)).ToList()));
    }

    private void FrontTapped(object sender, RoutedEventArgs e)
    {
        if (CurrentContext.OverlappedPresenter.IsAlwaysOnTop)
        {
            _vm.TopMost = CurrentContext.OverlappedPresenter.IsAlwaysOnTop = false;
            FadeOut("固定上层：关闭", false, "(°∀°)ﾉ");
        }
        else
        {
            _vm.TopMost = CurrentContext.OverlappedPresenter.IsAlwaysOnTop = true;
            FadeOut("固定上层：开启", false, "(・ω< )★");
        }
    }

    private async void SettingTapped(object sender, RoutedEventArgs e) => await DialogSetting.ShowAsync();

    private void CloseTapped(object sender, RoutedEventArgs e) => CurrentContext.App.Exit();

    private void PauseResumeTapped(object sender, RoutedEventArgs e)
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

    private void ImportPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = true;

    private void ImportPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = false;

    private void TitlePointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = true;

    private void TitlePointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = false;

    private void ControlPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = true;

    private void ControlPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = false;

    private void RootUnloaded(object sender, RoutedEventArgs e)
    {
        DanmakuCanvas.RemoveFromVisualTree();
        DanmakuCanvas = null;
    }

    #endregion
}
