using FluentValidation;

namespace gezzyn.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Ad boş olamaz.")
                .MaximumLength(100);

            RuleFor(x => x.Surname)
                .NotEmpty()
                .WithMessage("Soyad boş olamaz.")
                .MaximumLength(100);

            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage("Kullanıcı adı boş olamaz.")
                .MaximumLength(50)
                .Matches("^[a-zA-Z0-9_]+$")
                .WithMessage("Kullanıcı adı sadece harf, rakam ve _ içerebilir.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Geçerli bir e-posta girin.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
                .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermeli.")
                .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermeli.");
        }
    }
}
