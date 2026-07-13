using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PopularTracks.LastFm
{
    /// <summary>
    /// A caching layer over <see cref="ILastFmClient"/>. Artist top-tracks change slowly, so results
    /// are memoized per (normalized) artist for a configurable TTL — at most one Last.fm call per
    /// artist per window, keeping artist-page loads fast.
    /// </summary>
    public interface IArtistTopTracksCache
    {
        /// <summary>
        /// Gets an artist's top tracks, serving from cache when a fresh entry exists.
        /// </summary>
        /// <param name="artist">The artist name (raw; normalized internally for the cache key).</param>
        /// <param name="limit">The maximum number of tracks to request on a cache miss.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The artist's top tracks in popularity order; empty if unknown to Last.fm.</returns>
        Task<IReadOnlyList<SimilarTrack>> GetAsync(string artist, int limit, CancellationToken cancellationToken);
    }
}
