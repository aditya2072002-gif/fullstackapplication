using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IAuthService
{
    Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
