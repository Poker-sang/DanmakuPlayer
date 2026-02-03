using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI;

namespace DanmakuPlayer.Services;

public static class ColorHelper
{
    /// <param name="argbColor">ARGB格式的4*16位整型</param>
    extension(uint argbColor)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetColor(float alpha = 1)
        {
            return argbColor.GetColor((byte) (0xFF * alpha));
        }

        public Color GetColor(byte alpha = 0xFF)
        {
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref argbColor), 4);
            return Color.FromArgb(alpha, span[2], span[1], span[0]);
        }

        public Color GetAlphaColor()
        {
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref argbColor), 4);
            return Color.FromArgb(span[3], span[2], span[1], span[0]);
        }
    }

    extension(Color color)
    {
        public uint GetUInt(byte alpha = 0xFF)
        {
            var ret = 0u;
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref ret), 4);
            span[0] = color.B;
            span[1] = color.G;
            span[2] = color.R;
            span[3] = alpha;
            return ret;
        }

        public uint GetAlphaUInt()
        {
            var ret = 0u;
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref ret), 4);
            span[0] = color.B;
            span[1] = color.G;
            span[2] = color.R;
            span[3] = color.A;
            return ret;
        }
    }

    extension(System.Drawing.Color color)
    {
        public uint GetUInt(byte alpha = 0xFF)
        {
            var ret = 0u;
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref ret), 4);
            span[0] = color.B;
            span[1] = color.G;
            span[2] = color.R;
            span[3] = alpha;
            return ret;
        }

        public uint GetAlphaUInt()
        {
            var ret = 0u;
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref ret), 4);
            span[0] = color.B;
            span[1] = color.G;
            span[2] = color.R;
            span[3] = color.A;
            return ret;
        }
    }
}
