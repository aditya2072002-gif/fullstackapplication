using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponseDto>>> GetNotifications(
        CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        return Ok(await notificationService.GetNotificationsAsync(accountId.Value, cancellationToken));
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        await notificationService.MarkAllAsReadAsync(accountId.Value, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dismiss(int id, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        return await notificationService.DismissAsync(accountId.Value, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private int? GetAccountId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var accountId) ? accountId : null;
    }
}
