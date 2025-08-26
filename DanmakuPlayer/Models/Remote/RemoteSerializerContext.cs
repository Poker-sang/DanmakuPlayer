using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DanmakuPlayer.Models.Remote;

[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(CurrentStatus))]
[JsonSerializable(typeof(LoginInfo))]
[JsonSerializable(typeof(RemoteStatus))]
[JsonSerializable(typeof(IList<RoomInfo>))]
[JsonSerializable(typeof(RoomInfo))]
[JsonSerializable(typeof(IReadOnlyList<UserInfo>))]
[JsonSerializable(typeof(UserInfo))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
public partial class RemoteSerializerContext : JsonSerializerContext;
