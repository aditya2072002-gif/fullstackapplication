using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/admin/work-tracking")]
[Authorize(Roles = EmployeeRoles.Admin)]
public sealed class AdminWorkTrackingController(IWorkTrackingService workTrackingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CandidateWorkEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CandidateWorkEntryResponse>>> GetEntries(
        [FromQuery] int? candidateId,
        [FromQuery] string? name,
        [FromQuery] string? stack,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] int? grade,
        CancellationToken cancellationToken)
    {
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
        {
            return BadRequest(new ProblemDetails { Title = "The start date must be on or before the end date." });
        }

        if (grade is < 0 or > 100)
        {
            return BadRequest(new ProblemDetails { Title = "Grade must be between 0 and 100." });
        }

        return Ok(await workTrackingService.SearchAsync(
            candidateId, name, stack, dateFrom, dateTo, grade, cancellationToken));
    }

    [HttpPut("{entryId:int}/review")]
    [ProducesResponseType<CandidateWorkEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateWorkEntryResponse>> ReviewEntry(
        int entryId,
        ReviewCandidateWorkEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workTrackingService.ReviewAsync(entryId, request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => Ok(result.Value)
        };
    }
}
