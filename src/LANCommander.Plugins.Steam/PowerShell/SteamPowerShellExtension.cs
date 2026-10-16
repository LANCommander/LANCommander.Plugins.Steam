using LANCommander.Plugins.Steam.PowerShell.Cmdlets;
using LANCommander.SDK.Plugins;

namespace LANCommander.Plugins.Steam.PowerShell;

/// <summary>
/// Adds the SteamCMD and Steam store cmdlets to the runspace LANCommander scripts run in.
/// </summary>
/// <remarks>
/// Cmdlets are created by PowerShell rather than by the container, so they reach the plugin's
/// services through <see cref="SteamScriptServices"/>. The runspace resolves this extension before
/// it runs anything, which is when the services are handed over.
/// </remarks>
public sealed class SteamPowerShellExtension : IPluginPowerShellExtension
{
    internal static readonly Type[] CmdletTypes =
    [
        typeof(ConnectSteamCmdCmdlet),
        typeof(DisconnectSteamCmdCmdlet),
        typeof(GetSteamAppInfoCmdlet),
        typeof(GetSteamCmdConnectionStatusCmdlet),
        typeof(GetSteamCmdPathCmdlet),
        typeof(GetSteamCmdProfileCmdlet),
        typeof(GetSteamCmdProfilesCmdlet),
        typeof(GetSteamWebAssetUriCmdlet),
        typeof(InstallSteamContentCmdlet),
        typeof(RemoveSteamCmdProfileCmdlet),
        typeof(RemoveSteamContentCmdlet),
        typeof(SearchSteamGamesCmdlet),
        typeof(SetSteamCmdProfileCmdlet),
    ];

    public SteamPowerShellExtension(IServiceProvider services) => SteamScriptServices.Attach(services);

    public IEnumerable<Type> GetCmdletTypes() => CmdletTypes;

    public IEnumerable<string> GetModulePaths() => [];
}
