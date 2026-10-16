using Microsoft.Extensions.DependencyInjection;

namespace LANCommander.Plugins.Steam.PowerShell;

/// <summary>
/// The plugin's services, for its cmdlets. PowerShell creates cmdlets itself, so they cannot have
/// services injected.
/// </summary>
internal static class SteamScriptServices
{
    private static IServiceProvider? _services;

    /// <summary>Hands over the host's root service provider.</summary>
    public static void Attach(IServiceProvider services) => _services = services;

    public static T Get<T>() where T : notnull
    {
        var services = _services
            ?? throw new InvalidOperationException("The Steam plugin is not loaded. The Steam cmdlets work only in scripts run by a LANCommander server that has loaded it.");

        return services.GetRequiredService<T>();
    }
}
