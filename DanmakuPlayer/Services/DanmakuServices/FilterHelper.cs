using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class FilterHelper
{
    public static IReadOnlyCollection<IDanmakuFilter> FilterList { get; } =
    [
        DanmakuCombiner.Instance,
        DanmakuRegex.Instance
    ];

    public static IAsyncEnumerable<Danmaku> FiltrateAsync(
        this IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, CancellationToken token) =>
        FilterList.Aggregate(pool, (current, filter) => filter.FiltrateAsync(current, appConfig, token));
}
