using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

public static class DanmakuRegex
{
    public static async Task<IEnumerable<Danmaku>> Match(IEnumerable<Danmaku> pool, AppConfig appConfig) =>
        await Task.Run(() =>
        {
            var regexPatterns = JsonSerializer.Deserialize<string[]>(appConfig.RegexPatterns) ?? Array.Empty<string>();
            return regexPatterns.Aggregate(pool, (current, pattern) => current.Where(d => !Regex.IsMatch(d.Text, pattern)));
        });
}