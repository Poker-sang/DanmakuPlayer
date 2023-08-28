using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToPinUnPinSymbolConverter : BoolToIconConverter
{
    protected override object TrueValue => Symbol.Pin;

    protected override object FalseValue => Symbol.UnPin;
}
