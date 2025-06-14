using System;
using System.Numerics;
using System.Text.Json;
using WinUI3Utilities;

namespace DanmakuPlayer.Models;

public record AdvancedDanmaku(
    Vector2 StartPosition,
    Vector2 EndPosition,
    float StartOpacity,
    float EndOpacity,
    int DurationMs,
    string Text,
    float ZFlip,
    float YFlip,
    int ActionTimeMs,
    int DelayTimeMs,
    bool Outline,
    string Font,
    bool LinearAccelerate,
    string Path)
{
    private readonly bool _isPercentage = StartPosition is { X: <= 1, Y: <= 1 } && EndPosition is { X: <= 1, Y: <= 1 };

    public static AdvancedDanmaku Parse(string text)
    {
        using var jsonDocument = JsonDocument.Parse(text);
        var jsonArray = jsonDocument.RootElement;
        var opacity = jsonArray[2].GetString()!.Split('-');
        var path = jsonArray.GetArrayLength() > 14 ? jsonArray[14].GetString()! : "";
        return new(
            new(ParseValue<float>(jsonArray[0]), ParseValue<float>(jsonArray[1])),
            new(ParseValue<float>(jsonArray[7]), ParseValue<float>(jsonArray[8])),
            float.Parse(opacity[0]), float.Parse(opacity[1]),
            (int) (ParseValue<float>(jsonArray[3]) * 1000),
            jsonArray[4].GetString()!,
            ParseValue<float>(jsonArray[5]) * float.Pi / 180,
            ParseValue<float>(jsonArray[6]) * float.Pi / 180,
            ParseValue<int>(jsonArray[9]),
            ParseValue<int>(jsonArray[10]),
            ParseValue<bool>(jsonArray[11]),
            jsonArray[12].GetString()!,
            ParseValue<bool>(jsonArray[13]),
            path
        );

        static T ParseValue<T>(JsonElement jsonElement) where T : struct, IParsable<T>
        {
            if (jsonElement.ValueKind is JsonValueKind.String)
            {
                var s = jsonElement.GetString();
                if (string.IsNullOrEmpty(s))
                    return default;
                return default(T) switch
                {
                    float => T.Parse(s, null),
                    int => T.Parse(s, null),
                    bool => T.TryParse(s, null, out var b) ? b : (T)(object)(int.Parse(s) is not 0),
                    _ => ThrowHelper.ArgumentOutOfRange<T, T>(default)
                };
            }

            return (T)(object) (default(T) switch
            {
                float => jsonElement.GetSingle(),
                int => jsonElement.GetInt32(),
                bool => jsonElement.ValueKind switch
                {
                    JsonValueKind.Number => jsonElement.GetInt32() is not 0,
                    JsonValueKind.False or JsonValueKind.True => jsonElement.GetBoolean(),
                    _ => ThrowHelper.ArgumentOutOfRange<JsonValueKind, T>(jsonElement.ValueKind)
                },
                _ => ThrowHelper.ArgumentOutOfRange<T, T>(default)
            });
        }
    }

    public float GetOpacity(int timeMs)
    {
        return (EndOpacity - StartOpacity) * timeMs / DurationMs + StartOpacity;
    }

    public Vector2 GetPosition(int timeMs, float width, float height)
    {
        var ms = timeMs - DelayTimeMs;
        float x;
        float y;
        switch (ms)
        {
            case <= 0:
                x = StartPosition.X;
                y = StartPosition.Y;
                break;
            case > 0 when ms >= ActionTimeMs:
                x = EndPosition.X;
                y = EndPosition.Y;
                break;
            default:
                var scale = ms / ActionTimeMs;
                if (LinearAccelerate)
                {
                    x = (EndPosition.X - StartPosition.X) * MathF.Pow(scale, 2) + StartPosition.X;
                    y = (EndPosition.Y - StartPosition.Y) * MathF.Pow(scale, 2) + StartPosition.Y;
                }
                else
                {
                    x = (EndPosition.X - StartPosition.X) * (2 * scale - MathF.Pow(scale, 2)) + StartPosition.X;
                    y = (EndPosition.Y - StartPosition.Y) * (2 * scale - MathF.Pow(scale, 2)) + StartPosition.Y;
                }
                break;
        }

        return _isPercentage ? new(x * width, y * height) : new(x, y);
    }
}
