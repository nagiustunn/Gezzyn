using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.LogoutAllDevices
{
    public class LogoutAllDevicesCommandHandler : IRequestHandler<LogoutAllDevicesCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public LogoutAllDevicesCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Response<bool>> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new Response<bool>
                    {
                        Data = false,
                        Errors = new List<string> { "Kullanıcı oturumu bulunamadı." },
                        Message = "Kullanıcı oturumu bulunamadı.",
                        Status = "Unauthorized",
                        StatusCode = System.Net.HttpStatusCode.Unauthorized
                    };
                }

                var tokens = await _unitOfWork.Repository<Domain.Entities.RefreshToken>().FindAsync(t => t.UserId == Guid.Parse(userId) && t.RevokedAt == null);
                foreach (var token in tokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<Domain.Entities.RefreshToken>().UpdateAsync(token);
                }

                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<bool>
                {
                    Data = result,
                    Errors = result ? new List<string>() : new List<string> { "Tüm cihazlardan çıkış işlemi başarısız." },
                    Message = result ? "Tüm cihazlardan başarıyla çıkış yapıldı." : "Tüm cihazlardan çıkış işlemi başarısız.",
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.BadRequest
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Data = false,
                    Errors = new List<string> { ex.Message },
                    Message = "Tüm cihazlardan çıkış işlemi sırasında bir hata oluştu.",
                    Status = "Error",
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }   
    }
}
