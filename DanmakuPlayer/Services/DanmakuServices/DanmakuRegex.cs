using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public class DanmakuRegex : IDanmakuFilter
{
    public static IDanmakuFilter Instance { get; } = new DanmakuRegex();

    public bool IsEnabled(AppConfig appConfig) => appConfig.DanmakuEnableRegex;

    public async IAsyncEnumerable<Danmaku> FiltrateCoreAsync(IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, [EnumeratorCancellation] CancellationToken token)
    {
        await foreach (var danmaku in pool.WithCancellation(token))
            if (!appConfig.Regexes.Any(regex => regex.IsMatch(danmaku.Text)))
                yield return danmaku;
    }
}
