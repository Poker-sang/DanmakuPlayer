using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ProtoBuf;
using Vanara.Extensions;
using Vanara.PInvoke;
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

            FadeOut($"已获取{tempPool.Count}条弹幕，正在过滤", false, "✧(≖ ◡ ≖✿)");

            DanmakuHelper.Pool = await _filter.Filtrate(tempPool, _vm.AppConfig);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            FadeOut($"已过滤为{DanmakuHelper.Pool.Length}条弹幕，剩余{filtrateRate}%，正在渲染", false, "('ヮ')");

            var renderedCount = await DanmakuHelper.PoolRenderInit(DanmakuCanvas);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            _vm.TotalTime = (DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].Time) + _vm.AppConfig.DanmakuDuration;

            FadeOut($"{DanmakuHelper.Pool.Length}条弹幕已装载，渲染率{filtrateRate}%*{renderRate}%={totalRate}%", false, "(/・ω・)/");
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

    public async void ReloadDanmaku(RenderType renderType)
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
                ThrowHelper.ArgumentOutOfRange(renderType);
                break;
        }

        TryResume();
    }

    #region SnackBar功能

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

    #endregion

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

    private void RootUnloaded(object sender, RoutedEventArgs e)
    {
        DanmakuCanvas.RemoveFromVisualTree();
        DanmakuCanvas = null;
    }

    #region DragMove和放缩实现

    // TODO 锚点位置和控件绑定
    // TODO 最小缩放大小

    private const int MinimumOffset = 10;
    private POINT _mousePoint;
    private PointerOperationType _type;

    private InputSystemCursorShape _lastShape = InputSystemCursorShape.Arrow;

    [Flags]
    private enum PointerOperationType
    {
        /// <summary>
        /// 用以区分有没有在根控件按下按钮
        /// </summary>
        None = 0,
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        LeftTop = Top | Left,
        RightTop = Right | Top,
        LeftBottom = Left | Bottom,
        RightBottom = Right | Bottom,
        Move = Top | Left | Right | Bottom
    }

    private void RootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var frameworkElement = sender.To<FrameworkElement>();
        var width = frameworkElement.ActualWidth;
        var height = frameworkElement.ActualHeight;
        var point = e.GetCurrentPoint(frameworkElement);
        var position = point.Position;
        const int o = MinimumOffset;

        var pointerShape = InputSystemCursorShape.Arrow;
        if (CurrentContext.OverlappedPresenter.State is not OverlappedPresenterState.Maximized)
            switch (position._x, position._y, width - position._x, height - position._y)
            {
                case ( < o, < o, _, _):
                case (_, _, < o, < o):
                    pointerShape = InputSystemCursorShape.SizeNorthwestSoutheast;
                    break;
                case ( < o, _, _, < o):
                case (_, < o, < o, _):
                    pointerShape = InputSystemCursorShape.SizeNortheastSouthwest;
                    break;
                case ( < o, _, _, _):
                case (_, _, < o, _):
                    pointerShape = InputSystemCursorShape.SizeWestEast;
                    break;
                case (_, < o, _, _):
                case (_, _, _, < o):
                    pointerShape = InputSystemCursorShape.SizeNorthSouth;
                    break;
                case (_, _, _, _):
                    break;
            }

        if (pointerShape != _lastShape)
        {
            ProtectedCursor?.Dispose();
            ProtectedCursor = InputSystemCursor.Create(pointerShape);
            _lastShape = pointerShape;
        }

        var properties = point.Properties;
        if (!properties.IsLeftButtonPressed || _type is PointerOperationType.None)
            return;


        _ = User32.GetCursorPos(out var pt);

        var xOffset = pt.X - _mousePoint.X;
        var yOffset = pt.Y - _mousePoint.Y;


        var offset = Vector2.DistanceSquared(Vector2.Zero, new(xOffset, yOffset));

        if (offset < o)
            return;

        if (CurrentContext.OverlappedPresenter.State is OverlappedPresenterState.Maximized)
        {
            if (_type is PointerOperationType.Move)
            {
                var originalSize = CurrentContext.AppWindow.Size;
                CurrentContext.OverlappedPresenter.Restore();
                var size = CurrentContext.AppWindow.Size;
                var rate = 1 - (double)size.Width / originalSize.Width;
                CurrentContext.AppWindow.Move(new((pt.X * rate).To<int>(), (pt.Y * rate).To<int>()));
            }
        }
        else
        {
            var xPos = CurrentContext.AppWindow.Position.X;
            var yPos = CurrentContext.AppWindow.Position.Y;
            var xSize = CurrentContext.AppWindow.Size.Width;
            var ySize = CurrentContext.AppWindow.Size.Height;

            if (_type.IsFlagSet(PointerOperationType.Top))
            {
                yPos += yOffset;
                ySize -= yOffset;
            }
            if (_type.IsFlagSet(PointerOperationType.Bottom))
                ySize += yOffset;

            if (_type.IsFlagSet(PointerOperationType.Left))
            {
                xPos += xOffset;
                xSize -= xOffset;
            }
            if (_type.IsFlagSet(PointerOperationType.Right))
                xSize += xOffset;
            CurrentContext.AppWindow.MoveAndResize(new(xPos, yPos, xSize, ySize));
        }

        _mousePoint.X = pt.X;
        _mousePoint.Y = pt.Y;
    }

    private void RootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var frameworkElement = sender.To<FrameworkElement>();
        var point = e.GetCurrentPoint(frameworkElement);
        var properties = point.Properties;

        if (!properties.IsLeftButtonPressed)
            return;

        var width = frameworkElement.ActualWidth;
        var height = frameworkElement.ActualHeight;
        var position = point.Position;
        // offset
        const int o = MinimumOffset;

        _type = PointerOperationType.None;
        if (position.X < o)
            _type |= PointerOperationType.Left;
        if (position.Y < o)
            _type |= PointerOperationType.Top;
        if (width - position.X < o)
            _type |= PointerOperationType.Right;
        if (height - position.Y < o)
            _type |= PointerOperationType.Bottom;
        if (_type is PointerOperationType.None)
            _type = PointerOperationType.Move;

        _ = frameworkElement.CapturePointer(e.Pointer);
        _ = User32.GetCursorPos(out _mousePoint);
    }

    private void RootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        sender.To<UIElement>().ReleasePointerCaptures();
        _type = PointerOperationType.None;
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
            FadeOut("固定上层：关闭", false, "(°∀°)ﾉ");
        }
        else
        {
            _vm.TopMost = CurrentContext.OverlappedPresenter.IsAlwaysOnTop = true;
            FadeOut("固定上层：开启", false, "(・ω< )★");
        }
    }

    private async void SettingTapped(object sender, RoutedEventArgs e) => await DialogSetting.ShowAsync();

    #endregion

    #region Import区按钮

    private async void ImportTapped(object sender, RoutedEventArgs e)
    {
        if ((await DialogInput.ShowAsync()) is not { } cId)
            return;

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
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            FadeOut("━━Σ(ﾟДﾟ川)━ 未知的异常", true, ex.Message);
        }
    }

    private async void FileTapped(object sender, RoutedEventArgs e)
    {
        var file = await PickerHelper.PickSingleFileAsync();
        if (file is not null)
            await LoadDanmaku(() => Task.FromResult(BiliHelper.ToDanmaku(XDocument.Load(file.Path)).ToList()));
    }

    #endregion

    #region Control区事件

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

    #endregion
}
