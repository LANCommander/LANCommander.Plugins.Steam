# LANCommander Steam plugin

A LANCommander server plugin holding everything Steam that was part of the server before 2.2.0:

- SteamCMD. An administrator points the plugin at SteamCMD and signs in to one or more Steam
  accounts, and server scripts get the SteamCMD and Steam store cmdlets (`Install-SteamContent`,
  `Connect-SteamCmd`, `Search-SteamGames` and the rest), most often to install and update
  dedicated servers.
- The Steam store as a source in the media search: covers, backgrounds, logos, grids,
  screenshots, trailers and manuals.

## Layout

| Path | What it is |
| --- | --- |
| `src/LANCommander.Steam` | Drives SteamCMD and reads Steam's public store data. Moved here from the LANCommander repository, and loaded privately by the plugin. |
| `src/LANCommander.Plugins.Steam` | The server plugin: settings and profile storage, the SteamCMD page, the cmdlets, and the media source. |
| `tests/` | xunit and bUnit tests, including loading the built plugin the way the server does and running the cmdlets in the SDK's script runspace. |
| `docs/Cmdlets.md` | Reference for the cmdlets. |

## Installing

On a LANCommander server (2.2.0-alpha.2 or later), open **Settings → Plugins**. Find Steam on the **Browse**
tab and install it, or download the `.nupkg` attached to a
[release](https://github.com/LANCommander/LANCommander.Plugins.Steam/releases) and upload it on the
**Installed** tab. The plugin is installed when the server restarts.

## Building

The plugin compiles against the `LANCommander.SDK`, `LANCommander.Server.Plugins` and
`LANCommander.Server.UI` packages of the server it targets, set by `LANCommanderVersion` in
`Directory.Packages.props`.

```
dotnet build
dotnet test
```

To build against a server that is not released yet, pack its `LANCommander.SDK`,
`LANCommander.Server.Plugins` and `LANCommander.Server.UI` projects into a folder under a version
of your own, then build with that version and folder:

```
dotnet build -p:LANCommanderVersion=2.2.0-dev.3 -p:RestoreAdditionalProjectSources=D:\path\to\feed
```

The plugin's build output is its drop-in folder, and it holds only the plugin's own assemblies:
`LANCommander.Plugins.Steam`, `LANCommander.Steam` and `HtmlAgilityPack`. Everything else,
including the SDK, PowerShell and the server's Blazor controls, comes from the host, so the
plugin's types are the host's types. The build removes anything else that lands in the output and
fails if an expected assembly is missing. No `.deps.json` is written on purpose; without one, the
plugin's load context resolves from the plugin folder and leaves everything else to the host.

## Deploying

```
dotnet build src/LANCommander.Plugins.Steam -t:Deploy -p:LANCommanderDataDir=D:\path\to\Data\
```

This copies the plugin to `<data dir>\Plugins\LANCommander.Plugins.Steam\`. The data directory
defaults to `LANCommander.Server\Data` in a LANCommander checkout beside this repository, which is
where a server run from the IDE keeps its data. Restart the server; its log shows
`Loaded plugin 'Steam' (lancommander.steam)`.

The plugin keeps its own state under `<data dir>\PluginData\Steam\`, outside the drop-in folder, so
redeploying does not lose it:

| File | Holds |
| --- | --- |
| `settings.json` | The SteamCMD path |
| `profiles.json` | The Steam accounts set up for SteamCMD, and the directory each installs into by default |

Neither file holds a password. SteamCMD keeps each account's sign-in itself, in its own folder.

Until the plugin has saved a file of its own, it starts from what the server's settings held
before 2.2.0: the path from `Steam.Path` and the profiles from `Steam.Profiles`. The server no
longer reads those settings, and drops them the next time it saves its settings file, so install
the plugin before changing any server settings to keep them.

## Releasing

Pushing a tag `vX.Y.Z` (or `vX.Y.Z-suffix` for a prerelease, such as `v2.2.0-alpha` and then
`v2.2.0-alpha.2`) runs `.github/workflows/Release.yml`, e.g. `git tag v1.0.0 && git push origin v1.0.0`.
It builds and tests at that version, signs the plugin's own assemblies when Azure Artifact Signing is
configured, packs the plugin, pushes it to nuget.org, and attaches the package to the tag's GitHub
release, creating it (with generated notes, as a prerelease for suffixed tags) if there isn't one. It
runs in a `release` environment and publishes through nuget.org Trusted Publishing, so no NuGet API key
is stored: it needs a Trusted Publishing policy for this repository, `Release.yml` and the `release`
environment, owned by the [`LANCommander` organization](https://www.nuget.org/profiles/LANCommander),
which is what makes the server mark the plugin official, and a `NUGET_USER` variable. The signing
secrets and variables are listed at the top of the workflow.
`.github/workflows/CI.yml` builds, tests and packs every push and pull request.

Both workflows restore from nuget.org only, so they can't build until LANCommander 2.2.0-alpha.2 is
published there.

`dotnet pack src/LANCommander.Plugins.Steam -c Release` makes the same package locally.
`build/PluginPackage.targets` packs the drop-in folder into the package's `lib/net10.0/` folder, with
no dependencies, and sets the package types from the `Hosts` in `[LANCommanderPlugin]`:
`LANCommander.Plugin`, plus `LANCommander.Plugin.Server` for a server plugin, which is what the
server's Plugins page searches for. The file is the same in every LANCommander plugin repository.

## Using it

The plugin adds a **SteamCMD** entry under **Settings → Integrations** for administrators.

**SteamCMD path.** Set the path to the SteamCMD executable, or use auto detect. Left empty,
SteamCMD is looked for in the usual places whenever it is needed.

**Profiles.** Add a profile per Steam account, sign in with its password, and confirm the sign-in
in the Steam mobile app if Steam Guard asks. Set the directory the account installs into by
default. Removing a profile signs the account out of SteamCMD.

**Media.** In a game's media search, **Steam** is one of the sources, as it was before. It
finds the game on the Steam store by title and offers its library cover, hero background, logo,
header (as a grid), screenshots, trailers and manual.

**Scripts.** See [docs/Cmdlets.md](docs/Cmdlets.md). A script that installs a dedicated server
with SteamCMD looks like this:

```powershell
Install-SteamContent -AppId 232250 -InstallDirectory "$InstallDirectory\tf2" -ErrorAction Stop
```

Differences from the cmdlets as they were in the SDK:

- `Install-SteamContent` waits for SteamCMD, reports progress, returns the finished job and writes
  an error when SteamCMD fails. Before, it returned nothing, and an install SteamCMD failed
  wrote no error.
- Profiles are kept in the plugin's `profiles.json`, not in the server's settings, and the page
  and scripts share them.

## Limitations

- The cmdlets are available only to scripts the server runs. Launcher-side scripts do not get
  them, and the plugin does not bring back Steam lobbies in the launcher.
- One SteamCMD install is shared by the whole server, and nothing stops two scripts from running
  it at once.
