using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bilibili.Community.Service.Dm.V1;
using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;

namespace DanmakuPlayer.Services;

public static partial class BiliHelper
{
    public enum CodeType
    {
        Error, AvId, BvId, CId, MediaId, SeasonId, EpisodeId
    }

    private const int Xor = 177451812;

    private const long Add = 8728348608;

    private static ReadOnlySpan<byte> Table => "fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF"u8;

    public static unsafe string Av2Bv(this int av)
    {
        fixed (byte* table = Table)
        {
            var result = stackalloc byte[10] { 49, 0, 0, 52, 0, 49, 0, 55, 0, 0 };
            var temp = (av ^ Xor) + Add;
            result[9] = table[temp % 58];
            result[8] = table[temp / 58 % 58];
            result[1] = table[temp / (58 * 58) % 58];
            result[6] = table[temp / (58 * 58 * 58) % 58];
            result[2] = table[temp / (58 * 58 * 58 * 58) % 58];
            result[4] = table[temp / (58 * 58 * 58 * 58 * 58) % 58];
            return Encoding.ASCII.GetString(result, 12);
        }
    }

    public static int Bv2Av(this string bv) =>
        (int)((Table.IndexOf((byte)bv[9]) +
            Table.IndexOf((byte)bv[8]) * 58 +
            Table.IndexOf((byte)bv[1]) * 58 * 58 +
            Table.IndexOf((byte)bv[6]) * 58 * 58 * 58 +
            Table.IndexOf((byte)bv[2]) * 58 * 58 * 58 * 58 +
            Table.IndexOf((byte)bv[4]) * 58 * 58 * 58 * 58 * 58 - Add) ^ Xor);

    private static bool CheckSuccess(JsonDocument jd) => jd.RootElement.GetProperty("code").GetInt32() is 0;

    private static async Task<IEnumerable<VideoPage>> GetCIdsAsync(Task<JsonDocument> jd)
    {
        var response = await jd;
        return CheckSuccess(response)
            ? response.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(episode => new VideoPage(
                    episode.GetProperty("cid").GetInt32(),
                    episode.GetProperty("page").GetInt32().ToString(),
                    episode.GetProperty("part").GetString()!))
            : [];
    }

    public static Task<IEnumerable<VideoPage>> Av2CIdsAsync(int av, CancellationToken token) => GetCIdsAsync(BiliApis.GetVideoPageListAsync(av, token));

    public static Task<IEnumerable<VideoPage>> Bv2CIdsAsync(string bv, CancellationToken token) => GetCIdsAsync(BiliApis.GetVideoPageListAsync(bv, token));

    public static async Task<int> Ep2CIdAsync(int episodeId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiEpisodeInfoAsync(episodeId, token);
        if (CheckSuccess(response))
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            // Linq会多循环几遍
            foreach (var episode in response.RootElement
                         .GetProperty("result")
                         .GetProperty("episodes")
                         .EnumerateArray())
                if (episode.GetProperty("id").GetInt32() == episodeId)
                    return episode.GetProperty("cid").GetInt32();

        return -1;
    }

    public static async Task<IEnumerable<VideoPage>> Ss2CIdsAsync(int seasonId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiEpisodeAsync(seasonId, token);
        return CheckSuccess(response)
            ? response.RootElement
                .GetProperty("result")
                .GetProperty("main_section")
                .GetProperty("episodes")
                .EnumerateArray()
                .Select(episode => new VideoPage(
                    episode.GetProperty("cid").GetInt32(),
                    episode.GetProperty("title").GetString()!,
                    episode.GetProperty("long_title").GetString()!))
            : [];
    }

    public static async Task<int> Md2SsAsync(int mediaId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiInfoAsync(mediaId, token);
        return CheckSuccess(response)
            ? response.RootElement
                .GetProperty("result")
                .GetProperty("media")
                .GetProperty("season_id").GetInt32()
            : -1;
    }

    [GeneratedRegex("(av|md|ss|ep)[0-9]+")]
    private static partial Regex DigitalRegex();

    [GeneratedRegex(@"(?:BV)1\w\w4\w1\w7\w\w")]
    private static partial Regex BvRegex();

    public static CodeType Match(this string url, out string result)
    {
        if (DigitalRegex().Match(url) is { Success: true } match)
        {
            result = match.Value[2..];
            return match.Value[..2] switch
            {
                "av" => CodeType.AvId,
                "md" => CodeType.MediaId,
                "ss" => CodeType.SeasonId,
                "ep" => CodeType.EpisodeId,
                _ => CodeType.Error
            };
        }

        if (BvRegex().Match(url) is { Success: true } bvMatch)
        {
            result = bvMatch.Value;
            return CodeType.BvId;
        }

        result = "";
        return CodeType.Error;
    }

    public static IEnumerable<Danmaku> ToDanmaku(IEnumerable<DanmakuElem> elems) => elems.Select(Danmaku.Parse).OrderBy(t => t.Time);

    public static IEnumerable<Danmaku> ToDanmaku(XDocument xDocument) => xDocument.Element("i")!.Elements("d").Select(Danmaku.Parse).OrderBy(t => t.Time);
}
