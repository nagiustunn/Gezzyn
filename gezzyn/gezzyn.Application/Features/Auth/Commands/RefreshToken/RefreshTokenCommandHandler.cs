using gezzyn.Application.DTO.Auth;
using gezzyn.Application.DTO.User;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Response<AuthResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<Response<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _unitOfWork.Repository<Domain.Entities.RefreshToken>().FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

                if (existing == null)
                {
                    return new Response<AuthResponseDto>
                    {
                        Data = null,
                        Errors = new List<string> { "Geçersiz veya süresi dolmuş token." },
                        Message = "Geçersiz veya süresi dolmuş token.",
                        Status = "Error",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                var user = await _unitOfWork.Repository<Domain.Entities.User>().GetByIdAsync(existing.UserId);

                if (user == null)
                {
                    return new Response<AuthResponseDto>
                    {
                        Data = null,
                        Errors = new List<string> { "Kullanıcı bulunamadı." },
                        Message = "Kullanıcı bulunamadı.",
                        Status = "Error",
                        StatusCode = HttpStatusCode.NotFound
                    };
                }

                existing.RevokedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<Domain.Entities.RefreshToken>().UpdateAsync(existing);


                var newAccessToken = _tokenService.GenerateToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                var refreshToken = new Domain.Entities.RefreshToken
                {
                    UserId = user.Id,
                    Token = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(refreshToken);
                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<AuthResponseDto>
                {
                    Data = result ? new AuthResponseDto
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Name = user.Name
                        }
                    } : null,
                    Errors = new List<string>(),
                    Message = result ? "Token başarıyla yenilendi." : "Token yenileme işlemi başarısız.",
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest
                };
            }
            catch (Exception ex)
            {

                return new Response<AuthResponseDto>
                {
                    Data = null,
                    Errors = new List<string> { ex.Message },
                    Message = "Token yenileme işlemi sırasında bir hata oluştu.",
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
