using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Models;
using Microsoft.Extensions.Configuration;

namespace LANCommander.Plugins.Steam.Storage;

/// <summary>
/// Keeps the Steam accounts SteamCMD has signed in to, and the directory each installs into by
/// default.
/// </summary>
/// <remarks>
/// SteamCMD keeps the sign-in itself; a profile only remembers which accounts were set up. Until
/// the plugin has saved profiles of its own, it starts from those in the server's settings
/// (<c>Steam:Profiles</c>), where scripts kept them before SteamCMD moved into this plugin.
/// </remarks>
public sealed class SteamCmdProfileStore(SteamPluginPaths paths, IConfiguration? configuration = null) : ISteamCmdProfileStore
{
    private readonly Lock _lock = new();
    private List<SteamCmdProfile>? _profiles;

    public Task<IEnumerable<SteamCmdProfile>> GetAllAsync()
    {
        lock (_lock)
            return Task.FromResult<IEnumerable<SteamCmdProfile>>(Profiles.Select(Copy).ToList());
    }

    public Task<SteamCmdProfile?> GetByUsernameAsync(string username)
    {
        lock (_lock)
        {
            var profile = Find(username);

            return Task.FromResult(profile is null ? null : Copy(profile));
        }
    }

    public Task SaveAsync(SteamCmdProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Username);

        lock (_lock)
        {
            var profiles = Profiles.ToList();
            var index = profiles.FindIndex(p => Matches(p, profile.Username));

            if (index >= 0)
                profiles[index] = Copy(profile);
            else
                profiles.Add(Copy(profile));

            Write(profiles);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string username)
    {
        lock (_lock)
        {
            var profiles = Profiles.Where(p => !Matches(p, username)).ToList();

            if (profiles.Count != Profiles.Count)
                Write(profiles);
        }

        return Task.CompletedTask;
    }

    private List<SteamCmdProfile> Profiles
        => _profiles ??= JsonFile.Read(paths.Profiles, SteamJsonContext.Default.ListSteamCmdProfile) ?? FromHost();

    private SteamCmdProfile? Find(string username) => Profiles.FirstOrDefault(p => Matches(p, username));

    private void Write(List<SteamCmdProfile> profiles)
    {
        JsonFile.Write(paths.Profiles, profiles, SteamJsonContext.Default.ListSteamCmdProfile);

        _profiles = profiles;
    }

    private List<SteamCmdProfile> FromHost()
    {
        if (configuration is null)
            return [];

        return configuration.GetSection("Steam:Profiles").GetChildren()
            .Select(section => new SteamCmdProfile
            {
                Username = section["Username"] ?? string.Empty,
                InstallDirectory = section["InstallDirectory"] ?? string.Empty,
            })
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Username))
            .ToList();
    }

    // Steam account names are not case sensitive
    private static bool Matches(SteamCmdProfile profile, string username)
        => string.Equals(profile.Username, username, StringComparison.OrdinalIgnoreCase);

    // Callers get copies, so editing one does not change what is stored until it is saved
    private static SteamCmdProfile Copy(SteamCmdProfile profile)
        => new() { Username = profile.Username, InstallDirectory = profile.InstallDirectory };
}
