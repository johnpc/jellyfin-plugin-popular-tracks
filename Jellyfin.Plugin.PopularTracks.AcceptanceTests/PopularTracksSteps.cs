using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.LastFm;
using Jellyfin.Plugin.PopularTracks.Services;
using Reqnroll;

namespace Jellyfin.Plugin.PopularTracks.AcceptanceTests
{
    [Binding]
    public sealed class PopularTracksSteps
    {
        private const string Artist = "Radiohead";

        private readonly List<OwnedTrack> _library = new();
        private readonly List<SimilarTrack> _ranked = new();
        private IReadOnlyList<OwnedTrack> _result = Array.Empty<OwnedTrack>();

        // A minimal owned-track stand-in: a title and its normalized key.
        private sealed record OwnedTrack(string Title, TrackKey Key);

        [Given("the library has these Radiohead tracks in this order:")]
        public void GivenTheLibraryHasTracks(Table table)
        {
            foreach (var row in table.Rows)
            {
                var title = row[0];
                _library.Add(new OwnedTrack(title, TrackNormalizer.ToKey(Artist, title)));
            }
        }

        [Given("Last.fm ranks Radiohead's top tracks as:")]
        public void GivenLastFmRanksTopTracks(Table table)
        {
            foreach (var row in table.Rows)
            {
                _ranked.Add(new SimilarTrack(TrackNormalizer.ToKey(Artist, row[0]), 1.0));
            }
        }

        [When("the Popular list is built")]
        public void WhenThePopularListIsBuilt()
        {
            _result = PopularityRanker.Order(_library, t => t.Key, _ranked, null, null);
        }

        [Then("the tracks appear in this order:")]
        public void ThenTheTracksAppearInOrder(Table table)
        {
            var expected = table.Rows.Select(r => r[0]).ToList();
            _result.Select(t => t.Title).Should().Equal(expected);
        }
    }
}
