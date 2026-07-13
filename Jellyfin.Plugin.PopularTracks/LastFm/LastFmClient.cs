using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PopularTracks.LastFm
{
    /// <summary>
    /// HTTP-backed <see cref="ILastFmClient"/>. Thin: it builds the request URL, performs the GET,
    /// and delegates parsing to <see cref="LastFmResponseParser"/>. Network/parse failures degrade
    /// to an empty result so the caller can fall back to the native Jellyfin ordering.
    /// </summary>
    public sealed class LastFmClient : ILastFmClient
    {
        private const string ApiRoot = "https://ws.audioscrobbler.com/2.0/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LastFmClient> _logger;
        private readonly Func<string> _apiKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastFmClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The Jellyfin-provided HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="apiKeyProvider">Supplies the current Last.fm API key from configuration.</param>
        public LastFmClient(
            IHttpClientFactory httpClientFactory,
            ILogger<LastFmClient> logger,
            Func<string> apiKeyProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiKeyProvider = apiKeyProvider;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SimilarTrack>> GetArtistTopTracksAsync(string artist, int limit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(_apiKeyProvider()))
            {
                return Array.Empty<SimilarTrack>();
            }

            var url = BuildUrl("artist.gettoptracks", limit, ("artist", artist));
            var body = await GetAsync(url, cancellationToken).ConfigureAwait(false);
            return body == null ? Array.Empty<SimilarTrack>() : LastFmResponseParser.ParseArtistTopTracks(body, artist);
        }

        private string BuildUrl(string method, int limit, params (string Key, string Value)[] parameters)
        {
            var builder = new System.Text.StringBuilder(ApiRoot);
            builder.Append("?method=").Append(method);
            foreach (var (key, value) in parameters)
            {
                builder.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(value));
            }

            builder.Append("&autocorrect=1&format=json&limit=").Append(limit.ToString(CultureInfo.InvariantCulture));
            builder.Append("&api_key=").Append(Uri.EscapeDataString(_apiKeyProvider()));
            return builder.ToString();
        }

        private async Task<string?> GetAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PopularTracks");
                using var response = await client.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PopularTracks: Last.fm returned {Status} for a request.", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "PopularTracks: Last.fm request failed.");
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
