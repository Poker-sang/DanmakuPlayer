using System;
using System.IO;

namespace DanmakuPlayer.Services;

public static class ClrHelper
{
    extension(File)
    {
        public static FileStream OpenAsyncRead(string path)
            => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

        public static FileStream OpenAsyncWrite(string path)
            => new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, true);
    }

    extension(TimeSpan)
    {
        public static TimeSpan Max(TimeSpan x, TimeSpan y) => x > y ? x : y;
    }
}
