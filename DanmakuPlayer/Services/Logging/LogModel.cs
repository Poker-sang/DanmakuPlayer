using System;

namespace DanmakuPlayer.Services.Logging;

public class LogModel
{
    public LogModel(string position, string level, string message, Exception? exception)
    {
        Time = DateTime.Now;
        Position = position;
        Level = level;
        Message = message;
        if (exception is not null)
            Exception = new(exception);
    }

    public DateTime Time { get; }

    public string Position { get; }

    public string Level { get; }

    public string Message { get; }

    public ExceptionModel? Exception { get; }

    public override string ToString() =>
        $"""
         {Time:HH:mm:ss} {Level}
         {Message}
         {Position}
         {Exception?.ToString(1)}
         """;
}
