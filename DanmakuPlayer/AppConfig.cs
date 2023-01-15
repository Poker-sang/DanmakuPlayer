using DanmakuPlayer.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer;

[GenerateConstructor]
public partial record AppConfig
{
    #region 应用设置

    private int _theme;

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0</remarks>
    public int Theme
    {
        get => _theme;
        set
        {
            if (_theme == value)
                return;

            var selectedTheme = value switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (CurrentContext.Window.Content is FrameworkElement rootElement)
                rootElement.RequestedTheme = selectedTheme;

            CurrentContext.App.Resources["WindowCaptionForeground"] = selectedTheme switch
            {
                ElementTheme.Dark => Colors.White,
                ElementTheme.Light => Colors.Black,
                _ => CurrentContext.App.RequestedTheme is ApplicationTheme.Dark ? Colors.White : Colors.Black
            };

            _theme = value;

            AppContext.SaveConfiguration(this);
            _theme = value;
        }
    }

    #endregion

    #region 播放设置

    private double _playSpeed = 1;
    private int _playFramePerSecond = 25;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    /// <remarks>default: 1</remarks>
    public double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            if (Equals(_playSpeed, value))
                return;
            _playSpeed = value;
            AppContext.ResetTimerInterval();
        }
    }

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    /// <remarks>default: 5</remarks>
    public int PlayFastForward { get; set; } = 5;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>PlayFramePerSecond ∈ [5, 100], default: 25</remarks>
    public int PlayFramePerSecond
    {
        get => _playFramePerSecond;
        set
        {
            if (Equals(_playFramePerSecond, value))
                return;
            _playFramePerSecond = value;
            AppContext.ResetTimerInterval();
        }
    }

    #endregion

    #region 弹幕设置

    private float _danmakuOpacity = 0.7f;
    private bool _danmakuAllowOverlap;
    private string _danmakuFont = "微软雅黑";
    private float _danmakuScale = 1;

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>DanmakuDuration ∈ [5, 20], default: 15</remarks>
    public float DanmakuDuration { get; set; } = 15;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ [0.1, 1], default: 0.7</remarks>
    public float DanmakuOpacity
    {
        get => _danmakuOpacity;
        set
        {
            if (Equals(_danmakuOpacity, value))
                return;
            _danmakuOpacity = value;
            AppContext.DanmakuCanvas.Opacity = _danmakuOpacity;
        }
    }

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuAllowOverlap
    {
        get => _danmakuAllowOverlap;
        set
        {
            if (Equals(_danmakuAllowOverlap, value))
                return;
            _danmakuAllowOverlap = value;
            AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadProvider);
        }
    }

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: "微软雅黑"</remarks>
    public string DanmakuFont
    {
        get => _danmakuFont;
        set
        {
            if (Equals(_danmakuFont, value))
                return;
            _danmakuFont = value;
            AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadFormat);
        }
    }

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>DanmakuScale ∈ [0.5, 2], default: 1</remarks>
    public float DanmakuScale
    {
        get => _danmakuScale;
        set
        {
            if (Equals(_danmakuScale, value))
                return;
            _danmakuScale = value;
            AppContext.BackgroundPanel.DanmakuReload(RenderType.ReloadProvider);
        }
    }

    #endregion

    public AppConfig()
    {

    }
}
