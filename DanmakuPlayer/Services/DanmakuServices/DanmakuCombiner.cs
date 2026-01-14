using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;

namespace DanmakuPlayer.Services.DanmakuServices;

/// <summary>
/// 弹幕合并器核心类
/// </summary>
public partial class DanmakuCombiner : IDanmakuFilter
{
    public static IDanmakuFilter Instance { get; } = new DanmakuCombiner();

    public bool IsEnabled(AppConfig appConfig) => appConfig.DanmakuEnableMerge;

    private static readonly FrozenSet<char> _EndingChars = ".。,，/?？!！…~～@^、+=-_♂♀ ".ToFrozenSet();
    
    [GeneratedRegex(@"[ 　]+")]
    private static partial Regex ExtraSpaceRegex { get; }

    [GeneratedRegex(@"([\u3000-\u9FFF\uFF00-\uFFEF]) (?=[\u3000-\u9FFF\uFF00-\uFFEF])")]
    private static partial Regex CjkSpaceRegex { get; }

    private static string ToSubscript(uint x)
    {
        if (x is 0)
            return "";

        var sb = new StringBuilder();
        while (x > 0)
        {
            _ = sb.Insert(0, (char) ('₀' + (x % 10)));
            x /= 10;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 合并弹幕数组的主入口方法
    /// <code>
    /// var danmakuList = new List&lt;Danmaku&gt; { /* 弹幕数据 */ };
    /// var appConfig = new AppConfig { DanmakuEnableMerge = true, DanmakuMergeThreshold = 5 };
    /// var combinedResult = DanmakuCombiner.CombineDanmaku(danmakuList, appConfig);
    /// </code>
    /// </summary>
    /// <param name="danmakuList">输入的弹幕数组</param>
    /// <param name="appConfig">应用配置（需要包含 DanmakuEnableMerge, DanmakuMergeThreshold 等设置）</param>
    /// <param name="token"></param>
    /// <returns>合并后的弹幕列表</returns>
    public IAsyncEnumerable<Danmaku> FiltrateCoreAsync(IAsyncEnumerable<Danmaku> danmakuList, AppConfig appConfig, CancellationToken token = default)
    {
        // 将簇转换为最终结果（包含后处理）
        return ConvertClustersToDanmakuAsync(DoCombineAsync(PreprocessDanmakuAsync(danmakuList, appConfig), appConfig, token), appConfig, token);
    }

    /// <summary>
    /// 执行实际的弹幕合并操作
    /// </summary>
    private async IAsyncEnumerable<DanmakuCluster> DoCombineAsync(IAsyncEnumerable<ProcessedDanmaku> danmakuList, AppConfig appConfig, [EnumeratorCancellation] CancellationToken token = default)
    {
        var nearbyQueue = new DanmakuQueue<List<ProcessedDanmaku>>();
        var thresholdMs = appConfig.DanmakuMergeTimeSpan * 1000;

        // 相似度检测的本地状态
        var nearbyDanmaku = new List<DanmakuCacheLine>();
        var disposeIndex = 0;

        await foreach (var dm in danmakuList.WithCancellation(token))
        {
            // 移除超出时间窗口的弹幕簇
            while (true)
            {
                if (!nearbyQueue.TryPeek(out var peeked) || dm.Original.TimeMs - peeked[0].Original.TimeMs <= thresholdMs)
                    break;

                yield return ApplyCluster(peeked);
                nearbyQueue.Dequeue();
            }

            // 设置当前文本用于相似度检测
            var currentStr = dm.NormalizedText;

            // 检查当前弹幕是否与已有簇相似
            var similarity = CheckSimilar((uint) dm.Original.Mode, nearbyQueue.IndexL, appConfig, nearbyDanmaku, ref disposeIndex, currentStr);

            if (similarity.IsSimilar)
            {
                var indexDiff = nearbyDanmaku.Count - nearbyQueue.IndexL - 1;
                if (indexDiff >= 0 && indexDiff < nearbyQueue.Count)
                {
                    var candidate = nearbyQueue.GetAt(indexDiff);
                    dm.SimilarityReason = similarity.Reason;
                    candidate.Add(dm);
                }
                else
                {
                    nearbyQueue.Enqueue([dm]);
                }
            }
            else
            {
                nearbyQueue.Enqueue([dm]);
            }
        }

        // 处理剩余的簇
        while (nearbyQueue.Count > 0)
        {
            var peeked = nearbyQueue.Dequeue();
            if (peeked is not null)
                yield return ApplyCluster(peeked);
        }
    }

    /// <summary>
    /// 预处理弹幕列表
    /// </summary>
    private static IAsyncEnumerable<ProcessedDanmaku> PreprocessDanmakuAsync(IAsyncEnumerable<Danmaku> danmakuList, AppConfig appConfig) => danmakuList
        .Where(danmaku => appConfig.DanmakuMergeProcessSubtitle || danmaku.Pool is not DanmakuPool.Subtitle)
        .Where(danmaku => appConfig.DanmakuMergeProcessAdvanced || danmaku.Mode is not DanmakuMode.Advanced)
        .Where(danmaku => appConfig.DanmakuMergeProcessBottom || danmaku.Mode is not DanmakuMode.Bottom)
        .Where(danmaku => danmaku.Mode is not (DanmakuMode.Code or DanmakuMode.Bas))
        .Select(t => new ProcessedDanmaku(t, NormalizeText(t.DisplayText, appConfig), "ORIG"));

    /// <summary>
    /// 规范化文本
    /// </summary>
    private static string NormalizeText(string text, AppConfig appConfig)
    {
        text = text.Trim();

        // 去除结尾字符
        if (appConfig.DanmakuMergeTrimEnding)
        {
            var len = text.Length;
            while (len > 0 && _EndingChars.Contains(text[len - 1]))
                len--;

            // 若全是结尾字符，保留原始长度
            if (len is not 0)
                text = text[..len];
        }

        // 转换全角字符为半角
        if (appConfig.DanmakuMergeTrimWidth)
        {
            var sb = new StringBuilder();
            foreach (var ch in text)
                _ = sb.Append(_WidthTable.GetValueOrDefault(ch, ch));
            text = sb.ToString();
        }

        // 去除多余空格
        if (appConfig.DanmakuMergeTrimSpace)
        {
            text = ExtraSpaceRegex.Replace(text, " ");
            text = CjkSpaceRegex.Replace(text, "$1");
        }

        return text;
    }

    /// <summary>
    /// 应用单个弹幕簇
    /// </summary>
    private static DanmakuCluster ApplyCluster(List<ProcessedDanmaku> items)
    {
        if (items.Count is 1)
            return new(items, items[0].NormalizedText);

        // 统计各个文本出现次数
        var textCounts = new Dictionary<string, int>();
        var mostTexts = new List<string>();
        var mostCount = 0;

        foreach (var item in items)
        {
            var text = item.NormalizedText;
            textCounts.TryGetValue(text, out var count);
            count++;
            textCounts[text] = count;

            if (count > mostCount)
            {
                mostTexts = [text];
                mostCount = count;
            }
            else if (count == mostCount)
            {
                mostTexts.Add(text);
            }
        }

        var chosenText = SelectMedianLength(mostTexts);

        return new(items, chosenText);
    }

    /// <summary>
    /// 从字符串列表中选择中位数长度的字符串
    /// </summary>
    private static string SelectMedianLength(List<string> strings)
    {
        if (strings.Count is 1)
            return strings[0];

        strings.Sort((x, y) => x.Length.CompareTo(y.Length));
        var mid = strings.Count / 2;
        return strings[mid];
    }

    /// <summary>
    /// 将簇转换为合并弹幕结果（包含模式提升、字号放缩等后处理）
    /// </summary>
    private static async IAsyncEnumerable<Danmaku> ConvertClustersToDanmakuAsync(IAsyncEnumerable<DanmakuCluster> clusters, AppConfig appConfig, [EnumeratorCancellation] CancellationToken token = default)
    {
        const int displayValueTimeThreshold = 5000; // 5秒
        const double displayValuePower = 0.35;
        const double shrinkMaxRate = 1.732;

        var displayValueBase = Math.Pow(appConfig.DanmakuMergeShrinkThreshold, displayValuePower);
        var onscreenDisplayValue = 0d;
        var displayValueQueue = new Queue<(int time, double value)>();

        await foreach (var cluster in clusters.WithCancellation(token))
        {
            if (cluster.Items.Count is 0)
                continue;

            // 选择代表性弹幕（根据百分比选择）
            var representativePercent = appConfig.DanmakuMergeRepresentativePercent;
            var repIndex = Math.Min(
                (int) (cluster.Items.Count * representativePercent / 100.0),
                cluster.Items.Count - 1);
            var representative = cluster.Items[repIndex].Original;

            // 计算最大值：字号、权重、模式
            var maxFontSize = representative.Size;
            var maxMode = representative.Mode;

            foreach (var item in cluster.Items)
            {
                var dm = item.Original;

                // 字号：只统计小于30的
                if (dm.Size < 30)
                    maxFontSize = Math.Max(maxFontSize, dm.Size);

                maxMode = dm.Mode switch
                {
                    // 模式提升：底部(4) > 顶部(5) > 其他
                    DanmakuMode.Bottom => DanmakuMode.Bottom,
                    DanmakuMode.Top when maxMode != DanmakuMode.Bottom => DanmakuMode.Top,
                    _ => maxMode
                };
            }

            // 应用模式提升
            var finalMode = representative.Mode;
            if (appConfig.DanmakuMergeModeElevation)
            {
                finalMode = maxMode;
            }

            // 计算放大率并应用字号放大
            var finalSize = maxFontSize;
            if (appConfig.DanmakuMergeEnlarge)
            {
                var enlargeRate = CalculateEnlargeRate(cluster.Items.Count);
                if (enlargeRate > 1.001)
                {
                    finalSize = (int) Math.Ceiling(finalSize * enlargeRate);
                }
            }

            // 构建显示文本（添加标记）
            var displayText = BuildMarkedText(cluster.ChosenText, cluster.Items.Count, appConfig);

            // 创建合并后的弹幕
            var mergedDanmaku = new Danmaku(
                displayText,
                representative.Id,
                representative.TimeMs,
                finalMode,
                finalSize,
                representative.Color
            )
            {
                UserHash = representative.UserHash,
                Colorful = representative.Colorful,
                UnixTimeStamp = representative.UnixTimeStamp,
                Attribute = representative.Attribute,
                Pool = representative.Pool
            };

            // 应用弹幕密度调整（如果启用）
            if (appConfig.DanmakuMergeShrinkThreshold > 0 || appConfig.DanmakuMergeDropThreshold > 0)
            {
                // 更新屏幕上的弹幕密度值
                while (displayValueQueue.Count > 0 && mergedDanmaku.TimeMs > displayValueQueue.Peek().time)
                {
                    onscreenDisplayValue -= displayValueQueue.Dequeue().value;
                }

                var dv = CalculateDisplayValue(mergedDanmaku);

                // 检查是否需要丢弃
                if (appConfig.DanmakuMergeDropThreshold > 0 &&
                    ShouldDrop(onscreenDisplayValue, appConfig.DanmakuMergeDropThreshold, cluster.Items.Count))
                    continue;

                onscreenDisplayValue += dv;
                displayValueQueue.Enqueue((mergedDanmaku.TimeMs + displayValueTimeThreshold, dv));

                // 检查是否需要缩小
                if (appConfig.DanmakuMergeShrinkThreshold > 0 &&
                    onscreenDisplayValue > appConfig.DanmakuMergeShrinkThreshold)
                {
                    var shrinkRate = Math.Min(
                        Math.Pow(onscreenDisplayValue, displayValuePower) / displayValueBase,
                        shrinkMaxRate
                    );

                    var newSize = (int) (mergedDanmaku.Size / shrinkRate);
                    mergedDanmaku = new Danmaku(
                        mergedDanmaku.Text,
                        mergedDanmaku.Id,
                        mergedDanmaku.TimeMs,
                        mergedDanmaku.Mode,
                        newSize,
                        mergedDanmaku.Color
                    )
                    {
                        UserHash = mergedDanmaku.UserHash,
                        Colorful = mergedDanmaku.Colorful,
                        UnixTimeStamp = mergedDanmaku.UnixTimeStamp,
                        Attribute = mergedDanmaku.Attribute,
                        Pool = mergedDanmaku.Pool
                    };
                }
            }

            // 应用滚动转换（将过长的顶部/底部弹幕转为滚动）
            if (appConfig.DanmakuMergeScrollThreshold > 0)
            {
                if (mergedDanmaku.Mode is DanmakuMode.Top or DanmakuMode.Bottom)
                {
                    var width = EstimateTextWidth(mergedDanmaku.Text, mergedDanmaku.Size);

                    if (width > appConfig.DanmakuMergeScrollThreshold)
                    {
                        var prefix = mergedDanmaku.Mode is DanmakuMode.Bottom ? "↓" : "↑";
                        var newText = prefix + mergedDanmaku.Text;

                        mergedDanmaku = new(
                            newText,
                            mergedDanmaku.Id,
                            mergedDanmaku.TimeMs,
                            DanmakuMode.Roll,
                            mergedDanmaku.Size,
                            mergedDanmaku.Color)
                        {
                            UserHash = mergedDanmaku.UserHash,
                            Colorful = mergedDanmaku.Colorful,
                            UnixTimeStamp = mergedDanmaku.UnixTimeStamp,
                            Attribute = mergedDanmaku.Attribute,
                            Pool = mergedDanmaku.Pool
                        };
                    }
                }
            }

            yield return mergedDanmaku;
        }
    }

    /// <summary>
    /// 构建带标记的文本（如 [x5] text 或 text [x5] 或下标形式）
    /// </summary>
    private static string BuildMarkedText(string text, int count, AppConfig appConfig)
    {
        if (appConfig.DanmakuMergeMarkStyle is DanmakuMergeMarkStyle.Off || count <= appConfig.DanmakuMergeMarkThreshold)
            return text;

        var mark = appConfig.DanmakuMergeUseSubscript
            ? $"₍{ToSubscript((uint) count)}₎"
            : $"[x{count}]";

        return appConfig.DanmakuMergeMarkStyle is DanmakuMergeMarkStyle.Suffix
            ? text + mark
            : mark + text;
    }

    /// <summary>
    /// 计算放大率（基于合并数量）
    /// </summary>
    private static double CalculateEnlargeRate(int count)
    {
        if (count <= 5)
            return 1.0;

        const double mathLog5 = 1.6094379124341003; // Math.Log(5)
        return Math.Log(count) / mathLog5;
    }

    /// <summary>
    /// 计算弹幕的显示值（用于密度计算）
    /// </summary>
    private static double CalculateDisplayValue(Danmaku dm)
    {
        // 统计小字符数量（ASCII + 下标字符等）
        var smallCharCount = 0;
        foreach (var ch in dm.Text)
            if (ch is >= ' ' and <= '~' or >= '₀' and <= '₉' or '₍' or '₎' or '↓' or '↑')
                smallCharCount++;

        var textLength = dm.Text.Length - (smallCharCount / 2.0);
        var fontSizeFactor = Math.Max(Math.Min(dm.Size / 25.0, 2.5), 0.7);

        return Math.Sqrt(textLength) * Math.Pow(fontSizeFactor, 1.5);
    }

    /// <summary>
    /// 判断是否应该丢弃弹幕
    /// </summary>
    private static bool ShouldDrop(double currentDisplayValue, double threshold, int peerCount)
    {
        if (threshold <= 0 || currentDisplayValue <= threshold)
            return false;

        var dropRate = ((currentDisplayValue - threshold) / threshold)
                      + 0.25
                      - ((Math.Sqrt(peerCount) - 1) / 5.0);

        return dropRate >= 1 || (dropRate > 0 && Random.Shared.NextDouble() < dropRate);
    }

    /// <summary>
    /// 估算文本宽度
    /// </summary>
    private static double EstimateTextWidth(string text, int fontSize)
    {
        // 简化的宽度估算：中文字符约等于字号，英文字符约为字号的0.5
        var width = 0.0;
        foreach (var ch in text)
        {
            if (ch is >= (char) 0x4E00 and <= (char) 0x9FFF) // 中日韩统一表意文字
                width += fontSize;
            else
                width += fontSize * 0.5;
        }
        return width;
    }

    private (bool IsSimilar, string Reason) CheckSimilar(uint mode, int indexL, AppConfig appConfig, List<DanmakuCacheLine> nearbyDanmaku, ref int disposeIndex, string currentStr)
    {
        var indexR = nearbyDanmaku.Count;
        var p = new DanmakuCacheLine(currentStr, mode, appConfig);
        const int maxIndexRange = (1 << 19) - 3;

        // 清理已处理过的缓存行
        for (; disposeIndex < indexL; disposeIndex++)
        {
            if (disposeIndex < nearbyDanmaku.Count)
                nearbyDanmaku[disposeIndex].Clear();
        }

        // 限制搜索范围
        if (indexL + maxIndexRange < indexR)
            indexL = indexR - maxIndexRange;

        // 在缓存中查找相似弹幕
        for (var index = indexL; index < indexR; index++)
        {
            if (index >= nearbyDanmaku.Count)
                break;

            var q = nearbyDanmaku[index];
            if (!appConfig.DanmakuMergeCrossMode && p.Mode != q.Mode)
                continue;

            var res = CalculateSimilarity(p, q, appConfig);
            if (res.IsSimilar)
                return res;
        }

        // 将当前弹幕添加到缓存
        nearbyDanmaku.Add(p);
        return (false, "");
    }

    /// <summary>
    /// 计算两个文本的相似度
    /// </summary>
    private (bool IsSimilar, string Reason) CalculateSimilarity(DanmakuCacheLine text1, DanmakuCacheLine text2, AppConfig appConfig)
    {
        // 完全相同
        if (text1.Original == text2.Original)
            return (true, "==");

        // 编辑距离检测
        var lenSum = text1.Original.Length + text2.Original.Length;
        var minDanmakuSize = Math.Max(1, appConfig.DanmakuMergeMaxEditDistance * 2);

        // 根据弹幕大小调整阈值
        var threshold = lenSum < minDanmakuSize
            ? (appConfig.DanmakuMergeMaxEditDistance * lenSum / minDanmakuSize) + 1
            : appConfig.DanmakuMergeMaxEditDistance;

        // 检查长度差异是否超过限制
        var calcEditDist = Math.Abs(text1.Str.Count - text2.Str.Count) <= appConfig.DanmakuMergeMaxEditDistance;
        var editDist = 0;
        if (calcEditDist)
        {
            editDist = EditDistance(text1.Str, text2.Str);

            if (editDist <= threshold)
                return (true, $"≤{editDist}");
        }

        // 检查长度差异是否超过限制
        var calcPinyinDist = Math.Abs(text1.Pinyin.Count - text2.Pinyin.Count) <= appConfig.DanmakuMergeMaxEditDistance;
        if (calcPinyinDist)
        {
            var pinyinDist = EditDistance(text1.Str, text2.Str);

            if (pinyinDist <= threshold)
                return (true, $"≤{pinyinDist}");
        }

        // 余弦相似度检测
        // 如果编辑距离显示两个文本没有共同字符，它们不可能有余弦相似
        var calcCosineSim = appConfig.DanmakuMergeMaxCosineDistance <= 10
                            && !(calcEditDist && editDist >= lenSum);

        if (calcCosineSim)
        {
            var cosineSim = (int) (10 * CosineDistance(text1.Gram, text2.Gram));
            if (cosineSim >= appConfig.DanmakuMergeMaxCosineDistance)
                return (true, $"{cosineSim}%");
        }

        return (false, "");
    }

    /// <summary>
    /// 计算Levenshtein编辑距离 (快速O(n)版本，基于字符频率)
    /// 这不是教科书中的真正编辑距离，因为那样会太慢
    /// 实际上这个版本在O(n)时间内运行
    /// </summary>
    private static int EditDistance<T>(Dictionary<T, int> s1, Dictionary<T, int> s2) where T : notnull
    {
        var diffMap = s1.ToDictionary();

        foreach (var (c, x) in s2)
        {
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(diffMap, c, out _);
            value -= x;
        }

        return diffMap.Values.Sum(Math.Abs);
    }

    /// <summary>
    /// 余弦距离 - 对应 C++ 的 cosine_distance 函数
    /// 接收预先计算好的 Dictionary&lt;int, int&gt; 频率字典
    /// 只需要 2 次遍历：第一遍处理 s1 及其在 s2 中的对应值
    /// 第二遍处理只在 s2 中存在的元素
    /// 返回 (x*x) / (y*z)，其中 x=点积, y=s1模平方, z=s2模平方
    /// </summary>
    private static float CosineDistance(Dictionary<int, int> s1, Dictionary<int, int> s2)
    {
        var dotProduct = 0;
        var normA = 0;
        var normB = 0;
        var processed = new HashSet<int>();

        // 第一遍：遍历 s1，计算点积、normA、以及 normB 的部分
        foreach (var (c, xa) in s1)
        {
            s2.TryGetValue(c, out var xb);
            dotProduct += xa * xb;
            normA += xa * xa;
            normB += xb * xb;
            processed.Add(c);
        }

        // 第二遍：处理只在 s2 中存在的元素，补充 normB
        foreach (var (c, xb) in s2)
            if (!processed.Contains(c))
                normB += xb * xb;

        if (normA is 0 || normB is 0)
            return 0;

        // 返回 (x*x) / (y*z) 公式
        return (float) dotProduct * dotProduct / normA / normB;
    }

    /// <summary>
    /// 内部使用的处理后弹幕对象
    /// </summary>
    private class ProcessedDanmaku(Danmaku original, string normalizedText, string similarityReason)
    {
        public Danmaku Original { get; set; } = original;

        public string NormalizedText { get; set; } = normalizedText;

        public string SimilarityReason { get; set; } = similarityReason;
    }

    /// <summary>
    /// 弹幕簇
    /// </summary>
    private class DanmakuCluster(List<ProcessedDanmaku> items, string chosenText)
    {
        public List<ProcessedDanmaku> Items { get; } = items;

        public string ChosenText { get; } = chosenText;
    }

    /// <summary>
    /// 简单队列实现，用于管理时间窗口内的弹幕
    /// </summary>
    private class DanmakuQueue<T> where T : notnull
    {
        private readonly List<T> _storage = [];

        public int IndexL { get; private set; }

        public int IndexR => _storage.Count;

        public int Count => IndexR - IndexL;

        public void Enqueue(T item)
        {
            _storage.Add(item);
        }

        public T? Dequeue()
        {
            if (Count == 0)
                return default;

            var item = _storage[IndexL];
            IndexL++;
            return item;
        }

        public bool TryPeek([NotNullWhen(true)]out T? peek)
        {
            if (Count is 0)
            {
                peek = default;
                return false;
            }
            peek = _storage[IndexL];
            return true;
        }

        public T GetAt(int relativeIndex)
        {
            return _storage[IndexL + relativeIndex];
        }
    }

    private class DanmakuCacheLine
    {
        public uint Mode { get; }

        public string Original { get; }

        public Dictionary<char, int> Str { get; }

        public Dictionary<ushort, int> Pinyin { get; }

        public Dictionary<int, int> Gram { get; }

        public DanmakuCacheLine(string text, uint mode, AppConfig appConfig)
        {
            Mode = mode;
            Original = text;
            Str = [];
            Pinyin = [];
            Gram = [];

            foreach (var c in text)
                Str.Push(c);

            // gen pinyin
            if (appConfig.DanmakuMergeEnablePinYin)
            {
                const ushort pinyinBase = 0xE000; // U+E000 ~ U+F8FF: Private Use Area

                foreach (var c in Original)
                {
                    if (_PinyinCache.TryGetValue(c, out var value))
                    {
                        Pinyin.Push((ushort) (pinyinBase + value.Item1));
                        if (value.Item2 > 0)
                            Pinyin.Push((ushort) (pinyinBase + value.Item2));
                    }
                    else
                    {
                        var ch = c;
                        // to lowercase
                        if (ch is >= 'A' and <= 'Z')
                            ch = (char) (ch + 'a' - 'A');
                        Pinyin.Push(ch);
                    }
                }
            }

            // gen gram
            if (appConfig.DanmakuMergeMaxCosineDistance <= 10 && Original.Length > 0)
            {
                const int hashMod = 1007;

                // 最后一个字符作为起始
                var lastChar = Original[^1] % hashMod;

                foreach (var c in Original)
                {
                    var currentChar = c % hashMod;
                    var biGram = (lastChar * hashMod) + currentChar;
                    Gram.Push(biGram);
                    lastChar = currentChar;
                }
            }
        }

        public void Clear()
        {
            Str.Clear();
            Pinyin.Clear();
            Gram.Clear();
        }
    }
}

static file class Helper
{
    extension<T>(Dictionary<T, int> dict) where T : notnull
    {
        public void Push(T key)
        {
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            value++;
        }
    }
}
