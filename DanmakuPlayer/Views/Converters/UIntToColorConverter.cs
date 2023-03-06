using System;
using Windows.UI;
using DanmakuPlayer.Services;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public class UIntToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value.To<uint>().GetAlphaColor();

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value.To<Color>().GetAlphaUInt();
}
