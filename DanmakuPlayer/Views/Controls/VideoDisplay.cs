namespace DanmakuPlayer.Views.Controls;

public record VideoDisplay(string DocumentJsQuery, string VideoJsQuery, double Duration)
{
    public override string ToString() => C.SecondToTime(Duration);

    public string DocumentJsQuery { get; set; } = DocumentJsQuery;

    public string VideoJsQuery { get; set; } = VideoJsQuery;

    public double Duration { get; set; } = Duration;
}
