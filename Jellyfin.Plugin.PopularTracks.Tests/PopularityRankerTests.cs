using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.LastFm;
using Jellyfin.Plugin.PopularTracks.Services;
using Xunit;

namespace Jellyfin.Plugin.PopularTracks.Tests
{
    public class PopularityRankerTests
    {
        // A minimal stand-in for a track: an id plus its normalized key.
        private sealed record Track(string Id, TrackKey Key);

        private static SimilarTrack Ranked(string artist, string title)
            => new(new TrackKey(artist, title), 1.0);

        private static IReadOnlyList<string> OrderIds(
            IReadOnlyList<Track> items, IReadOnlyList<SimilarTrack> ranked, int? start = null, int? limit = null, bool collapse = false)
            => PopularityRanker.Order(items, t => t.Key, ranked, start, limit, collapse).Select(t => t.Id).ToList();

        [Fact]
        public void Order_SortsByLastFmRank()
        {
            // Native order (by garbage PlayCount) is C, A, B; Last.fm popularity is A, B, C.
            var items = new List<Track>
            {
                new("C", new TrackKey("radiohead", "c")),
                new("A", new TrackKey("radiohead", "a")),
                new("B", new TrackKey("radiohead", "b")),
            };
            var ranked = new[] { Ranked("radiohead", "a"), Ranked("radiohead", "b"), Ranked("radiohead", "c") };

            OrderIds(items, ranked).Should().Equal("A", "B", "C");
        }

        [Fact]
        public void Order_PutsUnrankedTracksLast_PreservingTheirOriginalOrder()
        {
            var items = new List<Track>
            {
                new("Deep1", new TrackKey("radiohead", "deep1")),
                new("Hit", new TrackKey("radiohead", "hit")),
                new("Deep2", new TrackKey("radiohead", "deep2")),
            };
            var ranked = new[] { Ranked("radiohead", "hit") };

            // Hit is ranked → first; the two unranked keep native order (Deep1 before Deep2).
            OrderIds(items, ranked).Should().Equal("Hit", "Deep1", "Deep2");
        }

        [Fact]
        public void Order_AppliesStartIndexAndLimit()
        {
            var items = new List<Track>
            {
                new("A", new TrackKey("a", "a")),
                new("B", new TrackKey("a", "b")),
                new("C", new TrackKey("a", "c")),
                new("D", new TrackKey("a", "d")),
            };
            var ranked = new[] { Ranked("a", "a"), Ranked("a", "b"), Ranked("a", "c"), Ranked("a", "d") };

            OrderIds(items, ranked, start: 1, limit: 2).Should().Equal("B", "C");
        }

        [Fact]
        public void Order_CollapseDuplicates_KeepsOneCopyPerKey_BestRankedFirst()
        {
            // Library holds 3 physical copies of the hit and 2 of a deep cut.
            var items = new List<Track>
            {
                new("Hit-copy1", new TrackKey("cage", "cigarette daydreams")),
                new("Deep-copy1", new TrackKey("cage", "telescope")),
                new("Hit-copy2", new TrackKey("cage", "cigarette daydreams")),
                new("Hit-copy3", new TrackKey("cage", "cigarette daydreams")),
                new("Deep-copy2", new TrackKey("cage", "telescope")),
            };
            var ranked = new[] { Ranked("cage", "cigarette daydreams"), Ranked("cage", "telescope") };

            OrderIds(items, ranked, collapse: true).Should().Equal("Hit-copy1", "Deep-copy1");
        }

        [Fact]
        public void Order_WithoutCollapse_KeepsEveryCopy()
        {
            var items = new List<Track>
            {
                new("Hit-copy1", new TrackKey("cage", "cigarette daydreams")),
                new("Hit-copy2", new TrackKey("cage", "cigarette daydreams")),
            };
            var ranked = new[] { Ranked("cage", "cigarette daydreams") };

            OrderIds(items, ranked, collapse: false).Should().Equal("Hit-copy1", "Hit-copy2");
        }

        [Fact]
        public void Order_WithNoRankedTracks_KeepsOriginalOrder()
        {
            var items = new List<Track>
            {
                new("A", new TrackKey("a", "a")),
                new("B", new TrackKey("a", "b")),
            };

            OrderIds(items, System.Array.Empty<SimilarTrack>()).Should().Equal("A", "B");
        }
    }
}
