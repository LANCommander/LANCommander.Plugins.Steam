using LANCommander.Plugins.Steam.Storage;
using Microsoft.Extensions.Configuration;

namespace LANCommander.Plugins.Steam.Tests;

/// <summary>A scratch directory, deleted afterwards.</summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lancommander-steam-plugin-test-{Guid.NewGuid():N}");

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public SteamPluginPaths PluginPaths => new(System.IO.Path.Combine(Path, "state"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A plugin load context cannot be unloaded, so the assemblies it loaded stay locked
            // until the test process exits.
        }
    }
}

public static class TestConfiguration
{
    /// <summary>Server settings as they were before SteamCMD moved into the plugin.</summary>
    public static IConfiguration Host(string? path = null, params (string Username, string InstallDirectory)[] profiles)
    {
        var values = new Dictionary<string, string?>();

        if (path is not null)
            values["Steam:Path"] = path;

        for (var i = 0; i < profiles.Length; i++)
        {
            values[$"Steam:Profiles:{i}:Username"] = profiles[i].Username;
            values[$"Steam:Profiles:{i}:InstallDirectory"] = profiles[i].InstallDirectory;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
