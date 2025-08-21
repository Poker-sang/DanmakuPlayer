using System.Collections.Generic;
using System.Threading;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public interface IDanmakuFilter
{
    bool IsEnabled(AppConfig appConfig);

    sealed IAsyncEnumerable<Danmaku> FiltrateAsync(IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, CancellationToken token) =>
        IsEnabled(appConfig) ? FiltrateCoreAsync(pool, appConfig, token) : pool;

    IAsyncEnumerable<Danmaku> FiltrateCoreAsync(IAsyncEnumerable<Danmaku> pool, AppConfig appConfig, CancellationToken token);
}
