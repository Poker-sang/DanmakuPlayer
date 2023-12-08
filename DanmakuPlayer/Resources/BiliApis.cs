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
    public const string DanmakuFromCid = "https://api.bilibili.com/x/v2/dm/web/seg.so?type=1&";

    public const string BangumiInfo = "https://api.bilibili.com/pgc/review/user?";

    public const string BangumiEpisode = "https://api.bilibili.com/pgc/web/season/section?";

    public const string BangumiEpisodeInfo = "https://api.bilibili.com/pgc/view/web/season?";

    public static Task<JsonDocument> GetVideoPageList(string bv, CancellationToken token) => $"{VideoPageList}bvid={bv}".DownloadJsonAsync(token);
    public static Task<JsonDocument> GetVideoPageList(int av, CancellationToken token) => $"{VideoPageList}aid={av}".DownloadJsonAsync(token);

    public static Task<Stream?> GetDanmaku(int cid, int segment, CancellationToken token) => $"{DanmakuFromCid}oid={cid}&segment_index={segment}".TryDownloadStreamAsync(token);

    public static Task<JsonDocument> GetBangumiInfo(int mediaId, CancellationToken token) => $"{BangumiInfo}media_id={mediaId}".DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisode(int seasonId, CancellationToken token) => $"{BangumiEpisode}season_id={seasonId}".DownloadJsonAsync(token);

    public static Task<JsonDocument> GetBangumiEpisodeInfo(int episodeId, CancellationToken token) => $"{BangumiEpisodeInfo}ep_id={episodeId}".DownloadJsonAsync(token);
}
