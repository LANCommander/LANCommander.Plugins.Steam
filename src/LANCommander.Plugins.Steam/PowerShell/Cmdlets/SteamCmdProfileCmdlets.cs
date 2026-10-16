using System.Management.Automation;
using LANCommander.Steam.Models;

namespace LANCommander.Plugins.Steam.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.Get, "SteamCmdProfile")]
[OutputType(typeof(SteamCmdProfile))]
public class GetSteamCmdProfileCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username to retrieve the profile for.")]
    public string Username { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await SteamCmd.GetProfileAsync(Username);

            if (profile != null)
                WriteObject(profile);
        }
        catch (Exception ex)
        {
            WriteError(ex, "GetProfileError");
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SteamCmdProfiles")]
[OutputType(typeof(SteamCmdProfile))]
public class GetSteamCmdProfilesCmdlet : SteamCmdlet
{
    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamCmd.GetProfilesAsync(), enumerateCollection: true);
        }
        catch (Exception ex)
        {
            WriteError(ex, "GetProfilesError");
        }
    }
}

[Cmdlet(VerbsCommon.Set, "SteamCmdProfile")]
public class SetSteamCmdProfileCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username for the profile.")]
    public string Username { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1, HelpMessage = "The default installation directory for this Steam profile.")]
    public string InstallDirectory { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SteamCmd.SaveProfileAsync(new SteamCmdProfile
            {
                Username = Username,
                InstallDirectory = InstallDirectory,
            });
        }
        catch (Exception ex)
        {
            WriteError(ex, "SaveProfileError");
        }
    }
}

[Cmdlet(VerbsCommon.Remove, "SteamCmdProfile")]
public class RemoveSteamCmdProfileCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username whose profile should be deleted.")]
    public string Username { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SteamCmd.DeleteProfileAsync(Username);
        }
        catch (Exception ex)
        {
            WriteError(ex, "DeleteProfileError");
        }
    }
}
