using gezzyn.Application.DTO.Place;
using gezzyn.Application.Extensions;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Places.Commands
{
    public class ImportPlacesFromGoogleCommandHandler : IRequestHandler<ImportPlacesFromGoogleCommand, Response<List<PlaceDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly IMeiliSearchService _meiliSearchService;

        public ImportPlacesFromGoogleCommandHandler(IUnitOfWork unitOfWork, IGooglePlacesService googlePlacesService, IMeiliSearchService meiliSearchService)
        {
            _unitOfWork = unitOfWork;
            _googlePlacesService = googlePlacesService;
            _meiliSearchService = meiliSearchService;
        }

        public async Task<Response<List<PlaceDto>>> Handle(ImportPlacesFromGoogleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var googleResults = await _googlePlacesService.SearchPlacesAsync(request.SearchQuery, request.City, cancellationToken);

                if (googleResults.Count == 0)
                    return new Response<List<PlaceDto>>
                    {
                        Data = null,
                        Message = "Sonuç Bulunamadı",
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.OK
                    };

                var placeRepo = _unitOfWork.Repository<Place>();
                var savedPlaces = new List<Place>();

                foreach (var googleResult in googleResults)
                {
                    var exists = await placeRepo.AnyAsync(p => p.GooglePlaceId == googleResult.GooglePlaceId);
                    if (exists)
                        continue;

                    var place = googleResult.ToPlaceEntity(request.City);
                    await placeRepo.AddAsync(place);
                    savedPlaces.Add(place);
                }

                var result = await _unitOfWork.SaveChangesAsync() > 0;

                if (result && savedPlaces.Count > 0)
                {
                    var searchDocs = savedPlaces.Select(p => p.ToSearchDocument()).ToArray();
                    await _meiliSearchService.AddOrUpdateDocuments(searchDocs, "places", "id");
                }

                return new Response<List<PlaceDto>>
                {
                    Data = result ? savedPlaces.Select(p => p.ToDto()).ToList() : null,
                    Message = result ? $"{savedPlaces.Count} yeni mekan eklendi. ({googleResults.Count - savedPlaces.Count} zaten vardı)" : "Mekanlar eklenirken hata meydana geldi",
                    Status = result ? "Success" : "Internal Server Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                };
            }
            catch (Exception ex)
            {
                return new Response<List<PlaceDto>>
                {
                    Data = null,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message },
                    Status = "Internal Server Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                };
            }
        }
    }
}
