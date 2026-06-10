using FluentValidation;

namespace gezzyn.Application.Features.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Geçerli bir e-posta girin.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Şifre boş olamaz.");
        }
    }
}
