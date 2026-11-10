using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/admin/work-tracking")]
[Authorize(Roles = EmployeeRoles.Admin + "," + EmployeeRoles.Employee)]
public sealed class AdminWorkTrackingController(IWorkTrackingService workTrackingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CandidateWorkEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CandidateWorkEntryResponse>>> GetEntries(
        [FromQuery] SearchCandidateWorkEntriesRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        var isAdmin = User.IsInRole(EmployeeRoles.Admin);
        return Ok(await workTrackingService.SearchAsync(
            request.CandidateId,
            request.Name,
            request.Stack,
            request.DateFrom,
            request.DateTo,
            request.Status,
            accountId.Value,
            isAdmin,
            cancellationToken));
    }

    [HttpPut("review")]
    [ProducesResponseType<IReadOnlyList<CandidateWorkEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<CandidateWorkEntryResponse>>> ReviewEntries(
        ReviewCandidateWorkEntriesRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerAccountId = GetAccountId();
        if (reviewerAccountId is null) return Forbid();

        var result = await workTrackingService.ReviewAsync(
            reviewerAccountId.Value,
            User.IsInRole(EmployeeRoles.Admin),
            request,
            cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }

    [HttpPut("/api/admin/candidates/{candidateId:int}/interview-decision")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateInterviewDecision(
        int candidateId,
        UpdateCandidateInterviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerAccountId = GetAccountId();
        if (reviewerAccountId is null) return Forbid();

        var result = await workTrackingService.UpdateInterviewDecisionAsync(
            reviewerAccountId.Value,
            User.IsInRole(EmployeeRoles.Admin),
            candidateId,
            request,
            cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }

    private int? GetAccountId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var accountId) ? accountId : null;
    }
}
