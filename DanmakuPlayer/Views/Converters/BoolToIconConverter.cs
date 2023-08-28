using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public abstract class BoolToIconConverter : IValueConverter
{
    protected abstract object TrueValue { get; }

    protected abstract object FalseValue { get; }

    public object Convert(object value, Type targetType, object parameter, string language) => value.To<bool>() ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => ThrowHelper.InvalidCast<object>();
}
