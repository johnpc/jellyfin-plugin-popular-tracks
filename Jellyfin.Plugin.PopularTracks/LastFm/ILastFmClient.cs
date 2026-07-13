using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PopularTracks.LastFm
{
    /// <summary>
    /// Reads an artist's most-played tracks from the Last.fm listening crowd. This is the real
    /// popularity signal PopularTracks uses to re-order the artist "Popular" list, in place of the
    /// local <c>PlayCount</c> which is meaningless on servers where nobody scrobbles.
    /// </summary>
    public interface ILastFmClient
    {
        /// <summary>
        /// Gets an artist's top tracks, ordered from most to least popular.
        /// </summary>
        /// <param name="artist">The artist name (raw; it is normalized internally).</param>
        /// <param name="limit">The maximum number of tracks to request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The artist's top tracks in popularity order; empty if the artist is unknown to Last.fm or the request fails.</returns>
        Task<IReadOnlyList<SimilarTrack>> GetArtistTopTracksAsync(string artist, int limit, CancellationToken cancellationToken);
    }
}
