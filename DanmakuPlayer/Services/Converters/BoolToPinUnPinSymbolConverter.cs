using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace DanmakuPlayer.Services.Converters;

public class BoolToPinUnPinSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => ((bool)value) ? Symbol.UnPin : Symbol.Pin;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
