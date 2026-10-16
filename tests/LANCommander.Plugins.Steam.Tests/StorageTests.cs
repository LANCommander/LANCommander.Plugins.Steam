using LANCommander.Plugins.Steam.Storage;
using LANCommander.Steam.Models;

namespace LANCommander.Plugins.Steam.Tests;

public class StorageTests : IDisposable
{
    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task ProfilesSurviveARestart()
    {
        var store = new SteamCmdProfileStore(_directory.PluginPaths);

        await store.SaveAsync(new SteamCmdProfile { Username = "alice", InstallDirectory = @"D:\Steam\alice" });
        await store.SaveAsync(new SteamCmdProfile { Username = "bob", InstallDirectory = @"D:\Steam\bob" });

        var reopened = new SteamCmdProfileStore(_directory.PluginPaths);

        Assert.Equal(["alice", "bob"], (await reopened.GetAllAsync()).Select(p => p.Username));
        Assert.Equal(@"D:\Steam\bob", (await reopened.GetByUsernameAsync("bob"))?.InstallDirectory);
    }

    [Fact]
    public async Task SavingAProfileAgainUpdatesItWhateverTheUsernamesCase()
    {
        var store = new SteamCmdProfileStore(_directory.PluginPaths);

        await store.SaveAsync(new SteamCmdProfile { Username = "Alice", InstallDirectory = "old" });
        await store.SaveAsync(new SteamCmdProfile { Username = "alice", InstallDirectory = "new" });

        var profile = Assert.Single(await store.GetAllAsync());

        Assert.Equal("new", profile.InstallDirectory);

        await store.DeleteAsync("ALICE");

        Assert.Empty(await new SteamCmdProfileStore(_directory.PluginPaths).GetAllAsync());
    }

    [Fact]
    public async Task EditingAReturnedProfileDoesNotChangeTheStoredOne()
    {
        var store = new SteamCmdProfileStore(_directory.PluginPaths);

        await store.SaveAsync(new SteamCmdProfile { Username = "alice", InstallDirectory = "kept" });

        (await store.GetByUsernameAsync("alice"))!.InstallDirectory = "changed";

        Assert.Equal("kept", (await store.GetByUsernameAsync("alice"))?.InstallDirectory);
    }

    [Fact]
    public async Task ProfilesStartFromTheServersSettingsUntilThePluginSavesItsOwn()
    {
        var configuration = TestConfiguration.Host(profiles: [("alice", @"D:\Steam")]);
        var store = new SteamCmdProfileStore(_directory.PluginPaths, configuration);

        Assert.Equal("alice", Assert.Single(await store.GetAllAsync()).Username);

        await store.DeleteAsync("alice");

        // Once the plugin has a file of its own, the server's settings no longer count
        Assert.Empty(await new SteamCmdProfileStore(_directory.PluginPaths, configuration).GetAllAsync());
    }

    [Fact]
    public void TheSteamCmdPathStartsFromTheServersSettingsUntilThePluginSavesItsOwn()
    {
        var configuration = TestConfiguration.Host(path: @"C:\steamcmd\steamcmd.exe");

        Assert.Equal(@"C:\steamcmd\steamcmd.exe", new SteamSettingsStore(_directory.PluginPaths, configuration).Current.SteamCmdPath);

        new SteamSettingsStore(_directory.PluginPaths, configuration).Save(new SteamPluginSettings { SteamCmdPath = "  " });

        Assert.Null(new SteamSettingsStore(_directory.PluginPaths, configuration).Current.SteamCmdPath);
    }

    [Fact]
    public void ThereAreNoSettingsWithoutAFileOrTheServersSettings()
        => Assert.Null(new SteamSettingsStore(_directory.PluginPaths).Current.SteamCmdPath);
}
