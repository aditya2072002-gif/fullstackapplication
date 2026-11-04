using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/candidates/me")]
[Authorize(Roles = EmployeeRoles.Candidate)]
public sealed class CandidateController(
    ICandidateSelfService candidateService,
    IWorkTrackingService workTrackingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        var result = await candidateService.GetMyProfileAsync(accountId.Value, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpPut("stack")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateMyStack(
        UpdateCandidateStackRequest request, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        var result = await candidateService.UpdateMyStackAsync(accountId.Value, request, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpGet("work-tracking")]
    [ProducesResponseType<IReadOnlyList<CandidateWorkEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CandidateWorkEntryResponse>>> GetMyWorkEntries(CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        if ((await candidateService.GetMyProfileAsync(accountId.Value, cancellationToken)).Kind == ServiceResultKind.NotFound)
        {
            return NotFound();
        }

        return Ok(await workTrackingService.GetCandidateEntriesAsync(accountId.Value, cancellationToken));
    }

    [HttpPost("work-tracking")]
    [ProducesResponseType<CandidateWorkEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CandidateWorkEntryResponse>> CreateWorkEntry(
        CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        if ((await candidateService.GetMyProfileAsync(accountId.Value, cancellationToken)).Kind == ServiceResultKind.NotFound)
        {
            return NotFound();
        }

        var result = await workTrackingService.CreateAsync(accountId.Value, request, cancellationToken);
        return Created($"/api/candidates/me/work-tracking/{result.Value!.Id}", result.Value);
    }

    [HttpPut("work-tracking/{entryId:int}")]
    [ProducesResponseType<CandidateWorkEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateWorkEntryResponse>> UpdateWorkEntry(
        int entryId, CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        if ((await candidateService.GetMyProfileAsync(accountId.Value, cancellationToken)).Kind == ServiceResultKind.NotFound)
        {
            return NotFound();
        }

        var result = await workTrackingService.UpdateCandidateEntryAsync(accountId.Value, entryId, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }

    [HttpDelete("work-tracking/{entryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteWorkEntry(int entryId, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        var result = await workTrackingService.DeleteCandidateEntryAsync(accountId.Value, entryId, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }

    [HttpPut("resume")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CandidateResumeUploadValidator.MaxRequestSizeBytes)]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateMyResume(
        [FromForm] ResumeUploadRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        var result = await candidateService.UpdateMyResumeAsync(accountId.Value, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeMyPassword(
        ChangeCandidatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null)
        {
            return Forbid();
        }

        var result = await candidateService.ChangeMyPasswordAsync(accountId.Value, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
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
