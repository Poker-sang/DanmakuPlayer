using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using ProtoBuf;
using WinUI3Utilities;
using DanmakuPlayer.Enums;

namespace DanmakuPlayer.Controls;
public sealed partial class BackgroundPanel : SwapChainPanel
{
    private readonly AppViewModel _vm = new();

    public BackgroundPanel()
    {
        AppContext.BackgroundPanel = this;
        InitializeComponent();
        AppContext.DanmakuCanvas = DanmakuCanvas;
        
        AppContext.Timer.Tick += (sender, _) =>
        {
            if (_vm.Time < _vm.TotalTime)
            {
                if (_vm.IsPlaying)
                {
                    _vm.Time += ((DispatcherTimer)sender!).Interval.TotalSeconds;
                    DanmakuCanvas.Invalidate();
                }
            }
            else
            {
                Pause();
                _vm.Time = 0;
            }
        };
        // AppContext.Timer.Start();


        this.SetDragMove(CurrentContext.AppWindow, CurrentContext.OverlappedPresenter);
    }

    #region 操作

    private async void SettingOpen() => await DialogSetting.ShowAsync();
    // FadeOut("设置已更新", false, "✧(≖ ◡ ≖✿)");
    // BBackGround.Opacity = App.AppConfig.WindowOpacity;

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

            DanmakuHelper.Pool = (await DanmakuCombiner.Combine(tempPool)).OrderBy(t => t.Time).ToArray();

            var combineRate = DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            FadeOut($"已合并为{DanmakuHelper.Pool.Length}条弹幕，合并率{combineRate}%，正在渲染", false, "('ヮ')");

            var renderedCount = await DanmakuHelper.RenderInitPool(DanmakuCanvas);

            var renderRate = renderedCount * 100 / DanmakuHelper.Pool.Length;

            var totalRate = renderedCount * 100 / tempPool.Count;

            _vm.TotalTime = DanmakuHelper.Pool[^1].Time + 10;

            FadeOut($"{DanmakuHelper.Pool.Length}条弹幕已装载，渲染率{combineRate}%*{renderRate}%={totalRate}%", false, "(/・ω・)/");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            FadeOut(e.Message, true, "​( ´･_･)ﾉ(._.`) 发生异常了");
        }

        if (TbBanner is not null)
            _ = Children.Remove(TbBanner);
        BControl.IsHitTestVisible = true;
    }

    /// <summary>
    /// 出现信息并消失
    /// </summary>
    /// <param name="message">提示信息</param>
    /// <param name="isError"><see langword="true"/>为错误信息，<see langword="false"/>为提示信息</param>
    /// <param name="hint">信息附加内容</param>
    /// <param name="mSec">信息持续时间</param>
    private async void FadeOut(string message, bool isError, string? hint = null, int mSec = 3000)
    {
        RootSnackBar.Title = message;
        if (isError)
        {
            RootSnackBar.Subtitle = hint ?? "错误";
            RootSnackBar.IconSource = new SymbolIconSource { Symbol = Symbol.Important };
        }
        else
        {
            RootSnackBar.Subtitle = hint ?? "提示";
            RootSnackBar.IconSource = new SymbolIconSource { Symbol = Symbol.Accept };
        }

        _ = RootSnackBar.IsOpen = true;
        await Task.Delay(mSec);
        _ = RootSnackBar.IsOpen = false;
    }

    public async void DanmakuReload(RenderType renderType)
    {
        TryPause();
        AppContext.Timer.Stop();

        switch (renderType)
        {
            case RenderType.RenderInit:
                _ = await DanmakuHelper.RenderInitPool(DanmakuCanvas);
                break;
            case RenderType.ReloadProvider:
                await DanmakuHelper.ResetProvider(DanmakuCanvas);
                break;
            case RenderType.ReloadFormat:
                await DanmakuHelper.ResetFormat(DanmakuCanvas);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(renderType), renderType, null);
        }

        AppContext.Timer.Start();
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
            FadeOut(e.Message, true, "━━Σ(ﾟДﾟ川)━ 未知的异常");
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
        DanmakuHelper.RenderType = RenderType.None;
        AppContext.Timer.Stop();
    }

    #endregion

    #endregion

    #region 事件处理

    private void WindowDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
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
        DanmakuReload(RenderType.ReloadProvider);
    }

    private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        DanmakuReload(RenderType.ReloadProvider);
    }

    public void WindowKeyUp(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Left:
            {
                //  if (Time - App.AppConfig.PlayFastForward < 0)
                //      Time = 0;
                //  else
                //     Time -= App.AppConfig.PlayFastForward;
                break;
            }
            case VirtualKey.Right:
            {
                //  if (Time + App.AppConfig.PlayFastForward > STime.Maximum)
                //     Time = 0;
                // else
                //      Time += App.AppConfig.PlayFastForward;
                break;
            }
            case VirtualKey.Space:
            {
                PauseResumeTapped(null!, null!);
                break;
            }
            case VirtualKey.Tab:
            {
                SettingOpen();
                break;
            }
            default: break;
        }
    }

    /*
    private void WindowDragEnter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

    private void WindowDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] data)
            LoadDanmaku(() =>
            {
                App.ClearPool();
                App.Pool = BiliHelper.ToDanmaku(XDocument.Load(data[0])).ToArray();
                App.RenderPool();
                return Task.CompletedTask;
            });
    }
    */

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

    private void SettingTapped(object sender, RoutedEventArgs e) => SettingOpen();

    private void CloseTapped(object sender, RoutedEventArgs e) => CurrentContext.App.Exit();

    private void PauseResumeTapped(object sender, RoutedEventArgs e)
    {
        if (_vm.IsPlaying)
            Pause();
        else
            Resume();
    }

    private void DanmakuCanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs e) => DanmakuHelper.Rendering(sender, e, (float)_vm.Time, _vm.AppConfig);

    private void TimeMouseButtonDown(object sender, PointerRoutedEventArgs e) => TryPause();

    private void TimeMouseButtonUp(object sender, PointerRoutedEventArgs e) => TryResume();

    private void ImportPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = true;

    private void ImportPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInImportArea = false;

    private void TitlePointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = true;

    private void TitlePointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInTitleArea = false;

    private void ControlPointerEntered(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = true;

    private void ControlPointerExited(object sender, PointerRoutedEventArgs e) => _vm.PointerInControlArea = false;

    private void BackgroundPanelOnUnloaded(object sender, RoutedEventArgs e)
    {
        DanmakuCanvas.RemoveFromVisualTree();
        DanmakuCanvas = null;
    }

    #endregion

    private void DanmakuCanvas_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new CreatorProvider(sender, _vm.AppConfig);
}
