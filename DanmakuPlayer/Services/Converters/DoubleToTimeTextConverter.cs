using System;
using Microsoft.UI.Xaml.Data;

namespace DanmakuPlayer.Services.Converters;

public class DoubleToTimeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => ToTime((double)value);

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();

    public static string ToTime(double sec) =>
        ((int)sec / 60).ToString().PadLeft(2, '0') + ":" +
        ((int)sec % 60).ToString().PadLeft(2, '0');
}
