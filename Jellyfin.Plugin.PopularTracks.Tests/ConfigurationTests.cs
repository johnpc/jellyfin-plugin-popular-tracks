using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.Configuration;
using Xunit;

namespace Jellyfin.Plugin.PopularTracks.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void Defaults_AreSensible()
        {
            var config = new PluginConfiguration();

            config.Enabled.Should().BeTrue();
            config.CacheTtlHours.Should().Be(12);
            config.LastFmApiKey.Should().BeEmpty();
        }
    }
}
