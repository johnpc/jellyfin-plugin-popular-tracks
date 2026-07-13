using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Dto;

namespace Jellyfin.Plugin.PopularTracks.Services
{
    /// <summary>
    /// The subset of the intercepted <c>GetItems</c> parameters that PopularTracks needs to rebuild
    /// the artist "Popular" result. Decouples the service from the MVC filter context so the service
    /// is unit-testable without an <c>ActionExecutingContext</c>.
    /// </summary>
    public sealed class PopularTracksRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopularTracksRequest"/> class.
        /// </summary>
        /// <param name="artistIds">The requested artist ids.</param>
        /// <param name="albumArtistIds">The requested album-artist ids.</param>
        /// <param name="userId">The requesting user id.</param>
        /// <param name="startIndex">The paging offset.</param>
        /// <param name="limit">The page size.</param>
        /// <param name="dtoOptions">The DTO options to shape the result items with.</param>
        public PopularTracksRequest(
            IReadOnlyList<Guid> artistIds,
            IReadOnlyList<Guid> albumArtistIds,
            Guid? userId,
            int? startIndex,
            int? limit,
            DtoOptions dtoOptions)
        {
            ArtistIds = artistIds ?? Array.Empty<Guid>();
            AlbumArtistIds = albumArtistIds ?? Array.Empty<Guid>();
            UserId = userId;
            StartIndex = startIndex;
            Limit = limit;
            DtoOptions = dtoOptions;
        }

        /// <summary>Gets the requested artist ids.</summary>
        public IReadOnlyList<Guid> ArtistIds { get; }

        /// <summary>Gets the requested album-artist ids.</summary>
        public IReadOnlyList<Guid> AlbumArtistIds { get; }

        /// <summary>Gets the requesting user id.</summary>
        public Guid? UserId { get; }

        /// <summary>Gets the paging offset.</summary>
        public int? StartIndex { get; }

        /// <summary>Gets the page size.</summary>
        public int? Limit { get; }

        /// <summary>Gets the DTO options to shape the result items with.</summary>
        public DtoOptions DtoOptions { get; }
    }
}
