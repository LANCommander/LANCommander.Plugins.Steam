using LANCommander.Plugins.Steam.Media;
using LANCommander.SDK.Enums;
using LANCommander.Steam;

namespace LANCommander.Plugins.Steam.Tests;

public class SteamMediaGrabberTests
{
    private readonly SteamMediaGrabber _grabber = new(new SteamClient());

    [Fact]
    public void ItOffersEverythingTheServersSteamGrabberDid()
        => Assert.Equal(
            [MediaType.Cover, MediaType.Background, MediaType.Logo, MediaType.Grid, MediaType.Screenshot, MediaType.Video, MediaType.Manual],
            _grabber.SupportedMediaTypes);

    [Fact]
    public async Task SteamHasOnlyOnePageOfResults()
    {
        Assert.False(_grabber.SupportsPaging);

        // Answered without asking Steam
        Assert.Empty(await _grabber.SearchAsync(MediaType.Cover, "Half-Life", 1, CancellationToken.None));
    }
}
