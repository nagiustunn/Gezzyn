using FluentValidation;
using gezzyn.Application.DTO.Auth;
using gezzyn.Application.DTO.User;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Response<AuthResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IValidator<LoginCommand> _validator;

        public LoginCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ITokenService tokenService, IValidator<LoginCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _validator = validator;
        }

        public async Task<Response<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
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

                var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());
                if (user == null)
                {
                    return new Response<AuthResponseDto>
                    {
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized,
                        Message = "Geçersiz e-posta veya şifre.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

                var isPasswordValid = _passwordService.Verify(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    return new Response<AuthResponseDto>
                    {
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized,
                        Message = "Geçersiz e-posta veya şifre.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

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

                return result
                    ? new Response<AuthResponseDto>
                    {
                        Status = "Success",
                        StatusCode = HttpStatusCode.OK,
                        Message = "Giriş başarılı.",
                        Errors = new List<string>(),
                        Data = new AuthResponseDto
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshTokenStr,
                            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                            User = new UserDto
                            {
                                Id = user.Id,
                                Name = user.Name,
                                Email = user.Email
                            }
                        }
                    }
                    : new Response<AuthResponseDto>
                    {
                        Status = "Error",
                        StatusCode = HttpStatusCode.InternalServerError,
                        Message = "Giriş sırasında bir hata oluştu.",
                        Errors = new List<string>(),
                        Data = null
                    };
            }
            catch (Exception ex)
            {

                return new Response<AuthResponseDto>
                {
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message},
                    Data = null
                };
            }
        }
    }
}
