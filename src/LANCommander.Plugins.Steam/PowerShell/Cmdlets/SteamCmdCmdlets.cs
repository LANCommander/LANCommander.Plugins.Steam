using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using LANCommander.Steam.Enums;
using LANCommander.Steam.Events;
using LANCommander.Steam.Models;

namespace LANCommander.Plugins.Steam.PowerShell.Cmdlets;

[Cmdlet(VerbsCommunications.Connect, "SteamCmd")]
[OutputType(typeof(SteamCmdStatus))]
public class ConnectSteamCmdCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username to log in with.")]
    public string Username { get; set; } = string.Empty;

    [Parameter(Mandatory = false, HelpMessage = "The password for the Steam account. If omitted, SteamCMD uses the sign-in it has cached for the account.")]
    public SecureString? Password { get; set; }

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamCmd.LoginToSteamAsync(Username, ToPlainText(Password)));
        }
        catch (Exception ex)
        {
            WriteError(ex, "LoginError");
        }
    }

    private static string? ToPlainText(SecureString? value)
    {
        if (value is null)
            return null;

        var pointer = Marshal.SecureStringToBSTR(value);

        try
        {
            return Marshal.PtrToStringBSTR(pointer);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(pointer);
        }
    }
}

[Cmdlet(VerbsCommunications.Disconnect, "SteamCmd")]
[OutputType(typeof(SteamCmdStatus))]
public class DisconnectSteamCmdCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username to log out.")]
    public string Username { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamCmd.LogoutAsync(Username));
        }
        catch (Exception ex)
        {
            WriteError(ex, "LogoutError");
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SteamCmdConnectionStatus")]
[OutputType(typeof(SteamCmdConnectionStatus))]
public class GetSteamCmdConnectionStatusCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam account username to check the connection status for.")]
    public string Username { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamCmd.GetConnectionStatusAsync(Username));
        }
        catch (Exception ex)
        {
            WriteError(ex, "GetConnectionStatusError");
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SteamCmdPath")]
[OutputType(typeof(string))]
public class GetSteamCmdPathCmdlet : SteamCmdlet
{
    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = await SteamCmd.AutoDetectSteamCmdPathAsync();

            if (!string.IsNullOrEmpty(path))
                WriteObject(path);
        }
        catch (Exception ex)
        {
            WriteError(ex, "AutoDetectPathError");
        }
    }
}

[Cmdlet(VerbsLifecycle.Install, "SteamContent")]
[OutputType(typeof(SteamCmdInstallJob))]
public class InstallSteamContentCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Steam application ID to install.")]
    public uint AppId { get; set; }

    [Parameter(Mandatory = true, Position = 1, HelpMessage = "The directory path where the content should be installed.")]
    public string InstallDirectory { get; set; } = string.Empty;

    [Parameter(Mandatory = false, HelpMessage = "The Steam account username to use for downloading. If omitted, the anonymous account is used.")]
    public string? Username { get; set; }

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        // The service reports a failed install through its events rather than by throwing, and it
        // is this cmdlet's own, so every event is about this install.
        SteamCmdInstallJob? finished = null;

        void OnProgress(object? sender, SteamCmdInstallProgressEventArgs e)
            => WriteProgress(new ProgressRecord(0, $"Installing Steam app {AppId}", e.StatusMessage)
            {
                PercentComplete = (int)Math.Clamp(e.Progress, 0, 100),
            });

        void OnStatusChanged(object? sender, SteamCmdInstallStatusEventArgs e)
        {
            if (e.Status is SteamCmdInstallStatus.Completed or SteamCmdInstallStatus.Failed or SteamCmdInstallStatus.Cancelled)
                finished = e.Job;
        }

        var steamCmd = SteamCmd;

        steamCmd.InstallProgress += OnProgress;
        steamCmd.InstallStatusChanged += OnStatusChanged;

        try
        {
            await steamCmd.InstallContentAsync(AppId, InstallDirectory, Username);
        }
        catch (Exception ex)
        {
            WriteError(ex, "InstallContentError", AppId);

            return;
        }
        finally
        {
            steamCmd.InstallProgress -= OnProgress;
            steamCmd.InstallStatusChanged -= OnStatusChanged;
        }

        if (finished is null)
            return;

        if (finished.Status != SteamCmdInstallStatus.Completed)
            WriteError(new InvalidOperationException(string.IsNullOrWhiteSpace(finished.ErrorMessage)
                ? $"SteamCMD could not install app {AppId}"
                : finished.ErrorMessage), "InstallContentError", AppId);

        WriteObject(finished);
    }
}

[Cmdlet(VerbsCommon.Remove, "SteamContent")]
[OutputType(typeof(SteamCmdStatus))]
public class RemoveSteamContentCmdlet : SteamCmdlet
{
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The directory path of the installed Steam content to remove.")]
    public string InstallDirectory { get; set; } = string.Empty;

    protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            WriteObject(await SteamCmd.RemoveContentAsync(InstallDirectory));
        }
        catch (Exception ex)
        {
            WriteError(ex, "RemoveContentError");
        }
    }
}
