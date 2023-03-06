using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public class BoolToPlayPauseSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => ((bool)value) ? Symbol.Pause : Symbol.Play;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => ThrowHelper.InvalidCast<object>();
}
