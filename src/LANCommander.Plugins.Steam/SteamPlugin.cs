using System.Reflection;
using LANCommander.Plugins.Steam.Media;
using LANCommander.Plugins.Steam.PowerShell;
using LANCommander.Plugins.Steam.Storage;
using LANCommander.SDK.Plugins;
using LANCommander.Server.Plugins;
using LANCommander.Steam;
using LANCommander.Steam.Abstractions;
using LANCommander.Steam.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LANCommanderPlugin(
    typeof(LANCommander.Plugins.Steam.SteamPlugin),
    Id = LANCommander.Plugins.Steam.SteamPlugin.PluginId,
    // Steam left the core in 2.2.0, and navigation sections and media sources arrived with it; -0
    // admits its prereleases
    MinHostVersion = "2.2.0-0",
    Hosts = PluginHost.Server)]

namespace LANCommander.Plugins.Steam;

/// <summary>
/// Adds Steam to the server: SteamCMD with the accounts it signs in with and a page to manage them,
/// the SteamCMD and store cmdlets for scripts, and the Steam store as a source in the media search.
/// </summary>
public sealed class SteamPlugin : IPlugin
{
    public const string PluginId = "lancommander.steam";

    public string Id => PluginId;
    public string Name => "Steam";
    public string Version => typeof(SteamPlugin).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    public string Author => "LANCommander";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IServerRouteAssemblyExtension>(new SteamRouteAssembly());
        services.AddSingleton<IServerNavigationExtension, SteamNavigation>();

        AddSteamServices(services, _ => SteamPluginPaths.ForHost());
    }

    public Task InitializeAsync(PluginContext context, CancellationToken cancellationToken)
    {
        var settings = context.Services.GetRequiredService<SteamSettingsStore>();

        context.Logger.LogInformation(
            "Steam plugin keeps its state in {Root}; SteamCMD is {Path}",
            context.Services.GetRequiredService<SteamPluginPaths>().Root,
            settings.Current.SteamCmdPath ?? "not configured, and is looked for when needed");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers everything the plugin needs that does not come from the host, so the tests can
    /// build the same container around a scratch directory.
    /// </summary>
    internal static void AddSteamServices(IServiceCollection services, Func<IServiceProvider, SteamPluginPaths> paths)
    {
        services.AddSingleton(paths);
        services.AddSingleton(provider => new SteamSettingsStore(
            provider.GetRequiredService<SteamPluginPaths>(),
            provider.GetService<IConfiguration>()));
        services.AddSingleton<ISteamCmdProfileStore>(provider => new SteamCmdProfileStore(
            provider.GetRequiredService<SteamPluginPaths>(),
            provider.GetService<IConfiguration>()));
        services.AddSingleton(provider => new SteamCmdServiceFactory(
            provider.GetRequiredService<SteamSettingsStore>(),
            provider.GetRequiredService<ISteamCmdProfileStore>(),
            provider.GetService<ILoggerFactory>()));

        services.AddHttpClient<ISteamWebApiService, SteamWebApiService>();

        services.AddSingleton<SteamClient>();
        services.AddSingleton<IMediaGrabberExtension, SteamMediaGrabber>();

        services.AddSingleton<IPluginPowerShellExtension, SteamPowerShellExtension>();
    }

    private sealed class SteamRouteAssembly : IServerRouteAssemblyExtension
    {
        public Assembly Assembly => typeof(SteamPlugin).Assembly;
    }

    private sealed class SteamNavigation : IServerNavigationExtension
    {
        public string Id => $"{PluginId}.steamcmd";
        public string Label => "SteamCMD";
        public string Href => SteamRoutes.SteamCmd;
        public string? Icon => "SteamLogo";
        public string? Section => ServerNavigationSection.Integrations;
    }
}

/// <summary>The plugin's pages.</summary>
public static class SteamRoutes
{
    public const string SteamCmd = "/Plugins/Steam/SteamCMD";
}
