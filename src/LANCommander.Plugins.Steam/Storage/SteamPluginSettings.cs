namespace LANCommander.Plugins.Steam.Storage;

/// <summary>Settings an administrator can change from the plugin's page.</summary>
public sealed record SteamPluginSettings
{
    /// <summary>
    /// Path to the SteamCMD executable. Empty means SteamCMD is looked for in the usual places
    /// whenever it is needed.
    /// </summary>
    public string? SteamCmdPath { get; init; }
}
