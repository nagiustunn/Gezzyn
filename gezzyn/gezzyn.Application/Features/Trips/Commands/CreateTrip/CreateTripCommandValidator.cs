using FluentValidation;

namespace gezzyn.Application.Features.Trips.Commands.CreateTrip
{
    public class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
    {
        public CreateTripCommandValidator()
        {
            RuleFor(x => x.CreateTripDto.Title)
                .NotEmpty()
                .WithMessage("Gezi başlığı boş olamaz.")
                .MaximumLength(200);

            RuleFor(x => x.CreateTripDto.City)
                .NotEmpty()
                .WithMessage("Şehir boş olamaz.")
                .MaximumLength(100);

            RuleFor(x => x.CreateTripDto.Description)
                .MaximumLength(1000)
                .When(x => x.CreateTripDto.Description is not null);
        }
    }
}
