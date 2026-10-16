using LANCommander.SDK;

namespace LANCommander.Plugins.Steam.Storage;

/// <summary>
/// Where the plugin keeps its own state.
/// </summary>
/// <remarks>
/// The plugin's drop-in folder is replaced wholesale on every deploy, so nothing the plugin writes
/// lives there. Its state sits under the host's data directory instead, beside the server's own.
/// </remarks>
public sealed class SteamPluginPaths(string root)
{
    /// <summary>Paths rooted in the host's data directory.</summary>
    public static SteamPluginPaths ForHost() => new(AppPaths.GetConfigPath("PluginData", "Steam"));

    public string Root { get; } = Path.GetFullPath(root);

    public string Settings => Path.Combine(Root, "settings.json");

    public string Profiles => Path.Combine(Root, "profiles.json");
}
