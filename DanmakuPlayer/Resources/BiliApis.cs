using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Services;

namespace DanmakuPlayer.Resources;

public static class BiliApis
{
    public const string VideoPageList = "https://api.bilibili.com/x/player/pagelist?";

    /// <remarks>
    /// old .xml api:
    /// http://comment.bilibili.com/
    /// </remarks>
    public const string WebDanmakuFromCid = "https://api.bilibili.com/x/v2/dm/web/seg.so?type=1&";

    public const string MobileDanmakuFromCid = "https://api.bilibili.com/x/v2/dm/list/seg.so?type=1&";

    public const string BangumiInfo = "https://api.bilibili.com/pgc/review/user?";

    public const string BangumiEpisode = "https://api.bilibili.com/pgc/web/season/section?";

    public const string BangumiEpisodeInfo = "https://api.bilibili.com/pgc/view/web/season?";

    public static Task<JsonDocument> GetVideoPageListAsync(string bv, CancellationToken token) => new Uri($"{VideoPageList}bvid={bv}").DownloadJsonAsync(token);

    public static Task<JsonDocument> GetVideoPageListAsync(ulong av, CancellationToken token) => new Uri($"{VideoPageList}aid={av}").DownloadJsonAsync(token);

    public static Task<Stream?> GetWebDanmakuAsync(ulong cid, int segment, CancellationToken token) => new Uri($"{WebDanmakuFromCid}oid={cid}&segment_index={segment}").TryDownloadStreamAsync(token);

    public static Task<Stream?> GetMobileDanmakuAsync(ulong cid, int segment, CancellationToken token) => new Uri($"{MobileDanmakuFromCid}oid={cid}&segment_index={segment}").TryDownloadStreamAsync(token);

    public static Task<JsonDocument> GetBangumiInfoAsync(ulong mediaId, CancellationToken token) => new Uri($"{BangumiInfo}media_id={mediaId}").DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisodeAsync(ulong seasonId, CancellationToken token) => new Uri($"{BangumiEpisode}season_id={seasonId}").DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisodeInfoAsync(ulong episodeId, CancellationToken token) => new Uri($"{BangumiEpisodeInfo}ep_id={episodeId}").DownloadJsonAsync(token);
}
