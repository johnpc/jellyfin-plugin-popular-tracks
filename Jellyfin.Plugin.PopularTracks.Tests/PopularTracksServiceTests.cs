using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.PopularTracks.LastFm;
using Jellyfin.Plugin.PopularTracks.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using NSubstitute;
using Xunit;
using User = Jellyfin.Database.Implementations.Entities.User;

namespace Jellyfin.Plugin.PopularTracks.Tests
{
    public class PopularTracksServiceTests
    {
        private readonly IUserManager _userManager = Substitute.For<IUserManager>();
        private readonly ILibraryManager _libraryManager = Substitute.For<ILibraryManager>();
        private readonly IDtoService _dtoService = Substitute.For<IDtoService>();
        private readonly IArtistTopTracksCache _topTracks = Substitute.For<IArtistTopTracksCache>();

        private readonly Guid _artistId = Guid.NewGuid();

        private PopularTracksService Service()
            => new(_userManager, _libraryManager, _dtoService, _topTracks);

        private static Audio Song(string name) => new() { Id = Guid.NewGuid(), Name = name, Artists = new[] { "Radiohead" } };

        private PopularTracksRequest Request(int? startIndex = null, int? limit = null)
            => new(new[] { _artistId }, Array.Empty<Guid>(), null, startIndex, limit, new DtoOptions());

        private void GivenArtistNamed(string name)
            => _libraryManager.GetItemById<MusicArtist>(_artistId).Returns(new MusicArtist { Id = _artistId, Name = name });

        private void GivenOwnedTracks(params Audio[] tracks)
            => _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>()).Returns(tracks);

        private void GivenLastFmTopTracks(params string[] titles)
        {
            var ranked = titles.Select(t => new SimilarTrack(TrackNormalizer.ToKey("Radiohead", t), 1.0)).ToArray();
            _topTracks.GetAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<SimilarTrack>)ranked);
        }

        private void CaptureDtoOrder()
            => _dtoService.GetBaseItemDtos(Arg.Any<IReadOnlyList<BaseItem>>(), Arg.Any<DtoOptions>(), Arg.Any<User?>())
                .Returns(ci => ((IReadOnlyList<BaseItem>)ci[0]).Select(i => new BaseItemDto { Id = i.Id, Name = i.Name }).ToList());

        [Fact]
        public void BuildOrderedResult_ReordersByLastFmPopularity()
        {
            GivenArtistNamed("Radiohead");
            var creep = Song("Creep");
            var karma = Song("Karma Police");
            var deep = Song("Deep Cut");
            GivenOwnedTracks(deep, creep, karma); // native (garbage) order
            GivenLastFmTopTracks("Karma Police", "Creep"); // real popularity
            CaptureDtoOrder();

            var result = Service().BuildOrderedResult(Request());

            result.Should().NotBeNull();
            result!.Items.Select(i => i.Name).Should().Equal("Karma Police", "Creep", "Deep Cut");
            result.TotalRecordCount.Should().Be(3, "total reflects the full owned set, not the page");
        }

        [Fact]
        public void BuildOrderedResult_AppliesPaging()
        {
            GivenArtistNamed("Radiohead");
            var a = Song("A");
            var b = Song("B");
            var c = Song("C");
            GivenOwnedTracks(a, b, c);
            GivenLastFmTopTracks("A", "B", "C");
            CaptureDtoOrder();

            var result = Service().BuildOrderedResult(Request(startIndex: 1, limit: 1));

            result!.Items.Select(i => i.Name).Should().Equal("B");
            result.StartIndex.Should().Be(1);
        }

        [Fact]
        public void BuildOrderedResult_ReturnsNullWhenArtistUnknownToLibrary()
        {
            _libraryManager.GetItemById<MusicArtist>(_artistId).Returns((MusicArtist?)null);

            Service().BuildOrderedResult(Request()).Should().BeNull();
        }

        [Fact]
        public void BuildOrderedResult_ReturnsNullWhenNoOwnedTracks()
        {
            GivenArtistNamed("Radiohead");
            GivenOwnedTracks();

            Service().BuildOrderedResult(Request()).Should().BeNull();
        }

        [Fact]
        public void BuildOrderedResult_ReturnsNullWhenLastFmHasNothing()
        {
            GivenArtistNamed("Radiohead");
            GivenOwnedTracks(Song("Creep"));
            _topTracks.GetAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<SimilarTrack>)Array.Empty<SimilarTrack>());

            Service().BuildOrderedResult(Request()).Should().BeNull();
        }
    }
}
