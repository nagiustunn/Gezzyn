using FluentValidation;
using gezzyn.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace gezzyn.Application.Features.Trips.Commands.AddPlaceToTrip
{
    public class AddPlaceToTripCommandValidator : AbstractValidator<AddPlaceToTripCommand>
    {
        public AddPlaceToTripCommandValidator(IUnitOfWork _unitOfWork, IHttpContextAccessor _httpContextAccessor)
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
                        var isAlreadyAdded = trip.PlaceVisits
                                             .Any(pv => pv.TripId == tripId && pv.PlaceId == context.InstanceToValidate.PlaceId);

                        if (isAlreadyAdded)
                        {
                            context.AddFailure("Bu yer zaten bu geziye eklenmiş durumda.");
                        }

                        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var userId = Guid.Parse(userIdString);

                        var isMember = trip.Members.Any(m => m.UserId == userId);   

                        if(!isMember)
                        {
                            context.AddFailure("Bu geziye yer eklemek için yetkiniz yoktur.");
                        }
                    }
                }); 


            RuleFor(x => x.PlaceId)
                .CustomAsync(async (placeId, context, cancellationToken) =>
                {
                    var place = await _unitOfWork.Repository<Domain.Entities.Place>().GetByIdAsync(placeId);
                    if (place == null)
                    {
                        context.AddFailure("Sistemde eklemek istediğiniz yer bulunurken hata meydana gelmiştir. Daha sonra tekrar deneyiniz.");
                    }
                }); 
        }
    }
}
