using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class EmployeeRolesController(EmployeeAccessService accessService, IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAssignableRoles(CancellationToken cancellationToken)
    {
        var canAssignRoles = await permissionService.HasAsync(User, EmployeePermissions.GrantPermissions, cancellationToken);
        var canManageRolePermissions = await permissionService.HasAsync(User, EmployeePermissions.ManageRolePermissions, cancellationToken);
        if (!canAssignRoles && !canManageRolePermissions) return Forbid();
        return Ok(await accessService.GetAssignableRolesAsync(User, cancellationToken));
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetPermissionsAsync(User, cancellationToken);
        var mayManageRolePermissions = await permissionService.HasAsync(User, EmployeePermissions.ManageRolePermissions, cancellationToken);
        return Ok(new
        {
            permissions,
            catalog = mayManageRolePermissions
                ? EmployeePermissions.Catalog.Select(item => new PermissionOptionDto(item.Key, item.Value)).ToArray()
                : Array.Empty<PermissionOptionDto>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateEmployeeRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await accessService.CreateRoleAsync(User, request.Name, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            ServiceResultKind.Conflict => Conflict(new ProblemDetails { Title = result.Error }),
            _ => CreatedAtAction(nameof(GetAssignableRoles), result.Value)
        };
    }

    [HttpPut("{roleId:int}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(
        int roleId,
        UpdateEmployeeRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accessService.UpdateRolePermissionsAsync(User, roleId, request.Permissions, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }

    [HttpPut("employees/{employeeId:int}")]
    public async Task<IActionResult> AssignRole(
        int employeeId,
        AssignEmployeeRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accessService.AssignRoleAsync(User, employeeId, request.RoleDefinitionId, cancellationToken);
        return result.Kind switch
        {
            ServiceResultKind.Forbidden => Forbid(),
            ServiceResultKind.NotFound => NotFound(),
            ServiceResultKind.Invalid => BadRequest(new ProblemDetails { Title = result.Error }),
            _ => NoContent()
        };
    }
}
