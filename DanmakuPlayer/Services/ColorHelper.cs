using Windows.UI;

namespace DanmakuPlayer.Services;

public static class ColorHelper
{
    public static unsafe Color GetColor(this uint color, byte alpha = 0xFF)
    {
        var ptr = &color;
        var c = (byte*)ptr;
        return Color.FromArgb(alpha, c[2], c[1], c[0]);
    }

    public static unsafe Color GetAlphaColor(this uint color)
    {
        var ptr = &color;
        var c = (byte*)ptr;
        return Color.FromArgb(c[3], c[2], c[1], c[0]); 
    }

    public static unsafe uint GetUInt(this Color color)
    {
        uint ret;
        var ptr = &ret;
        var c = (byte*)ptr;
        c[0] = color.B;
        c[1] = color.G;
        c[2] = color.R;
        return ret;
    }

    public static unsafe uint GetAlphaUInt(this Color color)
    {
        uint ret;
        var ptr = &ret;
        var c = (byte*)ptr;
        c[0] = color.B;
        c[1] = color.G;
        c[2] = color.R;
        c[3] = color.A;
        return ret;
    }
}
