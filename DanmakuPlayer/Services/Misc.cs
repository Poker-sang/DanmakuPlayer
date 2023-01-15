using System;

namespace DanmakuPlayer.Services;

public static class Misc
{
    public static T Get<T>(this WeakReference<T> w) where T : class
    {
        if (w.TryGetTarget(out var t))
            return t;
        throw new NullReferenceException();
    }

    public static T CastThrow<T>(this object? obj) where T : notnull => (T)(obj ?? throw new InvalidCastException());
}
