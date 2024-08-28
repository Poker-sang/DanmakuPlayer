using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class InputDialog : UserControl
{
    private bool _canceled;

    private ulong? _cId;

    private BiliHelper.CodeType _codeType;

    public InputDialog() => InitializeComponent();

    private VideoPage[] ItemsSource { get; set; } = [];

    public async Task<ulong?> ShowAsync()
    {
        _canceled = false;
        _ = await Content.To<ContentDialog>().ShowAsync();
        return _canceled ? null : _cId;
    }

    #region 操作

    private static void ShowSecondButton(ContentDialog sender) => sender.SecondaryButtonText = InputDialogResources.SelectThisVideo;

    private static void HideSecondButton(ContentDialog sender)
    {
        sender.SecondaryButtonText = null;
        sender.IsSecondaryButtonEnabled = false;
    }

    private void SelectConfirm(ListView sender)
    {
        var index = sender.SelectedIndex;
        _cId = ItemsSource[index].CId;
        Content.To<ContentDialog>().Hide();
    }

    private void ShowInfoBar(string message, bool isError)
    {
        IbMessage.Message = message;
        IbMessage.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
        IbMessage.IsOpen = true;
    }

    private void CancelToken()
    {
        if (_cancellationTokenSource is null)
            return;
        lock (_cancellationTokenSource)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    #endregion

    #region 事件处理

    private CancellationTokenSource? _cancellationTokenSource;

    private int _activeCount;

#pragma warning disable IDE0052
    private int ActiveCount
#pragma warning restore IDE0052
    {
        get => _activeCount;
        set
        {
            _activeCount = value < 0 ? 0 : value;
            ProgressRing.IsActive = value > 0;
        }
    }

    private async void InquireClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        _cId = null;
        HideSecondButton(sender);
        e.Cancel = true;
        CancelToken();
        ++ActiveCount;
        _cancellationTokenSource = new();
        _codeType = InputBox.Text.Match(out var match);
        var code = 0ul;
        if (_codeType is not BiliHelper.CodeType.BvId and not BiliHelper.CodeType.Error)
            code = ulong.Parse(match);
        try
        {
            switch (_codeType)
            {
                case BiliHelper.CodeType.Error:
                    ShowInfoBar(InputDialogResources.VideoUnmatched, true);
                    break;
                case BiliHelper.CodeType.AvId:
                    ItemsSource = (await BiliHelper.Av2CIdsAsync(code, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.BvId:
                    ItemsSource = (await BiliHelper.Bv2CIdsAsync(match, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.CId:
                    _cId = code;
                    sender.Hide();
                    break;
                case BiliHelper.CodeType.MediaId:
                    if (await BiliHelper.Md2SsAsync(code, _cancellationTokenSource.Token) is { } ss)
                    {
                        code = ss;
                        goto case BiliHelper.CodeType.SeasonId;
                    }

                    ItemsSource = [];
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.SeasonId:
                    ItemsSource = (await BiliHelper.Ss2CIdsAsync(code, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.EpisodeId:
                    _cId = await BiliHelper.Ep2CIdAsync(code, _cancellationTokenSource.Token);
                    sender.Hide();
                    break;
                default:
                    ThrowHelper.ArgumentOutOfRange(_codeType);
                    break;
            }
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        finally
        {
            --ActiveCount;
        }
    }

    private void CheckItemsSource(ContentDialog sender)
    {
        switch (ItemsSource.Length)
        {
            case 0:
                ShowInfoBar(InputDialogResources.VideoUnmatched, true);
                break;
            case 1:
                _cId = ItemsSource[0].CId;
                sender.Hide();
                break;
            case > 1:
                VideoPageView.ItemsSource = ItemsSource;
                ShowInfoBar(InputDialogResources.PleaseSelectAVideo, false);
                ShowSecondButton(sender);
                break;
        }
    }

    private void SelectClick(object sender, object e) => SelectConfirm(VideoPageView);

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) => Content.To<ContentDialog>().IsSecondaryButtonEnabled = true;

    private void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        _canceled = true;
        CancelToken();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CancelToken();

    #endregion
}
