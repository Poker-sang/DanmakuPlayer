using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace DanmakuPlayer.Views.Controls;

public class VideoLocatorDisplay
{
    public override string ToString() => C.SecondToTime(Duration);

    public ILocator Video { get; set; } = null!;

    public double Duration { get; set; } = -1;

    public static async Task<VideoLocatorDisplay> CreateAsync(ILocator video)
    {
        double duration;
        try
        {
            var durationStr = await video.EvaluateAsync("video => video.duration");
            duration = durationStr!.Value.GetDouble();
        }
        // 可能出现.NET不支持无穷浮点数异常
        catch (ArgumentException)
        {
            duration = 0;
        }

        return new()
        {
            Duration = duration,
            Video = video
        };
    }
}
