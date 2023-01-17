using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using Microsoft.International.Converters.PinYinConverter;

namespace DanmakuPlayer.Services;

public static class DanmakuCombiner
{
    /// <summary>
    /// 全角字符和部分英文标点
    /// </summary>
    private const string FullAngleChars = "　０１２３４５６７８９!＠＃＄％＾＆＊()－＝＿＋［］｛｝;＇:＂,．／＜＞?＼｜｀～ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ";

    /// <summary>
    /// 半角字符和部分中文标点
    /// </summary>
    private const string HalfAngleChars = @" 0123456789！@#$%^&*（）-=_+[]{}；'：""，./<>？\|`~ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private static List<int> Gen2GramArray(string p)
    {
        p += p[0];
        var res = new List<int>();
        for (var i = 0; i < p.Length - 1; ++i)
            res.Add(p[i..(i + 1)].GetHashCode());
        return res;
    }

    private static string ToSubscript(uint x) => x is 0 ? "" : ToSubscript(x / 10) + (char)(0x2080 + x % 10);

    /// <remarks>abnormal edit distance</remarks>
    private static int EditDistance(string p, string q)
    {
        var edCounts = new Dictionary<char, int>();
        foreach (var t in p)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) + 1;

        foreach (var t in q)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) - 1;


        return edCounts.Values.Sum(Math.Abs);
    }

    private static double CosineDistanceSquare(List<int> p, List<int> q)
    {
        var hashList = new Dictionary<int, int>();
        var vector = new List<int[]>();
        foreach (var t in p)
            if (hashList.TryGetValue(t, out var index))
                ++vector[index][0];
            else
            {
                hashList[t] = vector.Count;
                vector.Add(new[] { 1, 0 });
            }

        foreach (var t in q)
            if (hashList.TryGetValue(t, out var index))
                ++vector[index][1];
            else
            {
                hashList[t] = vector.Count;
                vector.Add(new[] { 0, 1 });
            }

        var xy = 0;
        var x = 0;
        var y = 0;

        foreach (var t in vector)
        {
            xy += t[0] * t[1];
            x += t[0] * t[0];
            y += t[1] * t[1];
        }

        return (double)xy * xy / (x * y);
    }
    public const int MinDanmakuSize = 10;


    private static bool Similarity(DanmakuString p, DanmakuString q, AppConfig appConfig)
    {
        // 全等
        if (p.Original == q.Original)
            return true;

        if (appConfig.MaxDistance is not 0)
        {
            // 内容相近
            var dis = EditDistance(p.Original, q.Original);
            if ((p.Length + q.Length < MinDanmakuSize)
                    ? dis < (p.Length + q.Length) * appConfig.MaxDistance / MinDanmakuSize - 1
                    : dis <= appConfig.MaxDistance)
                return true;

            // 谐音
            var pyDis = EditDistance(p.Pinyin, q.Pinyin);
            if ((p.Length + q.Length < MinDanmakuSize)
                    ? pyDis < (p.Length + q.Length) * appConfig.MaxDistance / MinDanmakuSize - 1
                    : pyDis <= appConfig.MaxDistance)
                return true;

            // 完全不相似
            if (dis >= p.Length + q.Length)
                return false;
        }

        if (appConfig.MaxCosine is 10)
        {
            // 词频
            var cos = CosineDistanceSquare(p.Gram, q.Gram) * 10;
            if (cos >= appConfig.MaxCosine)
                return true;
        }

        return false;
    }

    private record DanmakuString(string Original, int Length, string Pinyin, List<int> Gram);

    public static async Task<List<Danmaku>> Combine(this IEnumerable<Danmaku> pool, AppConfig appConfig) =>
        await Task.Run(() =>
        {
            var danmakuChunk = new Queue<(DanmakuString Str, List<Danmaku> Peers)>();
            var outDanmaku = new List<List<Danmaku>>();

            foreach (var danmaku in pool.Where(danmaku => danmaku.Mode is DanmakuMode.Roll or DanmakuMode.Top or DanmakuMode.Bottom))
            {
                var text = danmaku.Text;

                if (text is "")
                    continue;

                for (var i = 0; i < FullAngleChars.Length; ++i)
                    text = text.Replace(FullAngleChars[i], HalfAngleChars[i]);

                var pinyin = "";
                foreach (var c in danmaku.Text)
                    if (ChineseChar.IsValidChar(c))
                        pinyin += new ChineseChar(c).Pinyins[0];
                    else
                        pinyin += c;

                var str = new DanmakuString(text, text.Length, pinyin, Gen2GramArray(text));
                while (danmakuChunk.Count > 0 && danmaku.Time - danmakuChunk.Peek().Peers[0].Time > appConfig.TimeSpan)
                    outDanmaku.Add(danmakuChunk.Dequeue().Peers);

                var addNew = true;
                foreach (var (_, peers) in danmakuChunk
                             .Where(chunk => appConfig.CrossMode || danmaku.Mode == chunk.Peers[0].Mode)
                             .Where(chunk => Similarity(str, chunk.Str, appConfig)))
                {
                    peers.Add(danmaku);
                    addNew = false;
                    break;
                }
                if (addNew)
                    danmakuChunk.Enqueue((str, new List<Danmaku> { danmaku }));
            }
            while (danmakuChunk.Count > 0)
                outDanmaku.Add(danmakuChunk.Dequeue().Peers);

            var ret = new List<Danmaku>();
            foreach (var peers in outDanmaku)
            {
                if (peers.Count is 1)
                {
                    ret.Add(peers[0]);
                    continue;
                }
                var mode = DanmakuMode.Bottom;
                foreach (var danmaku in peers)
                    switch (danmaku.Mode)
                    {
                        case DanmakuMode.Roll:
                        case DanmakuMode.Top when mode is DanmakuMode.Bottom:
                            mode = danmaku.Mode;
                            break;
                        default: break;
                    }

                var represent = new Danmaku(
                    (peers.Count > 5 ? $"₍{ToSubscript((uint)peers.Count)}₎" : "") + peers[0].Text,
                    peers[Math.Min(peers.Count * appConfig.RepresentativePercent / 100, peers.Count - 1)].Time,
                    mode,
                    (int)(25 * (peers.Count <= 5 ? 1 : Math.Log(peers.Count, 5))),
                    (uint)peers.Average(t => t.Color),
                    0,
                    DanmakuPool.Normal,
                    ""
                );
                ret.Add(represent);
            }

            return ret;
        });
}
