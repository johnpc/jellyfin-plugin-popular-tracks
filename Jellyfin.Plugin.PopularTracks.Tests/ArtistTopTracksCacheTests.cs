using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.LastFm;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.PopularTracks.Tests
{
    public class ArtistTopTracksCacheTests
    {
        private readonly ILastFmClient _client = Substitute.For<ILastFmClient>();
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static IReadOnlyList<SimilarTrack> OneTrack()
            => new[] { new SimilarTrack(new TrackKey("radiohead", "creep"), 0.5) };

        private ArtistTopTracksCache Cache(TimeSpan ttl)
            => new(_client, () => ttl, () => _now);

        [Fact]
        public async Task GetAsync_CachesWithinTtl_HittingLastFmOnce()
        {
            _client.GetArtistTopTracksAsync("Radiohead", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(OneTrack());
            var cache = Cache(TimeSpan.FromHours(12));

            await cache.GetAsync("Radiohead", 50, CancellationToken.None);
            _now += TimeSpan.FromHours(6);
            var second = await cache.GetAsync("Radiohead", 50, CancellationToken.None);

            second.Should().ContainSingle();
            await _client.Received(1).GetArtistTopTracksAsync("Radiohead", Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync_RefetchesAfterTtlExpiry()
        {
            _client.GetArtistTopTracksAsync("Radiohead", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(OneTrack());
            var cache = Cache(TimeSpan.FromHours(12));

            await cache.GetAsync("Radiohead", 50, CancellationToken.None);
            _now += TimeSpan.FromHours(13);
            await cache.GetAsync("Radiohead", 50, CancellationToken.None);

            await _client.Received(2).GetArtistTopTracksAsync("Radiohead", Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync_DoesNotCacheEmptyResults()
        {
            _client.GetArtistTopTracksAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Array.Empty<SimilarTrack>());
            var cache = Cache(TimeSpan.FromHours(12));

            await cache.GetAsync("Unknown", 50, CancellationToken.None);
            await cache.GetAsync("Unknown", 50, CancellationToken.None);

            await _client.Received(2).GetArtistTopTracksAsync("Unknown", Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync_ReturnsEmptyForBlankArtist_WithoutCallingLastFm()
        {
            var cache = Cache(TimeSpan.FromHours(12));

            var result = await cache.GetAsync("   ", 50, CancellationToken.None);

            result.Should().BeEmpty();
            await _client.DidNotReceive().GetArtistTopTracksAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }
}
