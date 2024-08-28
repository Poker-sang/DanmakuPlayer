using System;
using DanmakuPlayer.Views.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public partial class DoubleToTimeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => C.ToTime(value.To<double>());

    public object ConvertBack(object value, Type targetType, object parameter, string language) => ThrowHelper.InvalidCast<object>();
}
