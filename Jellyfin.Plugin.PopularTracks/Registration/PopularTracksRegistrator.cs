using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Jellyfin.Plugin.PopularTracks.LastFm;
using Jellyfin.Plugin.PopularTracks.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PopularTracks.Registration
{
    /// <summary>
    /// Registers PopularTracks services into Jellyfin's DI container at startup.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class PopularTracksRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpClient("PopularTracks", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin-PopularTracks/1.0 (+https://github.com/johnpc/jellyfin-plugin-popular-tracks)");
            });

            serviceCollection.AddSingleton<ILastFmClient>(sp => new LastFmClient(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<LastFmClient>>(),
                () => Plugin.GetConfiguration().LastFmApiKey));

            serviceCollection.AddSingleton<IArtistTopTracksCache>(sp => new ArtistTopTracksCache(
                sp.GetRequiredService<ILastFmClient>(),
                () => TimeSpan.FromHours(Math.Max(1, Plugin.GetConfiguration().CacheTtlHours)),
                () => DateTimeOffset.UtcNow));

            serviceCollection.AddSingleton<PopularTracksService>();
        }
    }
}
