using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value.To<int>() is not 0;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value.To<bool>() ? 1 : 0;
}
