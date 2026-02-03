using System;
using System.Diagnostics.CodeAnalysis;
using DanmakuPlayer.Services;
using DanmakuRoomRollList = System.Collections.Generic.LinkedList<(float BottomPos, DanmakuPlayer.Models.IDanmakuWidth Danmaku)>;
using DanmakuRoomStaticList = System.Collections.Generic.LinkedList<(float BottomPos, int TimeMs)>;
using RollNode = System.Collections.Generic.LinkedListNode<(float BottomPos, DanmakuPlayer.Models.IDanmakuWidth Danmaku)>;
using StaticNode = System.Collections.Generic.LinkedListNode<(float BottomPos, int TimeMs)>;

namespace DanmakuPlayer.Models;

public partial record Danmaku
{
    private double GetDistance(DanmakuCreatorProvider provider, IDanmakuWidth previous)
    {
        var timeMs = _layoutWidth <= previous.LayoutWidth
            ? TimeMs
            : previous.TimeMs + provider.AppConfig.DanmakuActualDurationMs;

        return GetPosition(previous, timeMs) - GetPosition(this, timeMs) - previous.LayoutWidth;

        // 弹幕左边缘距离屏幕右边缘的距离
        double GetPosition(IDanmakuWidth danmaku, int ms) => (ms - danmaku.TimeMs) * (provider.ViewWidth + danmaku.LayoutWidth) / provider.AppConfig.DanmakuActualDurationMs;
    }

    #region Roll

    /// <summary>
    /// 自上至下插入弹幕
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="list"></param>
    /// <param name="allowOverlap"></param>
    /// <param name="overlapPredicate"></param>
    /// <returns></returns>
    private bool TopDownRollDanmaku(DanmakuCreatorProvider provider, DanmakuRoomRollList list, bool allowOverlap, Func<bool, bool> overlapPredicate)
    {
        // 目前可用区间中，距离上条弹幕的最大距离
        var mostDistance = allowOverlap ? double.NegativeInfinity : 0;
        // 本区间开始位置（上一个区间的结束位置）
        var lastPos = 0f;
        // 插入的第一个区间（靠上）
        RollNode? firstNode = null;
        // 插入的最后一个区间（靠下）
        RollNode? lastNode = null;
        // 应该放置的位置（取左上角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.First; current is not null; current = current.Next)
        {
            // 如果允许重叠，循环中一定会进一次该if分支
            if (TopDownRollCanBePlaced(current, lastPos, out var outNode))
            {
                firstNode = current;
                pos = lastPos;
                (mostDistance, lastNode, overlap) = outNode.Value;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
                // 如果重叠了，则按照最大距离选择
            }
            lastPos = current.Value.BottomPos;
        }

        // 如果重叠了，去判断是否可以重叠
        // 如果firstNode为null则overlap也是默认值true，并且此时一定不允许重叠，故此处一定会为true并且返回
        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos;
        list.AddBefore(firstNode!, new RollNode((pos + (float)_layoutHeight, this)));
        for (var current = firstNode!; current != lastNode!;)
        {
            current = current.Next!;
            list.Remove(current.Previous!);
        }
        return true;

        bool TopDownRollCanBePlaced(RollNode? node, float startPos, [NotNullWhen(true)] out (double MostDistance, RollNode LastNode, bool Overlapped)? outNode)
        {
            // 覆盖区间的时间都大于最长的距离，并且不超出屏幕高度。如果返回true，则一定是目前的最优位置
            // 如果不允许重叠，则就放在第一个可以不重叠放的区间
            // 如果允许重叠，则放在与上一个距离最长的区间
            var intervalLeastDistance = double.PositiveInfinity;
            outNode = null;
            for (; node is not null; node = node.Next)
            {
                var distance = GetDistance(provider, node.Value.Danmaku);
                // 小于目前最小距离
                if (distance <= mostDistance)
                    return false;
                // 在连续区间取最短距离作为距离
                intervalLeastDistance = Math.Min(intervalLeastDistance, distance);
                if (_layoutHeight <= node.Value.BottomPos - startPos)
                {
                    outNode = new()
                    {
                        MostDistance = intervalLeastDistance,
                        LastNode = node,
                        Overlapped = 0 > intervalLeastDistance
                    };
                    // intervalMostTime <= refLeastTime
                    // 既没有小于最小距离，也没有超过屏幕高度
                    return true;
                }
            }
            // 超过屏幕
            return false;
        }
    }

