using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DanmakuPlayer.Controls;

public sealed partial class SettingDialog : UserControl
{
    private readonly SettingViewModel _vm = new();

    public SettingDialog() => InitializeComponent();

    public async Task ShowAsync() => await ((ContentDialog)Content).ShowAsync();


    #region 事件处理

    private void CloseClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => sender.Hide();

    #endregion
}
