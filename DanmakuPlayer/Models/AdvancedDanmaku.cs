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
    float Duration,
    string Text,
    float ZFlip,
    float YFlip,
    float ActionTime,
    float DelayTime,
    bool Outline,
    string Font,
    bool LinearAccelerate,
    string Path
)
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
            ParseValue<float>(jsonArray[3]),
            jsonArray[4].GetString()!,
            ParseValue<float>(jsonArray[5]) * float.Pi / 180,
            ParseValue<float>(jsonArray[6]) * float.Pi / 180,
            ParseValue<float>(jsonArray[9]) / 1000,
            ParseValue<float>(jsonArray[10]) / 1000,
            ParseValue<bool>(jsonArray[11]),
            jsonArray[12].GetString()!,
            ParseValue<bool>(jsonArray[13]),
            path
        );

        static T ParseValue<T>(JsonElement jsonElement) where T : struct
        {
            if (jsonElement.ValueKind is JsonValueKind.String)
            {
                var s = jsonElement.GetString();
                if (string.IsNullOrEmpty(s))
                    return default;
                return default(T) switch
                {
                    float => (T)(object)float.Parse(s),
                    int => (T)(object)int.Parse(s),
                    bool => (T)(object)(bool.TryParse(s, out var b) ? b : int.Parse(s) is 1),
                    _ => ThrowHelper.ArgumentOutOfRange<T, T>(default)
                };
            }

            return default(T) switch
            {
                float => (T)(object)jsonElement.GetSingle(),
                int => (T)(object)jsonElement.GetInt32(),
                bool => (T)(object)(jsonElement.ValueKind switch
                {
                    JsonValueKind.Number => jsonElement.GetInt32() is 1,
                    JsonValueKind.False or JsonValueKind.True => jsonElement.GetBoolean(),
                    _ => ThrowHelper.ArgumentOutOfRange<JsonValueKind, T>(jsonElement.ValueKind)
                }),
                _ => ThrowHelper.ArgumentOutOfRange<T, T>(default)
            };
        }
    }

    public float GetOpacity(float time)
    {
        return (EndOpacity - StartOpacity) * time / Duration + StartOpacity;
    }

    public Vector2 GetPosition(float time, float width, float height)
    {
        var t = time - DelayTime;
        float x;
        float y;
        switch (t)
        {
            case <= 0:
                x = StartPosition.X;
                y = StartPosition.Y;
                break;
            case > 0 when t >= ActionTime:
                x = EndPosition.X;
                y = EndPosition.Y;
                break;
            default:
                var scale = t / ActionTime;
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
