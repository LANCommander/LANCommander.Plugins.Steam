using Bunit;
using LANCommander.Plugins.Steam.Pages;
using LANCommander.Plugins.Steam.Storage;
using LANCommander.Server.UI.Extensions;
using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LANCommander.Plugins.Steam.Tests;

public class SteamCmdPageTests : BunitContext
{
    private readonly TempDirectory _directory = new();

    public SteamCmdPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddLANCommanderServerUI();
        Services.AddLogging();

        SteamPlugin.AddSteamServices(Services, _ => _directory.PluginPaths);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _directory.Dispose();
    }

    [Fact]
    public void ThePageOffersANewProfileWhenThereAreNone()
    {
        var page = Render<SteamCmdPage>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("SteamCMD Path", page.Markup);
            Assert.Contains("New Profile", page.Markup);
        });
    }

    [Fact]
    public async Task SavingKeepsThePathAndTheSignedInProfiles()
    {
        await Services.GetRequiredService<ISteamCmdProfileStore>()
            .SaveAsync(new SteamCmdProfile { Username = "alice", InstallDirectory = "before" });

        var page = Render<SteamCmdPage>();

        page.WaitForAssertion(() => Assert.Contains("alice", page.Markup));

        page.Find("input[placeholder='steamcmd']").Change(@"C:\steamcmd\steamcmd.exe");
        page.FindAll("button").First(button => button.TextContent.Trim() == "Save").Click();

        page.WaitForAssertion(() =>
            Assert.Equal(@"C:\steamcmd\steamcmd.exe", new SteamSettingsStore(_directory.PluginPaths).Current.SteamCmdPath));

        Assert.Equal("alice", Assert.Single(await new SteamCmdProfileStore(_directory.PluginPaths).GetAllAsync()).Username);
    }
}