    /// <summary>
    /// 自下至上插入弹幕
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="list"></param>
    /// <param name="allowOverlap"></param>
    /// <param name="overlapPredicate"></param>
    /// <returns></returns>
    private bool BottomUpRollDanmaku(DanmakuCreatorProvider provider, DanmakuRoomRollList list, bool allowOverlap, Func<bool, bool> overlapPredicate)
    {
        // 目前可用区间中，距离上条弹幕的最大距离
        var mostDistance = allowOverlap ? double.NegativeInfinity : 0;
        // 插入的第一个区间（靠下）
        RollNode? firstNode = null;
        // 插入的最后一个区间（靠上）
        RollNode? lastNode = null;
        // 应该放置的位置（取左下角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.Last; current is not null; current = current.Previous)
        {
            // 本区间开始位置
            var lastPos = current.Value.BottomPos;
            // 如果允许重叠，循环中一定会进一次该if分支
            if (BottomUpRollCanBePlaced(current, lastPos, out var outNode))
            {
                firstNode = current;
                pos = lastPos;
                (mostDistance, lastNode, overlap) = outNode.Value;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
                // 如果重叠了，则按照最大距离选择
            }
        }

        // 如果重叠了，去判断是否可以重叠
        // 如果firstNode为null则overlap也是默认值true，并且此时一定不允许重叠，故此处一定会为true并且返回
        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos - (float)_layoutHeight;
        list.AddAfter(firstNode!, new RollNode((pos, this)));
        list.AddBefore(lastNode!, new RollNode((_showPositionY, lastNode!.Value.Danmaku)));
        for (var current = firstNode!; current != lastNode;)
        {
            current = current.Previous!;
            list.Remove(current.Next!);
        }
        list.Remove(lastNode);
        return true;

        bool BottomUpRollCanBePlaced(RollNode? node, float startPos, [NotNullWhen(true)] out (double MostDistance, RollNode LastNode, bool Overlapped)? outNode)
        {
            var intervalLeastDistance = double.PositiveInfinity;
            outNode = null;
            for (; node is not null; node = node.Previous)
            {
                var distance = GetDistance(provider, node.Value.Danmaku);
                // 小于目前最小距离
                if (distance <= mostDistance)
                    return false;
                // 在连续区间取最短距离作为距离
                intervalLeastDistance = Math.Min(intervalLeastDistance, distance);
                if (_layoutHeight <= startPos - (node.Previous?.Value.BottomPos ?? 0))
                {
                    outNode = new()
                    {
                        MostDistance = intervalLeastDistance,
                        LastNode = node,
                        Overlapped = 0 > intervalLeastDistance
                    };
                    // 既没有小于最小距离，也没有超过屏幕高度
                    return true;
                }
            }
            // 超过屏幕
            return false;
        }
    }

    #endregion

    #region Static

