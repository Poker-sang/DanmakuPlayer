using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using Microsoft.International.Converters.PinYinConverter;

namespace DanmakuPlayer.Services.DanmakuServices;

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

    public const int MinDanmakuSize = 10;

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
                vector.Add([1, 0]);
            }

        foreach (var t in q)
            if (hashList.TryGetValue(t, out var index))
                ++vector[index][1];
            else
            {
                hashList[t] = vector.Count;
                vector.Add([0, 1]);
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

    private static bool Similarity(DanmakuString p, DanmakuString q, AppConfig appConfig)
    {
        // 全等
        if (p.Original == q.Original)
            return true;

        if (appConfig.DanmakuMergeMaxDistance is not 0)
        {
            // 内容相近
            var dis = EditDistance(p.Original, q.Original);
            if (p.Length + q.Length < MinDanmakuSize
                    ? dis < (p.Length + q.Length) * appConfig.DanmakuMergeMaxDistance / MinDanmakuSize - 1
                    : dis <= appConfig.DanmakuMergeMaxDistance)
                return true;

            // 谐音
            var pyDis = EditDistance(p.Pinyin, q.Pinyin);
            if (p.Length + q.Length < MinDanmakuSize
                    ? pyDis < (p.Length + q.Length) * appConfig.DanmakuMergeMaxDistance / MinDanmakuSize - 1
                    : pyDis <= appConfig.DanmakuMergeMaxDistance)
                return true;

            // 完全不相似
            if (dis >= p.Length + q.Length)
                return false;
        }

        if (appConfig.DanmakuMergeMaxCosine is 10)
        {
            // 词频
            var cos = CosineDistanceSquare(p.Gram, q.Gram) * 10;
            if (cos >= appConfig.DanmakuMergeMaxCosine)
                return true;
        }

        return false;
    }

    public static async IAsyncEnumerable<Danmaku> CombineAsync(
        IAsyncEnumerable<Danmaku> pool,
        AppConfig appConfig,
        [EnumeratorCancellation] CancellationToken token)
    {
        if (!appConfig.DanmakuEnableMerge)
        {
            await foreach (var danmaku in pool.WithCancellation(token))
                yield return danmaku;
            yield break;
        }

        var channel = Channel.CreateUnbounded<Danmaku>();
        var task = Task.Run(async () =>
        {
            var timeSpan = appConfig.DanmakuMergeTimeSpan * 1000;
            // 当前正在合并的弹幕
            var danmakuChunk = new Queue<(DanmakuString Str, List<Danmaku> Peers)>();
            // 弹幕池中合并后的弹幕
            var outDanmaku = new SortedSet<Danmaku>(_DanmakuComparer);

            await foreach (var danmaku in pool.WithCancellation(token))
            {
                if (danmaku is not
                    {
                        Pool: DanmakuPool.Normal,
                        Mode: DanmakuMode.Roll or DanmakuMode.Top or DanmakuMode.Bottom
                    })
                {
                    outDanmaku.Add(danmaku);
                    continue;
                }

                var text = danmaku.Text;

                if (text is "")
                    continue;

                var currentTime = danmaku.TimeMs;

                while (danmakuChunk.Count > 0 &&
                       currentTime - danmakuChunk.Peek().Peers[0].TimeMs > timeSpan)
                    outDanmaku.Add(Compress(danmakuChunk.Dequeue().Peers));

                while (outDanmaku.Min is { } oldestDanmaku &&
                       currentTime - oldestDanmaku.TimeMs > timeSpan)
                {
                    _ = outDanmaku.Remove(oldestDanmaku);
                    _ = channel.Writer.TryWrite(oldestDanmaku);
                }

                for (var i = 0; i < FullAngleChars.Length; ++i)
                    text = text.Replace(FullAngleChars[i], HalfAngleChars[i]);

                var pinyin = "";
                foreach (var c in danmaku.Text)
                    if (ChineseChar.IsValidChar(c))
                        pinyin += new ChineseChar(c).Pinyins[0];
                    else
                        pinyin += c;

                var str = new DanmakuString(text, text.Length, pinyin, Gen2GramArray(text));

                var addNew = true;
                foreach (var (_, peers) in danmakuChunk
                             .Where(chunk =>
                                 appConfig.DanmakuMergeCrossMode || danmaku.Mode == chunk.Peers[0].Mode)
                             .Where(chunk => Similarity(str, chunk.Str, appConfig)))
                {
                    peers.Add(danmaku);
                    addNew = false;
                    break;
                }

                if (addNew)
                    danmakuChunk.Enqueue((str, [danmaku]));
            }

            while (danmakuChunk.Count > 0)
                outDanmaku.Add(Compress(danmakuChunk.Dequeue().Peers));

            while (outDanmaku.Min is { } oldestDanmaku)
            {
                _ = outDanmaku.Remove(oldestDanmaku);
                _ = channel.Writer.TryWrite(oldestDanmaku);
            }

            _ = channel.Writer.TryComplete();
        }, token);

        await foreach (var danmaku in channel.Reader.ReadAllAsync(token))
            yield return danmaku;
        await task;
    }

    private static readonly Comparer<Danmaku> _DanmakuComparer = Comparer<Danmaku>.Create((x, y) => x.TimeMs.CompareTo(y.TimeMs));

    private static Danmaku Compress(IReadOnlyList<Danmaku> danmakuList)
    {
        if (danmakuList is [{ } first])
            return first;

        var mode = DanmakuMode.Bottom;
        foreach (var danmaku in danmakuList)
            switch (danmaku.Mode)
            {
                case DanmakuMode.Roll:
                case DanmakuMode.Top when mode is DanmakuMode.Bottom:
                    mode = danmaku.Mode;
                    break;
            }

        var represent = new Danmaku(
            (danmakuList.Count > 4 ? $"₍{ToSubscript((uint) danmakuList.Count)}₎" : "") + danmakuList[0].Text,
            0,
            (int) danmakuList.Average(t => t.TimeMs),
            mode,
            (int) (25 * (danmakuList.Count <= 5 ? 1 : Math.Log(danmakuList.Count, 5))),
            (uint) danmakuList.Average(t => t.Color)
        );
        return represent;
    }

    private record DanmakuString(string Original, int Length, string Pinyin, List<int> Gram);
}
