using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/admin/employees")]
[Authorize(Roles = EmployeeRoles.Admin)]
public sealed class AdminEmployeesController(IEmployeeManagementService employeeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EmployeeResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponseDto>>> GetEmployees(CancellationToken cancellationToken) =>
        Ok(await employeeService.GetEmployeesAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType<EmployeeResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponseDto>> CreateEmployee(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.CreateEmployeeAsync(request, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => CreatedAtAction(nameof(GetEmployees), result.Value)
        };
    }
}
