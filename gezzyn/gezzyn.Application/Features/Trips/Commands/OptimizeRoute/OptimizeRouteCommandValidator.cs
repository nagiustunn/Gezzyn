using FluentValidation;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.OptimizeRoute
{
    public class OptimizeRouteCommandValidator : AbstractValidator<OptimizeRouteCommand>
    {
        public OptimizeRouteCommandValidator(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor) {
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
                       var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                       var userId = Guid.Parse(userIdString);

                       var isMember = trip.Members.Any(m => m.UserId == userId);

                       if (!isMember)
                       {
                           context.AddFailure("Bu geziye yer eklemek için yetkiniz yoktur.");
                       }
                       else
                       {
                           var member = trip.Members.FirstOrDefault(m => m.UserId == userId);
                           if (member.Role == TripMemberRole.Member)
                               context.AddFailure("Rota optimize etme yetkiniz yok.");
                           else
                           {
                               var activeVisits = trip.PlaceVisits
                                                  .Where(pv => !pv.IsDeleted && pv.Place.Latitude.HasValue && pv.Place.Longitude.HasValue)
                                                  .OrderBy(pv => pv.Order)
                                                  .ToList();

                               if (activeVisits.Count < 2)
                               {
                                   context.AddFailure("Optimizasyon için en az 2 mekan gerekli (koordinatları olan).");
                               }
                           }
                       }
                   }
               });
        }
    }
}
