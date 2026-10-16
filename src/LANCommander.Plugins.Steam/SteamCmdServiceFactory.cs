using LANCommander.Plugins.Steam.Storage;
using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Options;
using LANCommander.Steam.Services;
using Microsoft.Extensions.Logging;

namespace LANCommander.Plugins.Steam;

/// <summary>
/// Creates SteamCMD services that use the plugin's current settings and profiles.
/// </summary>
/// <remarks>
/// A service is cheap and holds the SteamCMD path it was created with, so each user of one (a page,
/// a cmdlet) creates its own. That way a changed path is picked up by the next one, and a script's
/// SteamCMD output goes to the script rather than to the server log.
/// </remarks>
public sealed class SteamCmdServiceFactory(SteamSettingsStore settings, ISteamCmdProfileStore profiles, ILoggerFactory? loggers = null)
{
    public ISteamCmdService Create(ILogger<SteamCmdService>? logger = null)
        => new SteamCmdService(
            new SteamCmdOptions
            {
                ExecutablePath = settings.Current.SteamCmdPath,
                AutoDetectPath = true,
            },
            profiles,
            logger ?? loggers?.CreateLogger<SteamCmdService>());
}
