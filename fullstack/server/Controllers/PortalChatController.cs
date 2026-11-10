using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = EmployeeRoles.Admin + "," + EmployeeRoles.Employee + "," + EmployeeRoles.Candidate)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PortalChatController(IPortalChatService chatService) : ControllerBase
{
    [HttpPost("ask")]
    [ProducesResponseType<PortalChatAnswerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PortalChatAnswerDto>> Ask(
        PortalChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await chatService.AskAsync(User, request.Message, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Success => Ok(result.Value),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Forbidden => Forbid(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
