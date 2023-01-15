using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace DanmakuPlayer.Services.Converters;

public class PlayPauseSymbolProvider : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => ((bool)value) ? Symbol.Pause : Symbol.Play;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
