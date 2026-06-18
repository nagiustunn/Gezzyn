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

        public ReOrderPlacesCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response<bool>> Handle(ReOrderPlacesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var trip = await _unitOfWork.Repository<Domain.Entities.Trip>()
                                 .FirstOrDefaultAsync(x => x.Id == request.TripId);

                if (trip == null)
                    return new Response<bool>
                    {
                        Data = false,
                        Message = "Gezi bulunamadı.",
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.NotFound
                    };

                if ((trip.Members == null || (trip.Members != null && !trip.Members.Any())) &&
                   (trip.PlaceVisits == null || (trip.PlaceVisits != null && !trip.PlaceVisits.Any())))
                    return new Response<bool>
                    {
                        Data = false,
                        Message = "Gezinin detayları bulunamadı. Lütfen tekrar deneyiniz.",
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.NotFound
                    };

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Guid.Parse(userIdString);

                var member = trip.Members.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);

                if (member == null)
                    return new Response<bool>
                    {
                        Data = false,
                        Message = "Bu geziye erişim yetkiniz yoktur.",
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized
                    };

                if (member.Role == TripMemberRole.Member)
                    return new Response<bool>
                    {
                        Data = false,
                        Message = "Rota düzenleme yetkiniz yoktur.",
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized
                    };

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
