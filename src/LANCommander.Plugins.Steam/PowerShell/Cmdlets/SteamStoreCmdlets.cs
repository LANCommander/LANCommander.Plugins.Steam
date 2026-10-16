using System.Management.Automation;
using LANCommander.Steam;
using LANCommander.Steam.Models.SteamCmdNet;
using LANCommander.Steam.Services;

namespace LANCommander.Plugins.Steam.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.Get, "SteamAppInfo")]
[OutputType(typeof(AppInfo))]
public class GetSteamAppInfoCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The Steam application ID to retrieve information for.")]
    [ValidateRange(1u, uint.MaxValue)]
    public uint AppId { get; set; }

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await SteamWebApi.GetAppInfo(AppId);

            if (info != null)
                WriteObject(info);
        }
        catch (Exception ex)
        {
            WriteError(ex, "SteamAppInfoError", AppId);
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SteamWebAssetUri")]
[OutputType(typeof(Uri))]
public class GetSteamWebAssetUriCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam application ID.")]
    public int AppId { get; set; }

    [Parameter(Mandatory = true, Position = 1, HelpMessage = "The type of web asset to retrieve the URI for (e.g. icon, header, capsule artwork).")]
    public WebAssetType WebAssetType { get; set; }

    protected override Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(SteamWebApiService.GetWebAssetUri(AppId, WebAssetType));
        }
        catch (Exception ex)
        {
            WriteError(ex, "GetWebAssetUriError");
        }

        return Task.CompletedTask;
    }
}

[Cmdlet(VerbsCommon.Search, "SteamGames")]
[OutputType(typeof(GameSearchResult))]
public class SearchSteamGamesCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The search term to look for in the Steam store.")]
    public string Keyword { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamWebApi.SearchGamesAsync(Keyword), enumerateCollection: true);
        }
        catch (Exception ex)
        {
            WriteError(ex, "SearchGamesError");
        }
    }
}
