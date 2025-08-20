using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class DanmakuRegex
{
    public static async IAsyncEnumerable<Danmaku> MatchAsync(IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, [EnumeratorCancellation] CancellationToken token)
    {
        if (!appConfig.DanmakuEnableRegex)
        {
            await foreach (var danmaku in pool.WithCancellation(token))
                yield return danmaku;
            yield break;
        }

        await foreach (var danmaku in pool.WithCancellation(token))
            if (!appConfig.Regexes.Any(regex => regex.IsMatch(danmaku.Text)))
                yield return danmaku;
    }
}
