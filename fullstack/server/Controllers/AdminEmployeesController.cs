using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using System.Globalization;
using System.Security.Claims;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/admin/employees")]
[Authorize(Roles = EmployeeRoles.Admin + "," + EmployeeRoles.Employee)]
public sealed class AdminEmployeesController(
    IEmployeeManagementService employeeService,
    EmployeeAccessService accessService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EmployeeResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponseDto>>> GetEmployees(CancellationToken cancellationToken) =>
        User.IsInRole(EmployeeRoles.Admin)
            ? Ok(await employeeService.GetEmployeesAsync(cancellationToken))
            : Forbid();

    private int? GetAccountId()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var accountId) ? accountId : null;
    }

    [HttpPatch("{employeeId:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

    [HttpPost]
    [ProducesResponseType<EmployeeResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponseDto>> CreateEmployee(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var callerId = GetAccountId();
        var isAdmin = User.IsInRole(EmployeeRoles.Admin);
        if (!isAdmin && callerId is null) return Forbid();
        var result = await employeeService.CreateEmployeeAsync(request, callerId, isAdmin, User, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Forbidden => Forbid(),
            _ when isAdmin => CreatedAtAction(nameof(GetEmployees), result.Value),
            _ => Created("/api/employee/managed-employees", result.Value)
        };
    }
}
