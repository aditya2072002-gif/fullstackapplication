using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Identity;

namespace fullstackapplication.Server.Services;

public sealed class AuthService(
    IAuthRepository repository,
    IPasswordHasher<EmployeeAccount> passwordHasher,
    JwtTokenService tokenService) : IAuthService
{
    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var account = await repository.GetActiveAccountByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
        if (account is null)
        {
            return ServiceResult<LoginResponseDto>.Invalid("Invalid username or password.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult<LoginResponseDto>.Invalid("Invalid username or password.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
            await repository.SaveChangesAsync(cancellationToken);
        }

        var (accessToken, expiresAtUtc) = tokenService.CreateToken(account);
        var user = new AccountSummaryResponseDto(account.Id, account.Username, account.DisplayName, account.Role);
        return ServiceResult<LoginResponseDto>.Success(new LoginResponseDto(accessToken, "Bearer", expiresAtUtc, user));
    }
}
