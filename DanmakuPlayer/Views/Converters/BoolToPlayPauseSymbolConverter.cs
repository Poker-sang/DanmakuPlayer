using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToPlayPauseSymbolConverter : BoolToIconConverter
{
    protected override object TrueValue => Symbol.Pause;

    protected override object FalseValue => Symbol.Play;
}
