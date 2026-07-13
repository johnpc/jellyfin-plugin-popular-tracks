using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.PopularTracks.Configuration;
using Jellyfin.Plugin.PopularTracks.Filters;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PopularTracks
{
    /// <summary>
    /// The main plugin class for PopularTracks.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<Plugin> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="actionProvider">The action descriptor collection provider.</param>
        /// <param name="lifetime">The host application lifetime.</param>
        /// <param name="logger">The logger.</param>
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            IServiceProvider serviceProvider,
            IActionDescriptorCollectionProvider actionProvider,
            IHostApplicationLifetime lifetime,
            ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;

            // Controller actions are registered before plugins finish loading, so hook once the app has started.
            lifetime.ApplicationStarted.Register(() => InjectFilters(actionProvider, serviceProvider));
        }

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "PopularTracks";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("f24f31a3-b1a9-4f70-97c9-4b25a8863a59");

        /// <inheritdoc />
        public override string Description =>
            "Fixes artist \"Popular\" track ordering using real Last.fm popularity instead of local play counts.";

        /// <summary>
        /// Gets the current plugin configuration, or defaults if the plugin is not loaded.
        /// </summary>
        /// <returns>The active configuration.</returns>
        public static PluginConfiguration GetConfiguration()
        {
            return Instance?.Configuration ?? new PluginConfiguration();
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
                },
            };
        }

        private void InjectFilters(IActionDescriptorCollectionProvider provider, IServiceProvider serviceProvider)
        {
            var count = provider.AddDynamicFilter<PopularTracksFilter>(serviceProvider, action =>
                action.ControllerTypeInfo.FullName == "Jellyfin.Api.Controllers.ItemsController"
                && action.MethodInfo.Name == "GetItems");

            _logger.LogInformation("PopularTracks: attached to {Count} GetItems action(s).", count);
        }
    }
}
