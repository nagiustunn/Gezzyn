using gezzyn.Application.DTO.Trip;
using gezzyn.Application.Extensions;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Queries.GetTripById
{
    public class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, Response<TripDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetTripByIdQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response<TripDto>> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var trip = await _unitOfWork.Repository<Domain.Entities.Trip>()
                                 .FirstOrDefaultAsync(x => x.Id == request.TripId && !x.IsDeleted);

                if (trip == null)
                    return new Response<TripDto>
                    {
                        Data = null,
                        Message = "Gezi bulunamadı.",
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.NotFound,
                    };

                if ((trip.Members == null || (trip.Members != null && !trip.Members.Any())))
                    return new Response<TripDto>
                    {
                        Data = null,
                        Message = "Gezinin detayları bulunamadı. Lütfen tekrar deneyiniz.",
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.NotFound
                    };

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var member = trip.Members.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);

                if (member == null)
                    return new Response<TripDto>
                    {
                        Data = null,
                        Message = "Bu geziye erişim yetkiniz yoktur.",
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized
                    };

                var result = trip.ToDto();

                return new Response<TripDto>
                {
                    Data = trip.ToDto(),
                    Message = "Gezi detayı listelenmiştir.",
                    Status = "Success",
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new Response<TripDto>
                {
                    Data = null,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message},
                    Status = "Internal Server Error",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
