using System.Management.Automation;
using System.Management.Automation.Host;
using LANCommander.SDK.PowerShell;
using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Services;

namespace LANCommander.Plugins.Steam.PowerShell;

/// <summary>
/// Base for the plugin's cmdlets. Gives each cmdlet its own SteamCMD service, which logs SteamCMD's
/// output to the script's host.
/// </summary>
public abstract class SteamCmdlet : AsyncCmdlet
{
    private PSHostUserInterface? _hostUI;
    private ISteamCmdService? _steamCmd;

    protected ISteamCmdService SteamCmd
        => _steamCmd ??= SteamScriptServices.Get<SteamCmdServiceFactory>()
            .Create(_hostUI is null ? null : new PowerShellHostLogger<SteamCmdService>(_hostUI));

    protected static ISteamWebApiService SteamWebApi => SteamScriptServices.Get<ISteamWebApiService>();

    protected override void BeginProcessing()
    {
        // Read on the pipeline thread; the cmdlet's work runs on another
        _hostUI = Host?.UI;

        base.BeginProcessing();
    }

    protected void WriteError(Exception exception, string errorId, object? target = null)
        => WriteError(new ErrorRecord(exception, errorId, ErrorCategory.OperationStopped, target));
}
