using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }   

        public async Task<Response<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await _unitOfWork.Repository<Domain.Entities.RefreshToken>().FirstOrDefaultAsync(r => r.Token == request.RefreshToken);
                if (existing == null)
                {
                    return new Response<bool>
                    {
                        Data = false,
                        Errors = new List<string> { "Geçersiz veya süresi dolmuş token." },
                        Message = "Geçersiz veya süresi dolmuş token.",
                        Status = "Error",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                existing.RevokedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Domain.Entities.RefreshToken>().UpdateAsync(existing);

                var result = await _unitOfWork.SaveChangesAsync() > 0;

                return new Response<bool>
                {
                    Data = result,
                    Errors = result ? new List<string>() : new List<string> { "Çıkış işlemi başarısız." },
                    Message = result ? "Başarıyla çıkış yapıldı." : "Çıkış işlemi başarısız.",
                    Status = result ? "Success" : "Error",
                    StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Data = false,
                    Errors = new List<string> { ex.Message },
                    Message = "Çıkış işlemi sırasında bir hata oluştu.",
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
