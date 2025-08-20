using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public partial class DanmakuFilter : List<Func<IAsyncEnumerable<Danmaku>, AppConfig, CancellationToken, IAsyncEnumerable<Danmaku>>>
{
    public IAsyncEnumerable<Danmaku> FiltrateAsync(IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, CancellationToken token)
    {
        return this.Aggregate(pool, (current, func) => func(current, appConfig, token));
    }
}
