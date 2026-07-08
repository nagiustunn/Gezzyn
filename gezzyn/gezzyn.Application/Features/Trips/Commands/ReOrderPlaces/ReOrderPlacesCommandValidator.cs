using FluentValidation;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.ReOrderPlaces
{
    public class ReOrderPlacesCommandValidator : AbstractValidator<ReOrderPlacesCommand>
    {
        public ReOrderPlacesCommandValidator(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor)
        {
            RuleFor(x => x.TripId)
                .CustomAsync(async (tripId, context, cancellationToken) =>
                {
                    var trip = await _unitOfWork.Repository<Domain.Entities.Trip>().GetByIdAsync(tripId);
                    if (trip == null)
                    {
                        context.AddFailure("Sistemde yer eklemek istediğiniz gezi bulunurken hata meydana gelmiştir. Daha sonra tekrar deneyiniz.");
                    }
                    else
                    {
                        if ((trip.Members == null || (trip.Members != null && !trip.Members.Any())) &&
                            (trip.PlaceVisits == null || (trip.PlaceVisits != null && !trip.PlaceVisits.Any())))
                        {
                            context.AddFailure("Gezinin detayları bulunamadı. Lütfen tekrar deneyiniz");
                        }
                        else
                        {
                            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var userId = Guid.Parse(userIdString);

                            var isMember = trip.Members.Any(m => m.UserId == userId);

                            if (!isMember)
                            {
                                context.AddFailure("Bu geziye erişim yetkiniz yoktur.");
                            }
                            else
                            {
                                var member = trip.Members.FirstOrDefault(m => m.UserId == userId);
                                if (member.Role == TripMemberRole.Member)
                                    context.AddFailure("Rota düzenleme yetkiniz yoktur.");
                                
                            }
                        }
                    }
                });
        }
    }
}
