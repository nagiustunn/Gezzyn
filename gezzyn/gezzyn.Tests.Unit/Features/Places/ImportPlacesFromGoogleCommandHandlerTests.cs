using FluentAssertions;
using gezzyn.Application.DTO.Place;
using gezzyn.Application.Features.Places.Commands;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using Moq;

namespace gezzyn.Tests.Unit.Features.Places
{
    public class ImportPlacesFromGoogleCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGooglePlacesService> _googlePlacesServiceMock = new();
        private readonly Mock<IMeiliSearchService> _meiliSearchMock = new();
        private readonly Mock<IRepository<Place>> _placeRepoMock = new();
        private readonly ImportPlacesFromGoogleCommandHandler _handler;

        public ImportPlacesFromGoogleCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Place>()).Returns(_placeRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _meiliSearchMock.Setup(m => m.AddOrUpdateDocuments(It.IsAny<PlaceSearchDocument[]>(), It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);

            _handler = new ImportPlacesFromGoogleCommandHandler(_uowMock.Object, _googlePlacesServiceMock.Object, _meiliSearchMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldSavePlaces_WhenGoogleReturnsResults()
        {
            var googleResults = new List<GooglePlaceResult>
                                {
                                    new() { GooglePlaceId = "gp_1", Name = "Deyrulzafaran Manastırı",
                                        Latitude = 37.294, Longitude = 40.783 },
                                    new() { GooglePlaceId = "gp_2", Name = "Kasımiye Medresesi",
                                        Latitude = 37.312, Longitude = 40.731 },
                                    new() { GooglePlaceId = "gp_3", Name = "Mardin Müzesi",
                                        Latitude = 37.313, Longitude = 40.726 }
                                };

            _googlePlacesServiceMock.Setup(g => g.SearchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(googleResults);

            _placeRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Place, bool>>>()))
                          .ReturnsAsync(false);

            var cmd = new ImportPlacesFromGoogleCommand("Mardin", "tarihi mekanlar");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().HaveCount(3);
            result.Message.Should().Contain("3 yeni mekan eklendi");

            _placeRepoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Exactly(3));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);

            _meiliSearchMock.Verify(m => m.AddOrUpdateDocuments(It.IsAny<PlaceSearchDocument[]>(), "places", "id"), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSkipDuplicates_WhenPlaceAlreadyExistsInDb()
        {
            var googleResults = new List<GooglePlaceResult>
                                {
                                    new() { GooglePlaceId = "gp_existing", Name = "Zaten Var Olan Mekan",
                                        Latitude = 37.294, Longitude = 40.783 },
                                    new() { GooglePlaceId = "gp_new", Name = "Yeni Mekan",
                                        Latitude = 37.312, Longitude = 40.731 }
                                };

            _googlePlacesServiceMock.Setup(g => g.SearchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(googleResults);

            _placeRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Place, bool>>>()))
                          .ReturnsAsync((System.Linq.Expressions.Expression<Func<Place, bool>> expr) =>
                          {
                              var compiled = expr.Compile();
                              var existingPlace = new Place { GooglePlaceId = "gp_existing" };
                              return compiled(existingPlace);
                          });

            var cmd = new ImportPlacesFromGoogleCommand("Mardin", "tarihi mekanlar");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().HaveCount(1);
            result.Message.Should().Contain("1 yeni mekan eklendi");
            result.Message.Should().Contain("1 zaten vardı");

            _placeRepoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenGoogleReturnsNoResults()
        {
            _googlePlacesServiceMock.Setup(g => g.SearchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(new List<GooglePlaceResult>());

            var cmd = new ImportPlacesFromGoogleCommand("Mardin", "tarihi mekanlar");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().BeEmpty();
            result.Message.Should().Contain("Sonuç bulunamadı");

            _placeRepoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Never);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            _meiliSearchMock.Verify(m => m.AddOrUpdateDocuments(It.IsAny<PlaceSearchDocument[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenGoogleApiThrowsException()
        {
            _googlePlacesServiceMock.Setup(g => g.SearchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(new List<GooglePlaceResult>());

            var cmd = new ImportPlacesFromGoogleCommand("Mardin", "tarihi mekanlar");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Should().NotBeNull();
            _placeRepoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldNotIndexToMeilisearch_WhenNoNewPlacesAdded()
        {
            var googleResults = new List<GooglePlaceResult>
                                {
                                    new() { GooglePlaceId = "gp_1", Name = "Var Olan Mekan 1",
                                        Latitude = 37.294, Longitude = 40.783 }
                                };

            _googlePlacesServiceMock.Setup(g => g.SearchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(googleResults);

            _placeRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Place, bool>>>()))
                          .ReturnsAsync(true);

            var cmd = new ImportPlacesFromGoogleCommand("Mardin", "tarihi mekanlar");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _meiliSearchMock.Verify(m => m.AddOrUpdateDocuments(It.IsAny<PlaceSearchDocument[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
