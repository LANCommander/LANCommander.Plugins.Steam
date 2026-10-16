using Microsoft.Extensions.Configuration;

namespace LANCommander.Plugins.Steam.Storage;

/// <summary>Holds the plugin's settings and persists them.</summary>
/// <remarks>
/// Until the plugin has saved settings of its own, it starts from the SteamCMD path in the server's
/// settings (<c>Steam:Path</c>), where the server kept it before SteamCMD moved into this plugin.
/// </remarks>
public sealed class SteamSettingsStore(SteamPluginPaths paths, IConfiguration? configuration = null)
{
    private readonly Lock _lock = new();
    private SteamPluginSettings? _settings;

    public SteamPluginSettings Current
    {
        get
        {
            lock (_lock)
                return _settings ??= JsonFile.Read(paths.Settings, SteamJsonContext.Default.SteamPluginSettings)
                                     ?? FromHost();
        }
    }

    public void Save(SteamPluginSettings settings)
    {
        var normalized = settings with
        {
            SteamCmdPath = string.IsNullOrWhiteSpace(settings.SteamCmdPath) ? null : settings.SteamCmdPath.Trim(),
        };

        lock (_lock)
        {
            JsonFile.Write(paths.Settings, normalized, SteamJsonContext.Default.SteamPluginSettings);

            _settings = normalized;
        }
    }

    private SteamPluginSettings FromHost()
    {
        var path = configuration?["Steam:Path"];

        return new SteamPluginSettings { SteamCmdPath = string.IsNullOrWhiteSpace(path) ? null : path };
    }
}
