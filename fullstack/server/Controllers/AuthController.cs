using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.Kind == ServiceResultKind.Success && result.Value is not null
            ? Ok(result.Value)
            : Unauthorized();
    }
}
