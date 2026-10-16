using System.Text.Json.Serialization;
using LANCommander.Plugins.Steam.Storage;
using LANCommander.Steam.Models;

namespace LANCommander.Plugins.Steam;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SteamPluginSettings))]
[JsonSerializable(typeof(List<SteamCmdProfile>))]
internal sealed partial class SteamJsonContext : JsonSerializerContext;
