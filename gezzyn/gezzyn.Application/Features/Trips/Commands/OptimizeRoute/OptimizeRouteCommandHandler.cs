using FluentValidation;
using gezzyn.Application.DTO.PlaceVisit;
using gezzyn.Application.DTO.Route;
using gezzyn.Domain.DTO;
using gezzyn.Domain.DTO.Route;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.OptimizeRoute
{
    public class OptimizeRouteCommandHandler : IRequestHandler<OptimizeRouteCommand, Response<OptimizeRouteResultDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRouteOptimizationService _routeService;
        private readonly ITripNotificationService _notificationService;
        private readonly IValidator<OptimizeRouteCommand> _validator;

        public OptimizeRouteCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRouteOptimizationService routeService, 
                                           ITripNotificationService tripNotificationService,IValidator<OptimizeRouteCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _routeService = routeService;
            _notificationService = tripNotificationService;
            _validator = validator;
        }

        public async Task<Response<OptimizeRouteResultDto>> Handle(OptimizeRouteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return new Response<OptimizeRouteResultDto>
                    {
                        Data = null,
                        Status = "Validation Error",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                    };
                }

                var trip = await _unitOfWork.Repository<Trip>()
                                 .FirstOrDefaultAsync(t => t.Id == request.TripId);

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var isMember = trip.Members.Any(m => m.UserId == userId);

                var activeVisits = trip.PlaceVisits
                                   .Where(pv => !pv.IsDeleted && pv.Place.Latitude.HasValue && pv.Place.Longitude.HasValue)
                                   .OrderBy(pv => pv.Order)
                                   .ToList();

                var points = activeVisits.Select(pv => new RoutePoint
                {
                    PlaceId = pv.PlaceId,
                    Latitude = pv.Place.Latitude!.Value,
                    Longitude = pv.Place.Longitude!.Value
                }).ToList();

                var travelMode = Enum.TryParse<TravelMode>(request.TravelMode, true, out var tm) ? tm : TravelMode.Drive;

                var optimizationResult = await _routeService.OptimizeRouteAsync(points, travelMode, cancellationToken);

                if (optimizationResult == null)
                    return new Response<OptimizeRouteResultDto>
                    {
                        Data = null,
                        Message = "Rota hesaplanamadı. Lütfen tekrar deneyin.",
                        Status = "Bad Request",
                        StatusCode = HttpStatusCode.BadRequest
                    };

                for (int i = 0; i < optimizationResult.OptimizedPlaceIds.Count; i++)
                {
                    var placeId = optimizationResult.OptimizedPlaceIds[i];
                    var visit = activeVisits.FirstOrDefault(pv => pv.PlaceId == placeId);
                    if (visit is not null)
                    {
                        visit.Order = i + 1;
                        await _unitOfWork.Repository<PlaceVisit>().UpdateAsync(visit);
                    }
                }

                var result = await _unitOfWork.SaveChangesAsync() > 0;

                var orderedVisits = new List<PlaceVisitDto>();
                var resultDto = new OptimizeRouteResultDto();

                if (result)
                {
                    orderedVisits = activeVisits
                                    .OrderBy(pv => pv.Order)
                                    .Select(pv => new PlaceVisitDto
                                    {
                                        Id = pv.Id,
                                        PlaceId = pv.PlaceId,
                                        PlaceName = pv.Place.Name,
                                        PlaceAddress = pv.Place.FormattedAddress,
                                        Latitude = pv.Place.Latitude,
                                        Longitude = pv.Place.Longitude,
                                        Order = pv.Order,
                                        Status = pv.Status.ToString(),
                                        Note = pv.Note,
                                        HasEntranceFee = pv.Place.HasEntranceFee,
                                        EntranceFeeAmount = pv.Place.EntranceFeeAmount,
                                        EntranceFeeNote = pv.Place.EntranceFeeNote,
                                        AddedByUserId = pv.AddedByUserId,
                                        AddedByUserName = pv.AddedBy.UserName
                                    })
                                    .ToList();

                    resultDto = new OptimizeRouteResultDto
                    {
                        OrderedPlaces = orderedVisits,
                        TotalDistanceMeters = optimizationResult.TotalDistanceMeters,
                        TotalDurationSeconds = optimizationResult.TotalDurationSeconds,
                        EncodedPolyline = optimizationResult.EncodedPolyline
                    };

                    await _notificationService.NotifyRouteOptimized(request.TripId, resultDto);
                }

                return new Response<OptimizeRouteResultDto>
                {
                    Data = !result ? null : resultDto,
                    Message = !result ? "Rota güncellenemedi. Lütfen tekrar deneyin." : "Rota başarıyla optimize edildi.",
                    Status = !result ? "Bad Request" : "Success",
                    StatusCode = !result ? HttpStatusCode.BadRequest : HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new Response<OptimizeRouteResultDto>
                {
                    Data = null,
                    Message = "Rota optimize edilirken bir hata oluştu. Lütfen tekrar deneyin.",
                    Status = "Internal Server Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
