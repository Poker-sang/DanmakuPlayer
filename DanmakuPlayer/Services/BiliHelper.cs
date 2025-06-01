using System;
using System.Collections.Generic;
using System.IO;
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

    private const long Xor = 23442827791579L;
    private const long Mask = 2251799813685247L;

    private const long MaxAid = 1L << 51;
    private const long MinAid = 1L;

    private const int Base = 58;
    private const int BvLen = 12;

    private static ReadOnlySpan<byte> Table => "FcwAPNKTMug3GV5Lj7EJnHpWsx4tb8haYeviqBz6rkCy12mUSDQX9RdoZf"u8;

    private static void Swap(Span<byte> str, int i, int j)
    {
        (str[i], str[j]) = (str[j], str[i]);
    }

    private static void Swap(StringBuilder str, int i, int j)
    {
        (str[i], str[j]) = (str[j], str[i]);
    }

    public static unsafe string Av2Bv(long av)
    {
        if (av is < MinAid or > MaxAid)
            throw new InvalidDataException("av must be in range [1, 2251799813685248]");
        var data = (MaxAid | av) ^ Xor;
        Span<byte> arr = stackalloc byte[BvLen];
        arr[0] = (byte)'B';
        arr[1] = (byte)'V';
        arr[2] = (byte)'1';
        var bvArr = arr[3..];
        fixed (byte* table = Table)
            for (var i = BvLen - 4; i >= 0; --i)
            {
                bvArr[i] = table[data % Base];
                data /= Base;
            }
        Swap(bvArr, 0, 6);
        Swap(bvArr, 1, 4);
        return Encoding.ASCII.GetString(arr);
    }

    public static long Bv2Av(string bv)
    {
        Span<byte> arr = stackalloc byte[BvLen - 3];
        _ = Encoding.ASCII.GetBytes(bv.ToCharArray(3, BvLen - 3), arr);
        Swap(arr, 0, 6);
        Swap(arr, 1, 4);
        long avData = 0;
        foreach (var c in arr)
        {
            avData *= Base;
            avData += Table.IndexOf(c);
        }
        return (avData & Mask) ^ Xor;
    }

    private static bool CheckSuccess(JsonDocument jd) => jd.RootElement.GetProperty("code").GetInt32() is 0;

    private static async Task<IEnumerable<VideoPage>> GetCIdsAsync(Task<JsonDocument> jd)
    {
        var response = await jd;
        return CheckSuccess(response)
            ? response.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(episode => new VideoPage(
                    episode.GetProperty("cid").GetUInt64(),
                    episode.GetProperty("page").GetInt32().ToString(),
                    episode.GetProperty("part").GetString()!))
            : [];
    }

    public static Task<IEnumerable<VideoPage>> Av2CIdsAsync(ulong av, CancellationToken token) => GetCIdsAsync(BiliApis.GetVideoPageListAsync(av, token));

    public static Task<IEnumerable<VideoPage>> Bv2CIdsAsync(string bv, CancellationToken token) => GetCIdsAsync(BiliApis.GetVideoPageListAsync(bv, token));

    public static async Task<ulong?> Ep2CIdAsync(ulong episodeId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiEpisodeInfoAsync(episodeId, token);
        if (CheckSuccess(response))
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            // Linq会多循环几遍
            foreach (var episode in response.RootElement
                         .GetProperty("result")
                         .GetProperty("episodes")
                         .EnumerateArray())
                if (episode.GetProperty("id").GetUInt64() == episodeId)
                    return episode.GetProperty("cid").GetUInt64();

        return null;
    }

    public static async Task<IEnumerable<VideoPage>> Ss2CIdsAsync(ulong seasonId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiEpisodeAsync(seasonId, token);
        return CheckSuccess(response)
            ? response.RootElement
                .GetProperty("result")
                .GetProperty("main_section")
                .GetProperty("episodes")
                .EnumerateArray()
                .Select(episode => new VideoPage(
                    episode.GetProperty("cid").GetUInt64(),
                    episode.GetProperty("title").GetString()!,
                    episode.GetProperty("long_title").GetString()!))
            : [];
    }

    public static async Task<ulong?> Md2SsAsync(ulong mediaId, CancellationToken token)
    {
        var response = await BiliApis.GetBangumiInfoAsync(mediaId, token);
        return CheckSuccess(response)
            ? response.RootElement
                .GetProperty("result")
                .GetProperty("media")
                .GetProperty("season_id").GetUInt64()
            : null;
    }

    [GeneratedRegex("(av|md|ss|ep)[0-9]+")]
    private static partial Regex DigitalRegex();

    [GeneratedRegex(@"(?:BV)1\w{9}")]
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
