using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.LastFm;
using Xunit;

namespace Jellyfin.Plugin.PopularTracks.Tests
{
    public class LastFmResponseParserTests
    {
        [Fact]
        public void ParseArtistTopTracks_RankDecaysScoresAndKeysToArtist()
        {
            const string json = @"{
              ""toptracks"": {
                ""track"": [
                  { ""name"": ""Da Funk"" },
                  { ""name"": ""Digital Love"" }
                ]
              }
            }";

            var result = LastFmResponseParser.ParseArtistTopTracks(json, "Daft Punk");

            result.Should().HaveCount(2);
            result[0].Key.Should().Be(new TrackKey("daft punk", "da funk"));
            result[0].Score.Should().BeGreaterThan(result[1].Score, "earlier rank scores higher");
            result[0].Score.Should().BeApproximately(0.5, 1e-6);
        }

        [Fact]
        public void ParseArtistTopTracks_SkipsNamelessEntries()
        {
            const string json = @"{
              ""toptracks"": {
                ""track"": [
                  { ""name"": ""One More Time"" },
                  { ""playcount"": ""123"" }
                ]
              }
            }";

            LastFmResponseParser.ParseArtistTopTracks(json, "Daft Punk").Should().ContainSingle();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{}")]
        [InlineData(@"{""toptracks"":{}}")]
        [InlineData(@"{""toptracks"":{""track"":[]}}")]
        public void ParseArtistTopTracks_ReturnsEmptyForMissingShapes(string json)
        {
            LastFmResponseParser.ParseArtistTopTracks(json, "Daft Punk").Should().BeEmpty();
        }
    }
}
