using FluentValidation;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.AddPlaceToTrip
{
    public class AddPlaceToTripCommandHandler : IRequestHandler<AddPlaceToTripCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<AddPlaceToTripCommand> _validator;

        public AddPlaceToTripCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IValidator<AddPlaceToTripCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(AddPlaceToTripCommand request, CancellationToken cancellationToken)
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


                var trip = await _unitOfWork.Repository<Domain.Entities.Trip>().GetByIdAsync(request.TripId);

                var order = trip.PlaceVisits.Count(pv => !pv.IsDeleted) + 1;

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var placeVisit = new Domain.Entities.PlaceVisit
                {
                    PlaceId = request.PlaceId,
                    TripId = request.TripId,
                    Order = order,
                    AddedByUserId = userId,
                };

                await _unitOfWork.Repository<Domain.Entities.PlaceVisit>().AddAsync(placeVisit);
                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<bool>
                {
                    Data = result,
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    Message = result ? "Place added to trip successfully." : "Failed to add place to trip."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Data = false,
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Message = $"An error occurred while adding the place to the trip: {ex.Message}"
                };
            }
        }
    }
}
