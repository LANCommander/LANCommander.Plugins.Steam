using System.Management.Automation;
using System.Management.Automation.Runspaces;
using LANCommander.Plugins.Steam.Storage;
using LANCommander.SDK.PowerShell;
using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LANCommander.Plugins.Steam.Tests;

/// <summary>
/// Runs the cmdlets in the runspace the SDK builds for scripts, with the plugin registered the way
/// the server registers it.
/// </summary>
// The cmdlets find the plugin's services through a static, so these cannot run alongside each other
[Collection(nameof(CmdletTests))]
public class CmdletTests : IDisposable
{
    private readonly TempDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly Runspace _runspace;

    public CmdletTests()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        SteamPlugin.AddSteamServices(services, _ => _directory.PluginPaths);

        _services = services.BuildServiceProvider();

        _runspace = new PowerShellRunspaceBuilder(_services, NullLogger.Instance).Open();
    }

    public void Dispose()
    {
        _runspace.Dispose();
        _services.Dispose();
        _directory.Dispose();
    }

    [Fact]
    public void EveryCmdletIsAvailableToScripts()
    {
        var commands = Invoke("Get-Command -Noun Steam* | Select-Object -ExpandProperty Name").Select(o => o.ToString()).ToHashSet();

        foreach (var name in new[]
                 {
                     "Connect-SteamCmd", "Disconnect-SteamCmd", "Get-SteamAppInfo", "Get-SteamCmdConnectionStatus",
                     "Get-SteamCmdPath", "Get-SteamCmdProfile", "Get-SteamCmdProfiles", "Get-SteamWebAssetUri",
                     "Install-SteamContent", "Remove-SteamCmdProfile", "Remove-SteamContent", "Search-SteamGames",
                     "Set-SteamCmdProfile",
                 })
            Assert.Contains(name, commands);
    }

    [Fact]
    public async Task ScriptsManageTheSameProfilesAsThePage()
    {
        Invoke(@"Set-SteamCmdProfile -Username alice -InstallDirectory 'D:\Steam\alice'");
        Invoke(@"Set-SteamCmdProfile bob 'D:\Steam\bob'");

        var store = _services.GetRequiredService<ISteamCmdProfileStore>();

        Assert.Equal(["alice", "bob"], (await store.GetAllAsync()).Select(p => p.Username));

        var profile = Assert.IsType<SteamCmdProfile>(Assert.Single(Invoke("Get-SteamCmdProfile alice")).BaseObject);

        Assert.Equal(@"D:\Steam\alice", profile.InstallDirectory);

        Invoke("Remove-SteamCmdProfile alice");

        Assert.Equal(["bob"], Invoke("Get-SteamCmdProfiles").Select(o => ((SteamCmdProfile)o.BaseObject).Username));
    }

    [Fact]
    public void WebAssetUrisPointAtSteamsStore()
    {
        var uri = Assert.IsType<Uri>(Assert.Single(Invoke("Get-SteamWebAssetUri -AppId 440 -WebAssetType LibraryCover")).BaseObject);

        Assert.EndsWith("/apps/440/library_600x900.jpg", uri.AbsoluteUri);
    }

    [Fact]
    public void InstallingWithoutSteamCmdIsAnError()
    {
        _services.GetRequiredService<SteamSettingsStore>().Save(new SteamPluginSettings
        {
            SteamCmdPath = Path.Combine(_directory.Path, "missing", "steamcmd.exe"),
        });

        using var shell = System.Management.Automation.PowerShell.Create();

        shell.Runspace = _runspace;
        shell.AddScript($"Install-SteamContent -AppId 90 -InstallDirectory '{Path.Combine(_directory.Path, "hlds")}'");

        Assert.Empty(shell.Invoke());

        var error = Assert.Single(shell.Streams.Error);

        Assert.IsType<FileNotFoundException>(error.Exception);
    }

    private IReadOnlyList<PSObject> Invoke(string script)
    {
        using var shell = System.Management.Automation.PowerShell.Create();

        shell.Runspace = _runspace;
        shell.AddScript(script);

        var output = shell.Invoke();

        Assert.False(shell.HadErrors, string.Join(Environment.NewLine, shell.Streams.Error.Select(e => e.ToString())));

        return output;
    }
}
