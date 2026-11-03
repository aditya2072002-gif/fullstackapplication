using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = EmployeeRoles.Admin)]
public sealed class EmployeeDirectoryController(IEmployeeDirectoryService directoryService) : ControllerBase
{
    [HttpGet("admins")]
    [ProducesResponseType<IReadOnlyList<AccountSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AccountSummaryResponse>> GetAdmins(CancellationToken cancellationToken)
    {
        return directoryService.GetAdminsAsync(cancellationToken);
    }

    [HttpGet("candidates")]
    [ProducesResponseType<IReadOnlyList<AccountSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AccountSummaryResponse>> GetCandidates(CancellationToken cancellationToken)
    {
        return directoryService.GetCandidatesAsync(cancellationToken);
    }
}
