using System;
using System.Collections.Generic;
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

    public InputDialog() => InitializeComponent();

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
        _cId = sender.SelectedValue.To<ulong>();
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
        try
        {
            if (await BiliHelper.String2CIdsAsync(InputBox.Text, _cancellationTokenSource.Token) is { } cIds)
                CheckItemsSource(sender, [.. cIds]);
            else
                ShowInfoBar(InputDialogResources.VideoUnmatched, true);
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

    private void CheckItemsSource(ContentDialog sender, IReadOnlyList<VideoPage> cIds)
    {
        switch (cIds.Count)
        {
            case 0:
                VideoPageView.ItemsSource = null;
                ShowInfoBar(InputDialogResources.VideoUnmatched, true);
                break;
            case 1:
                VideoPageView.ItemsSource = null;
                _cId = cIds[0].CId;
                sender.Hide();
                break;
            case > 1:
                VideoPageView.ItemsSource = cIds;
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
