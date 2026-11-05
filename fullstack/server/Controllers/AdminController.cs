using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/admin/candidates")]
[Authorize(Roles = EmployeeRoles.Admin)]
public sealed class AdminController(IAdminCandidateService candidateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CandidateProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CandidateProfileResponse>>> GetCandidates(CancellationToken cancellationToken) =>
        Ok(await candidateService.GetCandidatesAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> GetCandidate(int id, CancellationToken cancellationToken)
    {
        var result = await candidateService.GetCandidateAsync(id, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpGet("{id:int}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCandidateResume(int id, CancellationToken cancellationToken)
    {
        var result = await candidateService.GetCandidateResumeAsync(id, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound || result.Value is null
            ? NotFound()
            : File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CandidateResumeUploadValidator.MaxRequestSizeBytes)]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CandidateProfileResponse>> CreateCandidate(
        [FromForm] CreateCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await candidateService.CreateCandidateAsync(request, cancellationToken);
        if (result.Kind == ServiceResultKind.Conflict)
        {
            return Conflict(new ProblemDetails { Title = result.Error });
        }

        if (result.Kind == ServiceResultKind.Invalid)
        {
            return BadRequest(new ProblemDetails { Title = result.Error });
        }

        return CreatedAtAction(nameof(GetCandidate), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CandidateResumeUploadValidator.MaxRequestSizeBytes)]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateCandidate(
        int id,
        [FromForm] UpdateCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await candidateService.UpdateCandidateAsync(id, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }

    [HttpPatch("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType<CandidateProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileResponse>> UpdateCandidateStatus(
        int id,
        [FromBody] UpdateCandidateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await candidateService.UpdateCandidateStatusAsync(id, request, cancellationToken);
        return result.Kind == ServiceResultKind.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCandidate(int id, CancellationToken cancellationToken)
    {
        var result = await candidateService.DeleteCandidateAsync(id, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }
}
