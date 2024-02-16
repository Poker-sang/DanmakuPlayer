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

    public static Task<JsonDocument> GetVideoPageListAsync(string bv, CancellationToken token) => $"{VideoPageList}bvid={bv}".DownloadJsonAsync(token);

    public static Task<JsonDocument> GetVideoPageListAsync(int av, CancellationToken token) => $"{VideoPageList}aid={av}".DownloadJsonAsync(token);

    public static Task<Stream?> GetWebDanmakuAsync(int cid, int segment, CancellationToken token) => $"{WebDanmakuFromCid}oid={cid}&segment_index={segment}".TryDownloadStreamAsync(token);

    public static Task<Stream?> GetMobileDanmaku(int cid, int segment, CancellationToken token) => $"{MobileDanmakuFromCid}oid={cid}&segment_index={segment}".TryDownloadStreamAsync(token);

    public static Task<JsonDocument> GetBangumiInfoAsync(int mediaId, CancellationToken token) => $"{BangumiInfo}media_id={mediaId}".DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisodeAsync(int seasonId, CancellationToken token) => $"{BangumiEpisode}season_id={seasonId}".DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisodeInfoAsync(int episodeId, CancellationToken token) => $"{BangumiEpisodeInfo}ep_id={episodeId}".DownloadJsonAsync(token);
}
