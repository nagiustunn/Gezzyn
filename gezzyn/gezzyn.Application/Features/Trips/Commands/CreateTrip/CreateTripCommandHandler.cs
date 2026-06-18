using FluentValidation;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.CreateTrip
{
    public class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<CreateTripCommand> _validator;

        public CreateTripCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IValidator<CreateTripCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return new Response<bool>
                    {
                        Status = "ValidationError",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = "Validation failed",
                        Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                        Data = false
                    };
                }

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var trip = new Domain.Entities.Trip
                {
                    Title = request.CreateTripDto.Title,
                    City = request.CreateTripDto.City,
                    Description = request.CreateTripDto.Description,
                    StartDate = request.CreateTripDto.StartDate,
                    EndDate = request.CreateTripDto.EndDate,
                    CreatedByUserId = userId
                };

                trip.Members = new List<Domain.Entities.TripMember>
                {
                    new Domain.Entities.TripMember
                    {
                        TripId = trip.Id,
                        UserId = userId,
                        Role = TripMemberRole.Admin
                    }
                };

                await _unitOfWork.Repository<Domain.Entities.Trip>().AddAsync(trip);
                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<bool>
                {
                    Data = result,
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                    Message = result ? "Trip created successfully" : "Failed to create trip"
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Data = false,
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Message = $"An error occurred while creating the trip: {ex.Message}"
                };  
            }
        }
    }
}
