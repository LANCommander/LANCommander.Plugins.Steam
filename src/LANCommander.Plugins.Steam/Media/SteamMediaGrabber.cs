using System.Net.Mime;
using LANCommander.SDK.Enums;
using LANCommander.Server.Plugins;
using LANCommander.Steam;

namespace LANCommander.Plugins.Steam.Media;

/// <summary>
/// Finds a game's artwork, screenshots, trailers and manual on the Steam store, for the server's
/// media search.
/// </summary>
public sealed class SteamMediaGrabber(SteamClient steam) : IMediaGrabberExtension
{
    /// <summary>The store image each kind of artwork is taken from.</summary>
    private static readonly Dictionary<MediaType, WebAssetType> Artwork = new()
    {
        [MediaType.Cover] = WebAssetType.LibraryCover,
        [MediaType.Background] = WebAssetType.LibraryHero,
        [MediaType.Logo] = WebAssetType.Logo,
        [MediaType.Grid] = WebAssetType.Header,
    };

    public string Name => "Steam";

    public IReadOnlyCollection<MediaType> SupportedMediaTypes { get; } =
    [
        MediaType.Cover,
        MediaType.Background,
        MediaType.Logo,
        MediaType.Grid,
        MediaType.Screenshot,
        MediaType.Video,
        MediaType.Manual,
    ];

    // Steam returns the full set of assets for a game in a single request, so there are no further
    // pages to load.
    public bool SupportsPaging => false;

    public async Task<IReadOnlyList<MediaGrabberItem>> SearchAsync(MediaType type, string keywords, int page, CancellationToken cancellationToken)
    {
        if (page > 0)
            return [];

        var results = new List<MediaGrabberItem>();

        // The store's search suggestions can name an app more than once
        foreach (var game in (await steam.SearchGamesAsync(keywords)).DistinctBy(game => game.AppId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Artwork.TryGetValue(type, out var asset))
                results.AddRange(await GetArtworkAsync(type, asset, game));
            else if (type == MediaType.Screenshot)
                results.AddRange(await GetScreenshotsAsync(game));
            else if (type == MediaType.Video)
                results.AddRange(await GetVideosAsync(game));
            else if (type == MediaType.Manual)
                results.AddRange(await GetManualsAsync(game));
        }

        return results;
    }

    private async Task<IEnumerable<MediaGrabberItem>> GetArtworkAsync(MediaType type, WebAssetType asset, GameSearchResult game)
    {
        var (exists, mimeType) = await steam.HasWebAssetAsync(game.AppId, asset);

        if (!exists)
            return [];

        var uri = SteamClient.GetWebAssetUri(game.AppId, asset).ToString();

        return
        [
            new MediaGrabberItem
            {
                Id = game.AppId.ToString(),
                Type = type,
                SourceUrl = uri,
                ThumbnailUrl = uri,
                Group = game.Name,
                MimeType = mimeType,
            },
        ];
    }

    private async Task<IEnumerable<MediaGrabberItem>> GetScreenshotsAsync(GameSearchResult game)
        => (await steam.GetScreenshotsAsync(game.AppId)).Select(screenshot => new MediaGrabberItem
        {
            Id = $"{game.AppId}_{screenshot.Id}",
            Type = MediaType.Screenshot,
            SourceUrl = screenshot.PathFull,
            ThumbnailUrl = screenshot.PathThumbnail,
            Group = game.Name,
            MimeType = MediaTypeNames.Image.Jpeg,
        });

    private async Task<IEnumerable<MediaGrabberItem>> GetVideosAsync(GameSearchResult game)
    {
        var results = new List<MediaGrabberItem>();

        foreach (var movie in await steam.GetMoviesAsync(game.AppId))
        {
            var sourceUrl = movie.Mp4?.Max ?? movie.Mp4?.Resolution480;

            if (string.IsNullOrWhiteSpace(sourceUrl))
                continue;

            results.Add(new MediaGrabberItem
            {
                Id = movie.Id.ToString(),
                Type = MediaType.Video,
                SourceUrl = sourceUrl,
                ThumbnailUrl = movie.Thumbnail,
                Group = game.Name,
                MimeType = "video/mp4",
                // Steam names only its 480p rendition; "max" is whatever the trailer was cut at
                Resolution = movie.Mp4?.Max == null ? "480p" : null,
            });
        }

        return results;
    }

    private async Task<IEnumerable<MediaGrabberItem>> GetManualsAsync(GameSearchResult game)
    {
        if (!await steam.HasManualAsync(game.AppId))
            return [];

        return
        [
            new MediaGrabberItem
            {
                Id = game.AppId.ToString(),
                Type = MediaType.Manual,
                SourceUrl = SteamClient.GetManualUri(game.AppId).ToString(),
                // The server's own PDF icon
                ThumbnailUrl = "/static/pdf.png",
                Group = game.Name,
                MimeType = MediaTypeNames.Application.Pdf,
            },
        ];
    }
}
