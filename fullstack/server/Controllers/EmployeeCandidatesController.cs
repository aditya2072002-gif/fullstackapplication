using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/employee/candidates")]
[Authorize(Roles = EmployeeRoles.Employee)]
public sealed class EmployeeCandidatesController(IAdminCandidateService candidateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CandidateProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CandidateProfileResponse>>> GetCandidates(CancellationToken cancellationToken)
    {
        var employeeId = GetAccountId();
        return employeeId is null
            ? Forbid()
            : Ok(await candidateService.GetEmployeeCandidatesAsync(employeeId.Value, cancellationToken));
    }

    [HttpGet("{candidateId:int}")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> GetCandidate(int candidateId, CancellationToken cancellationToken)
    {
        var employeeId = GetAccountId();
        if (employeeId is null) return Forbid();
        var result = await candidateService.GetEmployeeCandidateAsync(employeeId.Value, candidateId, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CandidateProfileResponse>> CreateCandidate(
        CreateEmployeeCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetAccountId();
        if (employeeId is null) return Forbid();
        var result = await candidateService.CreateEmployeeCandidateAsync(employeeId.Value, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => CreatedAtAction(nameof(GetCandidate), new { candidateId = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{candidateId:int}")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateCandidate(
        int candidateId,
        UpdateEmployeeCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetAccountId();
        if (employeeId is null) return Forbid();
        var result = await candidateService.UpdateEmployeeCandidateAsync(employeeId.Value, candidateId, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{candidateId:int}/status")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateCandidateStatus(
        int candidateId,
        UpdateEmployeeCandidateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetAccountId();
        if (employeeId is null) return Forbid();
        var result = await candidateService.UpdateEmployeeCandidateStatusAsync(employeeId.Value, candidateId, request, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    private int? GetAccountId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var accountId) ? accountId : null;
    }
}
