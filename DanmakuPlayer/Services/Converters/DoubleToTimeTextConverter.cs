using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Services.Converters;

public class DoubleToTimeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => ToTime(value.To<double>());

    public object ConvertBack(object value, Type targetType, object parameter, string language) => ThrowHelper.InvalidCast<object>();

    public static string ToTime(double sec) =>
        ((int)sec / 60).ToString().PadLeft(2, '0') + ":" +
        ((int)sec % 60).ToString().PadLeft(2, '0');
}
