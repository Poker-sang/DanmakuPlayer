using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class DanmakuRegex
{
    public static async Task<IEnumerable<Danmaku>> MatchAsync(IEnumerable<Danmaku> pool, AppConfig appConfig, CancellationToken token)
    {
        return !appConfig.DanmakuEnableRegex
            ? pool
            : await Task.Run(() => appConfig.Regexes.Aggregate(pool, (current, pattern) => current.Where(d => !pattern.IsMatch(d.Text))), token);
    }
}
