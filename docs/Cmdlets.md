# Steam cmdlets

The plugin adds these cmdlets to the runspace LANCommander server scripts run in, such as the
scripts of a game server. They were part of the SDK before 2.2.0 and keep their names and
parameters.

The SteamCMD cmdlets use the SteamCMD path and profiles set on the plugin's **SteamCMD** page.
Each cmdlet writes SteamCMD's output to the script's host.

## Connection management

### `Connect-SteamCmd`

```powershell
Connect-SteamCmd [-Username] <string> [-Password <SecureString>]
```

Signs in to Steam through SteamCMD and returns a `SteamCmdStatus`: `Success`, `InvalidPassword`,
`PathNotConfigured`, `ExecutableNotFound`, `Error` or `UnknownError`. Without `-Password`, SteamCMD
uses the sign-in it has cached for the account. A Steam Guard prompt goes to the account's Steam
mobile app.

```powershell
$password = ConvertTo-SecureString "mypassword" -AsPlainText -Force
Connect-SteamCmd -Username "myusername" -Password $password
```

### `Disconnect-SteamCmd`

```powershell
Disconnect-SteamCmd [-Username] <string>
```

Signs the account out of SteamCMD and returns a `SteamCmdStatus`.

### `Get-SteamCmdConnectionStatus`

```powershell
Get-SteamCmdConnectionStatus [-Username] <string>
```

Returns a `SteamCmdConnectionStatus`: `Authenticated`, `Unauthenticated`, `NotInstalled` (SteamCMD
could not be found) or `Unknown`.

```powershell
if ((Get-SteamCmdConnectionStatus "myusername") -ne "Authenticated") {
    Connect-SteamCmd "myusername"
}
```

## SteamCMD configuration

### `Get-SteamCmdPath`

```powershell
Get-SteamCmdPath
```

Looks for SteamCMD in the usual places and returns its path, or nothing when it is not found. This
does not change the path the plugin is set to use.

### `Get-SteamCmdProfile`

```powershell
Get-SteamCmdProfile [-Username] <string>
```

Returns the `SteamCmdProfile` (`Username`, `InstallDirectory`) for the account, or nothing.

### `Get-SteamCmdProfiles`

```powershell
Get-SteamCmdProfiles
```

Returns every `SteamCmdProfile`.

```powershell
foreach ($profile in Get-SteamCmdProfiles) {
    Write-Host "$($profile.Username): $($profile.InstallDirectory)"
}
```

### `Set-SteamCmdProfile`

```powershell
Set-SteamCmdProfile [-Username] <string> [-InstallDirectory] <string>
```

Adds a profile, or changes the install directory of an existing one. Usernames are matched without
regard to case.

### `Remove-SteamCmdProfile`

```powershell
Remove-SteamCmdProfile [-Username] <string>
```

Deletes the profile. It does not sign the account out; use `Disconnect-SteamCmd` for that.

## Content installation

### `Install-SteamContent`

```powershell
Install-SteamContent [-AppId] <uint> [-InstallDirectory] <string> [-Username <string>]
```

Installs or updates an app with SteamCMD (`app_update <AppId> validate`) into `InstallDirectory`,
creating it if needed. Without `-Username` it signs in anonymously, which is enough for most
dedicated servers. The cmdlet waits for SteamCMD to finish, reports its progress, and returns the
finished `SteamCmdInstallJob` (`Status`, `ErrorMessage`, `CompletedAt` and so on). A failed
install also writes an error, so `-ErrorAction Stop` stops the script.

```powershell
# Team Fortress 2 dedicated server
Install-SteamContent -AppId 232250 -InstallDirectory "$InstallDirectory\tf2" -ErrorAction Stop
```

### `Remove-SteamContent`

```powershell
Remove-SteamContent [-InstallDirectory] <string>
```

Deletes the directory and everything in it, and returns a `SteamCmdStatus`. A directory that does
not exist counts as removed.

## Steam store

These cmdlets read public data and do not need SteamCMD.

### `Search-SteamGames`

```powershell
Search-SteamGames [-Keyword] <string>
```

Searches the Steam store and returns `GameSearchResult` objects (`Name`, `AppId`).

```powershell
Search-SteamGames "Counter-Strike" | ForEach-Object { "$($_.Name) - $($_.AppId)" }
```

### `Get-SteamAppInfo`

```powershell
Get-SteamAppInfo [-AppId] <uint>
```

Returns the app's product info from [steamcmd.net](https://www.steamcmd.net/) as an `AppInfo`
object: `ChangeNumber`, and the `Common`, `Config`, `Depots`, `Extended`, `Install` and `UFS`
sections of the app's PICS record. Accepts app IDs from the pipeline.

```powershell
$info = Get-SteamAppInfo 413150
Write-Host "$($info.Common.Name) is at change $($info.ChangeNumber)"
```

### `Get-SteamWebAssetUri`

```powershell
Get-SteamWebAssetUri [-AppId] <int> [-WebAssetType] <WebAssetType>
```

Returns the `Uri` of one of the app's store images. `WebAssetType` is one of:

| Value | Image |
| --- | --- |
| `Capsule` | Small capsule, 231×87 |
| `CapsuleLarge` | Large capsule, 616×353 |
| `Header` | Store header |
| `HeroCapsule` | Hero capsule |
| `LibraryCover` | Library cover, 600×900 |
| `LibraryHeader` | Library header |
| `LibraryHero` | Library hero |
| `Logo` | Logo, PNG |

The URI is built, not checked; not every app has every image.
