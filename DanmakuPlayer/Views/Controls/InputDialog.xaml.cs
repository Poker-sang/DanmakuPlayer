using System;
using System.Linq;
using System.Threading.Tasks;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class InputDialog : UserControl
{
    public InputDialog() => InitializeComponent();

    private BiliHelper.CodeType _codeType;

    private int? _cId;

    private VideoPage[] ItemsSource { get; set; } = Array.Empty<VideoPage>();

    public async Task<int?> ShowAsync()
    {
        var result = await Content.To<ContentDialog>().ShowAsync();
        return result is ContentDialogResult.None ? null : _cId;
    }

    #region 操作

    private static void ShowSecondButton(ContentDialog sender) => sender.SecondaryButtonText = "选择此视频";

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

    private async void InquireClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        _cId = null;
        HideSecondButton(sender);
        e.Cancel = true;
        _codeType = TbInput.Text.Match(out var match);
        var code = 0;
        if (_codeType is not BiliHelper.CodeType.BvId and not BiliHelper.CodeType.Error)
            code = int.Parse(match);
        switch (_codeType)
        {
            case BiliHelper.CodeType.Error:
                ShowInfoBar("未匹配到相应的视频！", true);
                break;
            case BiliHelper.CodeType.AvId:
                ItemsSource = (await BiliHelper.Av2CIds(code)).ToArray();
                CheckItemsSource(sender);
                break;
            case BiliHelper.CodeType.BvId:
                ItemsSource = (await BiliHelper.Bv2CIds(match)).ToArray();
                CheckItemsSource(sender);
                break;
            case BiliHelper.CodeType.CId:
                _cId = code;
                sender.Hide();
                break;
            case BiliHelper.CodeType.MediaId:
                code = await BiliHelper.Md2Ss(code);
                goto case BiliHelper.CodeType.SeasonId;
            case BiliHelper.CodeType.SeasonId:
                ItemsSource = (await BiliHelper.Ss2CIds(code)).ToArray();
                CheckItemsSource(sender);
                break;
            case BiliHelper.CodeType.EpisodeId:
                _cId = await BiliHelper.Ep2CId(code);
                sender.Hide();
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(_codeType);
                break;
        }
    }

    private void CheckItemsSource(ContentDialog sender)
    {
        switch (ItemsSource.Length)
        {
            case 0:
                ShowInfoBar("未匹配到相应的视频！", true);
                break;
            case 1:
                _cId = ItemsSource[0].CId;
                sender.Hide();
                break;
            case > 1:
                LvPage.ItemsSource = ItemsSource;
                ShowInfoBar("请选择一个视频：", false);
                ShowSecondButton(sender);
                break;
        }
    }

    private void PageDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => SelectConfirm(sender.To<ListView>());

    private void SelectClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => SelectConfirm(LvPage);

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) => Content.To<ContentDialog>().IsSecondaryButtonEnabled = true;

    #endregion
}
