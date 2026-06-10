using FluentValidation;
using gezzyn.Application.DTO.Auth;
using gezzyn.Application.DTO.User;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Response<AuthResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IValidator<RegisterCommand> _validator;

        public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ITokenService tokenService, IValidator<RegisterCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _validator = validator;
        }

        public async Task<Response<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {

                    return new Response<AuthResponseDto>
                    {
                        Status = "ValidationError",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = "Validation failed",
                        Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                        Data = null
                    };
                }

                var emailExists = await _unitOfWork.Repository<User>().AnyAsync(u => u.Email == request.Email.ToLowerInvariant());

                if (emailExists)
                {
                    return new Response<AuthResponseDto>
                    {
                        Status = "Bad Request",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = "Bu e-posta adresi zaten kullanılıyor.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

                var userNameExists = await _unitOfWork.Repository<User>().AnyAsync(u => u.UserName == request.UserName.ToLowerInvariant());

                if (userNameExists)
                {
                    return new Response<AuthResponseDto>
                    {
                        Status = "Bad Request",
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = "Bu kullanıcı adı zaten alınmış.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

                var user = new User
                {
                    Name = request.Name.Trim(),
                    Surname = request.Surname.Trim(),
                    UserName = request.UserName.Trim(),
                    Email = request.Email.Trim().ToLowerInvariant()
                };

                user.PasswordHash = _passwordService.Hash(request.Password);

                await _unitOfWork.Repository<User>().AddAsync(user);

                var accessToken = _tokenService.GenerateToken(user);
                var refreshTokenStr = _tokenService.GenerateRefreshToken();

                var refreshToken = new Domain.Entities.RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenStr,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(refreshToken);
                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<AuthResponseDto>
                {
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                    Message = result ? "Kayıt başarılı." : "Kayıt sırasında bir hata oluştu.",
                    Errors = new List<string>(),
                    Data = result ? new AuthResponseDto
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshTokenStr,
                        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Surname = user.Surname,
                            UserName = user.UserName,
                            Email = user.Email,
                            AvatarUrl = user.AvatarUrl
                        }
                    } : null
                };
            }
            catch (Exception ex)
            {

                return new Response<AuthResponseDto>
                {
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Message = "Kayıt sırasında bir hata oluştu.",
                    Errors = new List<string> { ex.Message },
                    Data = null
                };
            }
        }
    }
}
