using gezzyn.Application.DTO.User;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Interfaces;
using MediatR;
using System.Net;

namespace gezzyn.Application.Features.Auth.Queries.GetAuthUser
{
    public class GetAuthUserQueryHandler : IRequestHandler<GetAuthUserQuery, Response<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetAuthUserQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Response<UserDto>> Handle(GetAuthUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new Response<UserDto>
                    {
                        Status = "Unauthorized",
                        StatusCode = HttpStatusCode.Unauthorized,
                        Message = "Kullanıcı oturumu bulunamadı.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

                var user = await _unitOfWork.Repository<Domain.Entities.User>().GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return new Response<UserDto>
                    {
                        Status = "Not Found",
                        StatusCode = HttpStatusCode.NotFound,
                        Message = "Kullanıcı bulunamadı.",
                        Errors = new List<string>(),
                        Data = null
                    };
                }

                var result = new UserDto
                {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Surname = user.Surname,
                };

                return new Response<UserDto>
                {
                    Status = "Success",
                    StatusCode = HttpStatusCode.OK,
                    Message = "Kullanıcı bilgileri başarıyla getirildi.",
                    Errors = new List<string>(),
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new Response<UserDto>
                {
                    Status = "Error",
                    StatusCode = HttpStatusCode.InternalServerError,
                    Message = "Kullanıcı bilgileri getirilirken bir hata oluştu.",
                    Errors = new List<string> { ex.Message },
                    Data = null
                };
            }
        }
    }
}
