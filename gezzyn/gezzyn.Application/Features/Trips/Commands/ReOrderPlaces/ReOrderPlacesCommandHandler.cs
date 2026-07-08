using FluentValidation;
using gezzyn.Application.DTO.Route;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.ReOrderPlaces
{
    public class ReOrderPlacesCommandHandler : IRequestHandler<ReOrderPlacesCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITripNotificationService _notificationService;
        private readonly IValidator<ReOrderPlacesCommand> _validator;

        public ReOrderPlacesCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ITripNotificationService notificationService, 
                                           IValidator<ReOrderPlacesCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(ReOrderPlacesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return new Response<bool>
                    {
                        Data = false,
                        Status = "Validation Error",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                    };
                }

                var trip = await _unitOfWork.Repository<Domain.Entities.Trip>()
                                 .FirstOrDefaultAsync(x => x.Id == request.TripId);

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var member = trip.Members.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);

                var count = 0;

                foreach (var item in request.OrderPlacesIds)
                {
                    var visit = trip.PlaceVisits.FirstOrDefault(x => x.Id == item && !x.IsDeleted);

                    if (visit != null)
                    {
                        visit.Order = count + 1;
                        await _unitOfWork.Repository<PlaceVisit>().UpdateAsync(visit);
                    }

                    count++;
                }

                var result = await _unitOfWork.SaveChangesAsync() > 0;

                if(result)
                    await _notificationService.NotifyPlacesReordered(request.TripId, request.OrderPlacesIds);

                return new Response<bool>
                {
                    Data = result,
                    Message = result ? "Rota sırası güncellendi." : "Rota sırası güncellenirken hata meydana geldi",
                    Status = result ? "Success" : "Internal Server Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Data = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message },
                    Status = "Internal Server Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                };
            }
        }
    }
}
