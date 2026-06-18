using gezzyn.Application.DTO.Trip;
using gezzyn.Application.Extensions;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Queries.GetMyTrips
{
    public class GetMyTripsQueryHandler : IRequestHandler<GetMyTripsQuery, Response<List<TripDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetMyTripsQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response<List<TripDto>>> Handle(GetMyTripsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var trips = await _unitOfWork.Repository<Domain.Entities.Trip>()
                                  .AsQueryable()
                                  .Where(x => !x.IsDeleted && x.Members.Any(a => a.UserId == userId && !a.IsDeleted))
                                  .OrderByDescending(x => x.CreatedAt)
                                  .ToListAsync();


                return new Response<List<TripDto>>
                {
                    Data = trips.Any() ? trips.Select(t => t.ToDto()).ToList() : null,
                    Message = trips.Any() ? "Gezi listesi getirildi" : "Daha önce oluşturduğunuz geziniz bulunmamaktadır.",
                    Status = trips.Any() ? "Success" : "Not Found",
                    StatusCode = HttpStatusCode.OK,
                };
            }
            catch (Exception ex)
            {
                return new Response<List<TripDto>>
                {
                    Data = null,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message },
                    Status = "Internal Server Errror",
                    StatusCode = HttpStatusCode.InternalServerError,
                };
            }
        }
    }
}
