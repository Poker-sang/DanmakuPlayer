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
    public InputDialog() => InitializeComponent();

    private BiliHelper.CodeType _codeType;

    private int? _cId;

    private bool _cancel;

    private VideoPage[] ItemsSource { get; set; } = Array.Empty<VideoPage>();

    public async Task<int?> ShowAsync()
    {
        _cancel = false;
        _ = await Content.To<ContentDialog>().ShowAsync();
        return _cancel ? null : _cId;
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

    #endregion

    #region 事件处理

    private CancellationTokenSource _cancellationTokenSource = new();

    private async void InquireClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        _cId = null;
        HideSecondButton(sender);
        e.Cancel = true;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();
        _codeType = InputBox.Text.Match(out var match);
        var code = 0;
        if (_codeType is not BiliHelper.CodeType.BvId and not BiliHelper.CodeType.Error)
            code = int.Parse(match);
        try
        {
            switch (_codeType)
            {
                case BiliHelper.CodeType.Error:
                    ShowInfoBar(InputDialogResources.VideoUnmatched, true);
                    break;
                case BiliHelper.CodeType.AvId:
                    ItemsSource = (await BiliHelper.Av2CIds(code, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.BvId:
                    ItemsSource = (await BiliHelper.Bv2CIds(match, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.CId:
                    _cId = code;
                    sender.Hide();
                    break;
                case BiliHelper.CodeType.MediaId:
                    code = await BiliHelper.Md2Ss(code, _cancellationTokenSource.Token);
                    goto case BiliHelper.CodeType.SeasonId;
                case BiliHelper.CodeType.SeasonId:
                    ItemsSource = (await BiliHelper.Ss2CIds(code, _cancellationTokenSource.Token)).ToArray();
                    CheckItemsSource(sender);
                    break;
                case BiliHelper.CodeType.EpisodeId:
                    _cId = await BiliHelper.Ep2CId(code, _cancellationTokenSource.Token);
                    sender.Hide();
                    break;
                default:
                    ThrowHelper.ArgumentOutOfRange(_codeType);
                    break;
            }
        }
        catch (TaskCanceledException) { }
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

    private void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => _cancel = true;

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    #endregion
}
