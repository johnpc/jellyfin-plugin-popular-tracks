using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.PopularTracks.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.PopularTracks.Filters
{
    /// <summary>
    /// Runs after Jellyfin's native <c>ItemsController.GetItems</c> action. When the request is the
    /// artist "Popular" query (audio, sorted by PlayCount descending, scoped to an artist) and the
    /// plugin is enabled, it replaces the native <see cref="QueryResult{BaseItemDto}"/> with one
    /// re-ordered by real Last.fm popularity. Every other request — and any failure — is left untouched.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class PopularTracksFilter : IAsyncActionFilter
    {
        private readonly PopularTracksService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopularTracksFilter"/> class.
        /// </summary>
        /// <param name="service">The popular-tracks orchestrator.</param>
        public PopularTracksFilter(PopularTracksService service)
        {
            _service = service;
        }

        /// <inheritdoc />
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next().ConfigureAwait(false);

            if (!Plugin.GetConfiguration().Enabled)
            {
                return;
            }

            if (executed.Result is not ObjectResult objectResult
                || objectResult.Value is not QueryResult<BaseItemDto>)
            {
                return;
            }

            if (!IsArtistPopularQuery(context))
            {
                return;
            }

            var request = new PopularTracksRequest(
                GetArray<Guid>(context, "artistIds"),
                GetArray<Guid>(context, "albumArtistIds"),
                GetNullable<Guid>(context, "userId"),
                GetNullable<int>(context, "startIndex"),
                GetNullable<int>(context, "limit"),
                BuildDtoOptions(context));

            var replacement = _service.BuildOrderedResult(request);
            if (replacement != null)
            {
                executed.Result = new ObjectResult(replacement) { StatusCode = objectResult.StatusCode };
            }
        }

        private static bool IsArtistPopularQuery(ActionExecutingContext context)
        {
            var sortBy = GetArray<ItemSortBy>(context, "sortBy");
            if (!sortBy.Contains(ItemSortBy.PlayCount))
            {
                return false;
            }

            var sortOrder = GetArray<SortOrder>(context, "sortOrder");
            if (sortOrder.Length > 0 && !sortOrder.Contains(SortOrder.Descending))
            {
                return false;
            }

            if (!GetArray<BaseItemKind>(context, "includeItemTypes").Contains(BaseItemKind.Audio))
            {
                return false;
            }

            return GetArray<Guid>(context, "artistIds").Any(id => id != Guid.Empty)
                || GetArray<Guid>(context, "albumArtistIds").Any(id => id != Guid.Empty);
        }

        private static DtoOptions BuildDtoOptions(ActionExecutingContext context)
        {
            return new DtoOptions { Fields = GetArray<ItemFields>(context, "fields") }
                .AddAdditionalDtoOptions(
                    GetNullable<bool>(context, "enableImages"),
                    GetNullable<bool>(context, "enableUserData"),
                    GetNullable<int>(context, "imageTypeLimit"),
                    GetArray<ImageType>(context, "enableImageTypes"));
        }

        private static T? GetNullable<T>(ActionExecutingContext context, string name)
            where T : struct
        {
            return context.ActionArguments.TryGetValue(name, out var raw) && raw is T typed ? typed : (T?)null;
        }

        private static T[] GetArray<T>(ActionExecutingContext context, string name)
        {
            return context.ActionArguments.TryGetValue(name, out var raw) && raw is T[] typed
                ? typed
                : Array.Empty<T>();
        }
    }
}
