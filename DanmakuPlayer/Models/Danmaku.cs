using System.Numerics;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using Microsoft.Graphics.Canvas;
using WinUI3Utilities;

namespace DanmakuPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Text">内容</param>
/// <param name="Time">出现时间</param>
/// <param name="Mode">模式</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="UnixTimeStamp">发送时间戳</param>
/// <param name="Pool">所属弹幕池</param>
/// <param name="UserHash">用户ID</param>
public partial record Danmaku(
    string Text,
    float Time,
    DanmakuMode Mode,
    int Size,
    uint Color,
    ulong UnixTimeStamp,
    DanmakuPool Pool,
    string UserHash) : IDanmakuWidth
{
    public double LayoutWidth { get; set; }

    private double _layoutHeight;

    /// <summary>
    /// 静止弹幕显示的位置
    /// </summary>
    private Vector2 _staticPosition;

    /// <summary>
    /// 弹幕显示的Y坐标
    /// </summary>
    private float _showPositionY;

    /// <summary>
    /// 是否需要渲染（取决于是否允许重叠）
    /// </summary>
    public bool NeedRender { get; private set; } = true;

    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <returns>是否需要渲染</returns>
    public bool RenderInit(DanmakuContext context, CreatorProvider provider)
    {
        var layoutExists = provider.Layouts.ContainsKey(ToString());
        var layout = layoutExists ? provider.Layouts[ToString()] : provider.GetNewLayout(this);
        LayoutWidth = layout.LayoutBounds.Width;
        _layoutHeight = layout.LayoutBounds.Height;

        NeedRender = false;
        switch (Mode)
        {
            case DanmakuMode.Roll:
                if (!TopDownRollDanmaku(provider, context.RollRoom, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Bottom:
                if (!BottomUpStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(provider.ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Top:
                if (!TopDownStaticDanmaku(context.StaticRoom, provider.AppConfig.DanmakuDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(provider.ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Inverse:
                if (!TopDownRollDanmaku(provider, context.InverseRoom, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }

        if (!layoutExists)
            provider.Layouts.Add(ToString(), layout);
        NeedRender = true;
        return true;

        // 如果覆盖了，并且不允许覆盖，则返回true，否则false
        // 本函数所有分支都会调用本本地函数
        bool OverlapPredicate(bool overlap)
        {
            // 是否覆盖
            if (!overlap || provider.AppConfig.DanmakuAllowOverlap)
                return false;
            if (!layoutExists)
                layout.Dispose();
            return true;
        }
    }

    public void OnRender(CanvasDrawingSession renderTarget, CreatorProvider provider, float time)
    {
        // 外部实现逻辑：if (Time <= time && time - provider.AppConfig.Speed < Time)
        if (!NeedRender)
            return;
        
        var layout = provider.Layouts[ToString()];
        var width = layout.LayoutBounds.Width;
        var color = provider.GetBrush(Color);
        switch (Mode)
        {
            case DanmakuMode.Roll:
                renderTarget.DrawTextLayout(layout, new((float)(provider.ViewWidth - ((provider.ViewWidth + width) * (time - Time) / provider.AppConfig.DanmakuDuration)), _showPositionY), color);
                break;
            case DanmakuMode.Bottom:
            case DanmakuMode.Top:
                renderTarget.DrawTextLayout(layout, _staticPosition, color);
                break;
            case DanmakuMode.Inverse:
                renderTarget.DrawTextLayout(layout, new((float)(((provider.ViewWidth + width) * (time - Time) / provider.AppConfig.DanmakuDuration) - width), _showPositionY), color);
                break;
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }
    }

    public override string ToString() => $"{Text},{Color},{Size}";
}
