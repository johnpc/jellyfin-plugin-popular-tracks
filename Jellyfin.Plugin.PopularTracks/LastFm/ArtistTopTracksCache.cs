using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PopularTracks.LastFm
{
    /// <summary>
    /// In-memory, thread-safe <see cref="IArtistTopTracksCache"/> backed by a
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/>. Successful (non-empty) results are cached
    /// for the configured TTL; empty results (unknown artist / transient failure) are not cached, so
    /// a later request can retry rather than being pinned to a miss.
    /// </summary>
    public sealed class ArtistTopTracksCache : IArtistTopTracksCache
    {
        private readonly ILastFmClient _client;
        private readonly Func<TimeSpan> _ttlProvider;
        private readonly Func<DateTimeOffset> _clock;
        private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistTopTracksCache"/> class.
        /// </summary>
        /// <param name="client">The underlying Last.fm client.</param>
        /// <param name="ttlProvider">Supplies the current cache TTL from configuration.</param>
        /// <param name="clock">Supplies the current time (injectable for testing).</param>
        public ArtistTopTracksCache(ILastFmClient client, Func<TimeSpan> ttlProvider, Func<DateTimeOffset> clock)
        {
            _client = client;
            _ttlProvider = ttlProvider;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SimilarTrack>> GetAsync(string artist, int limit, CancellationToken cancellationToken)
        {
            var key = TrackNormalizer.NormalizeArtist(artist);
            if (key.Length == 0)
            {
                return Array.Empty<SimilarTrack>();
            }

            var now = _clock();
            if (_entries.TryGetValue(key, out var cached) && cached.Expiry > now)
            {
                return cached.Tracks;
            }

            var tracks = await _client.GetArtistTopTracksAsync(artist, limit, cancellationToken).ConfigureAwait(false);
            if (tracks.Count > 0)
            {
                _entries[key] = new Entry(tracks, now + _ttlProvider());
            }

            return tracks;
        }

        private sealed class Entry
        {
            public Entry(IReadOnlyList<SimilarTrack> tracks, DateTimeOffset expiry)
            {
                Tracks = tracks;
                Expiry = expiry;
            }

            public IReadOnlyList<SimilarTrack> Tracks { get; }

            public DateTimeOffset Expiry { get; }
        }
    }
}
