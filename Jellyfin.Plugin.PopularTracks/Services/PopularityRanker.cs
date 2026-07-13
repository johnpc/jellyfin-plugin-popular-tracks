using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.PopularTracks.LastFm;

namespace Jellyfin.Plugin.PopularTracks.Services
{
    /// <summary>
    /// Pure re-ordering logic, decoupled from Jellyfin types so it is trivially unit-testable.
    /// Orders owned tracks by their rank in the Last.fm artist top-tracks list (most popular first),
    /// with tracks Last.fm doesn't rank falling to the bottom in their original order, then applies
    /// the request's paging.
    /// </summary>
    public static class PopularityRanker
    {
        /// <summary>
        /// Re-orders <paramref name="items"/> by Last.fm popularity and applies paging.
        /// </summary>
        /// <typeparam name="T">The item type (e.g. a Jellyfin audio item).</typeparam>
        /// <param name="items">The full set of owned tracks for the artist, in the native order.</param>
        /// <param name="keySelector">Projects an item to its normalized <see cref="TrackKey"/>.</param>
        /// <param name="ranked">The Last.fm top tracks, already ordered most-popular-first.</param>
        /// <param name="startIndex">The requested paging offset (null = 0).</param>
        /// <param name="limit">The requested page size (null = all remaining).</param>
        /// <param name="collapseDuplicates">When true, keep only the first (best-ranked) copy of each
        /// track key, so libraries that hold several physical copies of a song show it once.</param>
        /// <returns>The re-ordered, de-duplicated, paged items.</returns>
        public static IReadOnlyList<T> Order<T>(
            IReadOnlyList<T> items,
            Func<T, TrackKey> keySelector,
            IReadOnlyList<SimilarTrack> ranked,
            int? startIndex,
            int? limit,
            bool collapseDuplicates = false)
        {
            var rankByKey = BuildRankIndex(ranked);

            // OrderBy is a stable sort, so unranked tracks (all sharing int.MaxValue) keep their
            // original relative order and sit after every ranked track.
            IEnumerable<T> paged = items.OrderBy(item => rankByKey.GetValueOrDefault(keySelector(item), int.MaxValue));

            if (collapseDuplicates)
            {
                // After the stable sort the first copy of each key is the best-positioned one.
                paged = DistinctByKey(paged, keySelector);
            }

            var skip = startIndex.GetValueOrDefault(0);
            if (skip > 0)
            {
                paged = paged.Skip(skip);
            }

            if (limit.HasValue && limit.Value >= 0)
            {
                paged = paged.Take(limit.Value);
            }

            return paged.ToList();
        }

        private static IEnumerable<T> DistinctByKey<T>(IEnumerable<T> source, Func<T, TrackKey> keySelector)
        {
            var seen = new HashSet<TrackKey>();
            foreach (var item in source)
            {
                if (seen.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }

        private static Dictionary<TrackKey, int> BuildRankIndex(IReadOnlyList<SimilarTrack> ranked)
        {
            var rankByKey = new Dictionary<TrackKey, int>();
            for (var i = 0; i < ranked.Count; i++)
            {
                // First occurrence wins: Last.fm lists most-popular first, so keep the best rank on collision.
                rankByKey.TryAdd(ranked[i].Key, i);
            }

            return rankByKey;
        }
    }
}