    /// <summary>
    /// 自上至下插入弹幕
    /// </summary>
    /// <param name="list"></param>
    /// <param name="occupyTimeMs"></param>
    /// <param name="allowOverlap"></param>
    /// <param name="overlapPredicate"></param>
    /// <returns></returns>
    private bool TopDownStaticDanmaku(DanmakuRoomStaticList list, int occupyTimeMs, bool allowOverlap, Func<bool, bool> overlapPredicate)
    {
        // 目前可用区间中，上条弹幕最短的显示时间
        var leastTime = allowOverlap ? double.PositiveInfinity : TimeMs;
        // 本区间开始位置（上一个区间的结束位置）
        var lastPos = 0f;
        // 插入的第一个区间（靠上）
        StaticNode? firstNode = null;
        // 插入的最后一个区间（靠下）
        StaticNode? lastNode = null;
        // 应该放置的位置（取左上角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.First; current is not null; current = current.Next)
        {
            // 如果允许重叠，循环中一定会进一次该if分支
            if (TopDownStaticCanBePlaced(current, lastPos, out var outNode))
            {
                firstNode = current;
                pos = lastPos;
                (leastTime, lastNode, overlap) = outNode.Value;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
            }
            lastPos = current.Value.BottomPos;
        }

        // 如果重叠了，去判断是否可以重叠
        // 如果firstNode为null则overlap也是默认值true，并且此时一定不允许重叠，故此处一定会为true并且返回
        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos;
        list.AddBefore(firstNode!, new StaticNode((pos + (float)_layoutHeight, occupyTimeMs)));
        for (var current = firstNode!; current != lastNode!;)
        {
            current = current.Next!;
            list.Remove(current.Previous!);
        }
        return true;

        bool TopDownStaticCanBePlaced(StaticNode? node, float startPos, [NotNullWhen(true)] out (double LeastTime, StaticNode LastNode, bool Overlapped)? outNode)
        {
            // 覆盖区间的时间都低于最小的时间，并且不超出屏幕高度。如果返回true，则一定是目前的最优位置
            var intervalMostTime = 0d;
            outNode = null;
            for (; node is not null; node = node.Next)
            {
                // 高于目前最小时间
                if (node.Value.TimeMs >= leastTime)
                    return false;
                intervalMostTime = Math.Max(intervalMostTime, node.Value.TimeMs);
                if (_layoutHeight <= node.Value.BottomPos - startPos)
                {
                    outNode = new()
                    {
                        LeastTime = intervalMostTime,
                        LastNode = node,
                        Overlapped = TimeMs < intervalMostTime
                    };
                    // 既没有高于最小时间，也没有超过屏幕高度
                    return true;
                }
            }
            // 超过屏幕
            return false;
        }
    }

    /// <summary>
    /// 自下至上插入弹幕
    /// </summary>
    /// <param name="list"></param>
    /// <param name="occupyTimeMs"></param>
    /// <param name="overlapPredicate"></param>
    /// <param name="allowOverlap"></param>
    /// <returns></returns>
    private bool BottomUpStaticDanmaku(DanmakuRoomStaticList list, int occupyTimeMs, bool allowOverlap, Func<bool, bool> overlapPredicate)
    {
        // 目前可用区间中，上条弹幕最短的显示时间
        var leastTime = allowOverlap ? double.PositiveInfinity : TimeMs;
        // 插入的第一个区间（靠下）
        StaticNode? firstNode = null;
        // 插入的最后一个区间（靠上）
        StaticNode? lastNode = null;
        // 应该放置的位置（取左下角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.Last; current is not null; current = current.Previous)
        {
            // 本区间开始位置
            var lastPos = current.Value.BottomPos;
            // 如果允许重叠，循环中一定会进一次该if分支
            if (BottomUpStaticCanBePlaced(current, lastPos, out var outNode))
            {
                firstNode = current;
                pos = lastPos;
                (leastTime, lastNode, overlap) = outNode.Value;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
            }
        }

        // 如果重叠了，去判断是否可以重叠
        // 如果firstNode为null则overlap也是默认值true，并且此时一定不允许重叠，故此处一定会为true并且返回
        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos - (float)_layoutHeight;
        list.AddAfter(firstNode!, new StaticNode((pos, occupyTimeMs)));
        list.AddBefore(lastNode!, new StaticNode((_showPositionY, lastNode!.Value.TimeMs)));
        for (var current = firstNode!; current != lastNode;)
        {
            current = current.Previous!;
            list.Remove(current.Next!);
        }
        list.Remove(lastNode);
        return true;

        bool BottomUpStaticCanBePlaced(StaticNode? node, float startPos, [NotNullWhen(true)] out (double LeastTime, StaticNode LastNode, bool Overlapped)? outNode)
        {
            var intervalMostTime = 0d;
            outNode = null;
            for (; node is not null; node = node.Previous)
            {
                // 高于目前最小时间
                if (node.Value.TimeMs >= leastTime)
                    return false;
                intervalMostTime = Math.Max(intervalMostTime, node.Value.TimeMs);
                if (_layoutHeight <= startPos - (node.Previous?.Value.BottomPos ?? 0))
                {
                    outNode = new()
                    {
                        LeastTime = intervalMostTime,
                        LastNode = node,
                        Overlapped = TimeMs < intervalMostTime
                    };
                    // 既没有高于最小时间，也没有超过屏幕高度
                    return true;
                }
            }
            // 超过屏幕
            return false;
        }
    }

    #endregion
}
