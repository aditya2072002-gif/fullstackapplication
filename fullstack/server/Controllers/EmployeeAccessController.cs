using System.Globalization;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/employee/managed-employees")]
[Authorize(Roles = EmployeeRoles.Employee)]
public sealed class EmployeeAccessController(
    EmployeeAccessService accessService,
    IEmployeeManagementService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetManagedEmployees(CancellationToken cancellationToken)
    {
        var result = await accessService.GetManagedEmployeesAsync(User, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            _ => Ok(result.Value)
        };
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await accessService.GetManagedEmployeeSummaryAsync(User, cancellationToken);
        return result.Kind == ServiceResultKind.Forbidden ? Forbid() : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType<EmployeeResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Forbid();
        var result = await employeeService.CreateEmployeeAsync(request, accountId, false, User, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Forbidden => Forbid(),
            _ => CreatedAtAction(nameof(GetManagedEmployees), result.Value)
        };
    }

    [HttpDelete("{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEmployee(int employeeId, CancellationToken cancellationToken)
    {
        var result = await accessService.DeleteManagedEmployeeAsync(User, employeeId, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }

    [HttpPatch("{employeeId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int employeeId,
        UpdateEmployeeManagedStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accessService.UpdateManagedEmployeeStatusAsync(User, employeeId, request.IsActive!.Value, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            ServiceResultKind.NotFound => NotFound(),
            _ => NoContent()
        };
    }

    private int? GetAccountId()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var accountId) ? accountId : null;
    }
}
