#region Copyright

// GPL v3 License
// 
// DanmakuPlayer/DanmakuPlayer
// Copyright (c) 2024 DanmakuPlayer/VideoLocatorDisplay.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Threading.Tasks;
using DanmakuPlayer.Views.Converters;
using Microsoft.Playwright;

namespace DanmakuPlayer.Views.Controls;

public class VideoLocatorDisplay
{
    public override string ToString() => C.ToTime(Duration);

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
