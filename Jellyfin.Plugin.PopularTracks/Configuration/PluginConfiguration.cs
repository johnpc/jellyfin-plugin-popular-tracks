using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PopularTracks.Configuration
{
    /// <summary>
    /// Plugin configuration for PopularTracks.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            LastFmApiKey = string.Empty;
            Enabled = true;
            CacheTtlHours = 12;
        }

        /// <summary>
        /// Gets or sets the Last.fm API key used to read artist top-track popularity.
        /// </summary>
        public string LastFmApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether PopularTracks re-orders the artist "Popular" list.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets how long (in hours) a fetched artist top-tracks list is cached before refetch.
        /// </summary>
        public int CacheTtlHours { get; set; }
    }
}
