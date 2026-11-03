using fullstackapplication.Server.Contracts;
using fullstackapplication.Server.Data;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ApplicationDbContext dbContext,
    IPasswordHasher<EmployeeAccount> passwordHasher,
    JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var account = await dbContext.EmployeeAccounts.SingleOrDefaultAsync(
            employee => employee.NormalizedUsername == normalizedUsername && employee.IsActive,
            cancellationToken);
        if (account is null)
        {
            return Unauthorized();
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var (accessToken, expiresAtUtc) = tokenService.CreateToken(account);
        var user = new AccountSummaryResponse(account.Id, account.Username, account.DisplayName, account.Role);
        return Ok(new LoginResponse(accessToken, "Bearer", expiresAtUtc, user));
    }
}
