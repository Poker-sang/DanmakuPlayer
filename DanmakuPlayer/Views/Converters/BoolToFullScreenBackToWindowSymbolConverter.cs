using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToFullScreenBackToWindowSymbolConverter : BoolToIconConverter
{
    protected override object TrueValue => Symbol.BackToWindow; 
    protected override object FalseValue => Symbol.FullScreen;
}
