using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.PopularTracks.LastFm;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.PopularTracks.Services
{
    /// <summary>
    /// Orchestrates the corrected artist "Popular" ordering. Because Jellyfin applies the request's
    /// <c>Limit</c> in SQL (top-N by the meaningless local PlayCount) before the plugin ever sees the
    /// result, this service ignores the native result and re-queries the artist's full owned track
    /// set, re-orders it by Last.fm popularity, then re-applies paging itself.
    /// </summary>
    public sealed class PopularTracksService
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IArtistTopTracksCache _topTracks;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopularTracksService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="dtoService">The DTO service.</param>
        /// <param name="topTracks">The cached Last.fm artist top-tracks source.</param>
        public PopularTracksService(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IArtistTopTracksCache topTracks)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _topTracks = topTracks;
        }

        /// <summary>
        /// Builds a Last.fm-popularity-ordered result for an artist's tracks, or <c>null</c> to signal
        /// the caller should leave the native result untouched (unknown artist, no Last.fm data, etc.).
        /// </summary>
        /// <param name="request">The parsed request parameters.</param>
        /// <returns>The re-ordered result, or <c>null</c> to fall back to native.</returns>
        public QueryResult<BaseItemDto>? BuildOrderedResult(PopularTracksRequest request)
        {
            var user = request.UserId.HasValue && request.UserId.Value != Guid.Empty
                ? _userManager.GetUserById(request.UserId.Value)
                : null;

            var artistName = ResolveArtistName(request);
            if (string.IsNullOrEmpty(artistName))
            {
                return null;
            }

            var owned = QueryArtistTracks(request, user);
            if (owned.Count == 0)
            {
                return null;
            }

            var ranked = _topTracks.GetAsync(artistName, Math.Max(owned.Count, 50), CancellationToken.None)
                .GetAwaiter().GetResult();
            if (ranked.Count == 0)
            {
                return null;
            }

            var ordered = PopularityRanker.Order(owned, KeyOf, ranked, request.StartIndex, request.Limit);
            var dtos = _dtoService.GetBaseItemDtos(ordered.Cast<BaseItem>().ToList(), request.DtoOptions, user);
            return new QueryResult<BaseItemDto>(request.StartIndex.GetValueOrDefault(0), owned.Count, dtos);
        }

        private static TrackKey KeyOf(Audio audio)
        {
            return TrackNormalizer.ToKey(PrimaryArtist(audio), audio.Name);
        }

        private static string? PrimaryArtist(Audio audio)
        {
            if (audio.Artists.Count > 0)
            {
                return audio.Artists[0];
            }

            return audio.AlbumArtists.Count > 0 ? audio.AlbumArtists[0] : null;
        }

        private string? ResolveArtistName(PopularTracksRequest request)
        {
            var artistId = request.ArtistIds.FirstOrDefault(id => id != Guid.Empty);
            if (artistId == Guid.Empty)
            {
                artistId = request.AlbumArtistIds.FirstOrDefault(id => id != Guid.Empty);
            }

            return artistId == Guid.Empty ? null : _libraryManager.GetItemById<MusicArtist>(artistId)?.Name;
        }

        private List<Audio> QueryArtistTracks(PopularTracksRequest request, User? user)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                MediaTypes = new[] { MediaType.Audio },
                Recursive = true,
                EnableTotalRecordCount = false,
            };

            if (request.ArtistIds.Count > 0)
            {
                query.ArtistIds = request.ArtistIds.ToArray();
            }

            if (request.AlbumArtistIds.Count > 0)
            {
                query.AlbumArtistIds = request.AlbumArtistIds.ToArray();
            }

            return _libraryManager.GetItemList(query).OfType<Audio>().ToList();
        }
    }
}
