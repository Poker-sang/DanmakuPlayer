using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Models;
using WinUI3Utilities;

namespace DanmakuPlayer.Services.DanmakuServices;

public class DanmakuFilter : List<Func<IEnumerable<Danmaku>, AppConfig, CancellationToken, Task<IEnumerable<Danmaku>>>>
{
    public async Task<Danmaku[]> FiltrateAsync(List<Danmaku> pool, AppConfig appConfig, CancellationToken token)
    {
        var result = pool.To<IEnumerable<Danmaku>>();
        foreach (var func in this)
            result = await func(result, appConfig, token);
        return result.ToArray();
    }
}
