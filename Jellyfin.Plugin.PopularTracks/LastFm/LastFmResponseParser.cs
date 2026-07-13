using System.Collections.Generic;
using System.Text.Json;

namespace Jellyfin.Plugin.PopularTracks.LastFm
{
    /// <summary>
    /// Pure parser for the Last.fm JSON responses this plugin consumes. Kept separate from
    /// the HTTP client so the (branch-heavy) parsing logic is unit-testable without a network.
    /// </summary>
    public static class LastFmResponseParser
    {
        /// <summary>
        /// Parses an <c>artist.getTopTracks</c> response into rank-ordered tracks. Last.fm returns
        /// them most-popular-first; the score is a rank-decayed value in (0..1] preserving that order.
        /// </summary>
        /// <param name="json">The raw JSON response body.</param>
        /// <param name="artist">The artist these top tracks belong to.</param>
        /// <returns>The parsed tracks in popularity order; empty on any missing/invalid shape.</returns>
        public static IReadOnlyList<SimilarTrack> ParseArtistTopTracks(string json, string artist)
        {
            var results = new List<SimilarTrack>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return results;
            }

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("toptracks", out var container)
                || !container.TryGetProperty("track", out var tracks)
                || tracks.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            var rank = 0;
            foreach (var track in tracks.EnumerateArray())
            {
                var name = ReadString(track, "name");
                if (name.Length == 0)
                {
                    continue;
                }

                rank++;
                var score = 1d / (1d + rank);
                results.Add(new SimilarTrack(TrackNormalizer.ToKey(artist, name), score));
            }

            return results;
        }

        private static string ReadString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
