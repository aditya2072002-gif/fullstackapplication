using System.Globalization;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class EmployeeAccessService(EmployeeAccessRepository repository, IPermissionService permissionService)
{
    public async Task<IReadOnlyList<EmployeeRoleResponseDto>> GetAssignableRolesAsync(
        ClaimsPrincipal caller,
        CancellationToken cancellationToken)
    {
        var roles = await repository.GetRolesAsync(cancellationToken);
        if (caller.IsInRole(EmployeeRoles.Admin)) return roles.Select(ToResponse).ToArray();
        var grants = await permissionService.GetPermissionsAsync(caller, cancellationToken);
        return roles.Where(role => role.Permissions.All(item => grants.Contains(item.PermissionCode)))
            .Select(ToResponse).ToArray();
    }

    public async Task<ServiceResult<EmployeeRoleResponseDto>> CreateRoleAsync(
        ClaimsPrincipal caller,
        string name,
        CancellationToken cancellationToken)
    {
        var roleName = name.Trim();
        if (roleName.Length is < 2 or > 80)
        {
            return ServiceResult<EmployeeRoleResponseDto>.Invalid("Enter a role name between 2 and 80 characters.");
        }

        if (!await permissionService.HasAsync(caller, EmployeePermissions.CreateRoles, cancellationToken))
        {
            return ServiceResult<EmployeeRoleResponseDto>.Forbidden("You do not have permission to create roles.");
        }

        if (await repository.RoleNameExistsAsync(roleName.ToUpperInvariant(), cancellationToken))
        {
            return ServiceResult<EmployeeRoleResponseDto>.Conflict("A role with this name already exists.");
        }

        int? creatorId = TryGetAccountId(caller, out var accountId) ? accountId : null;
        var role = new EmployeeRoleDefinition
        {
            Name = roleName,
            CreatedByEmployeeAccountId = creatorId
        };
        await repository.AddRoleAsync(role, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<EmployeeRoleResponseDto>.Success(ToResponse(role));
    }

    public async Task<ServiceResult<bool>> UpdateRolePermissionsAsync(
        ClaimsPrincipal caller,
        int roleId,
        IReadOnlyList<string> requestedPermissions,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasAsync(caller, EmployeePermissions.ManageRolePermissions, cancellationToken))
        {
            return ServiceResult<bool>.Forbidden("You do not have permission to manage role permissions.");
        }

        var role = await repository.GetRoleAsync(roleId, cancellationToken);
        if (role is null) return ServiceResult<bool>.NotFound();

        if (requestedPermissions is null)
        {
            return ServiceResult<bool>.Invalid("Provide a list of supported permissions.");
        }
        var permissions = requestedPermissions.Distinct(StringComparer.Ordinal).ToArray();
        if (permissions.Any(permission => !EmployeePermissions.Catalog.ContainsKey(permission)))
        {
            return ServiceResult<bool>.Invalid("The role contains an unsupported permission.");
        }
        if (permissions.Any(permission => permission is EmployeePermissions.CreateCandidates
                or EmployeePermissions.UpdateCandidates or EmployeePermissions.DeactivateCandidates
                or EmployeePermissions.DeleteCandidates)
            && !permissions.Contains(EmployeePermissions.ViewCandidates))
        {
            return ServiceResult<bool>.Invalid("Candidate management permissions require Candidates.View.");
        }
        if (permissions.Contains(EmployeePermissions.DecideInterviews)
            && !permissions.Contains(EmployeePermissions.ReviewWork))
        {
            return ServiceResult<bool>.Invalid("Interview decisions require Reviews.Work.");
        }

        var grantable = await permissionService.GetPermissionsAsync(caller, cancellationToken);
        if (!caller.IsInRole(EmployeeRoles.Admin) && permissions.Any(permission => !grantable.Contains(permission)))
        {
            return ServiceResult<bool>.Forbidden("A role cannot grant permissions you do not have.");
        }

        var requestedPermissionSet = permissions.ToHashSet(StringComparer.Ordinal);
        foreach (var existingPermission in role.Permissions
            .Where(item => !requestedPermissionSet.Contains(item.PermissionCode)).ToArray())
        {
            role.Permissions.Remove(existingPermission);
        }
        var existingPermissionSet = role.Permissions.Select(item => item.PermissionCode).ToHashSet(StringComparer.Ordinal);
        foreach (var permission in permissions.Where(permission => !existingPermissionSet.Contains(permission)))
        {
            role.Permissions.Add(new EmployeeRolePermission { PermissionCode = permission });
        }
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> AssignRoleAsync(
        ClaimsPrincipal caller,
        int employeeId,
        int? roleId,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasAsync(caller, EmployeePermissions.GrantPermissions, cancellationToken))
        {
            return ServiceResult<bool>.Forbidden("You do not have permission to assign employee roles.");
        }
        if (!TryGetAccountId(caller, out var managerId)) return ServiceResult<bool>.Forbidden("The account identity is unavailable.");
        var employee = await repository.GetManagedEmployeeAsync(employeeId, managerId, caller.IsInRole(EmployeeRoles.Admin), cancellationToken);
        if (employee is null) return ServiceResult<bool>.NotFound();

        if (roleId.HasValue)
        {
            var role = await repository.GetRoleAsync(roleId.Value, cancellationToken);
            if (role is null) return ServiceResult<bool>.Invalid("The selected role does not exist.");
            var grantable = await permissionService.GetPermissionsAsync(caller, cancellationToken);
            if (!caller.IsInRole(EmployeeRoles.Admin)
                && role.Permissions.Any(permission => !grantable.Contains(permission.PermissionCode)))
            {
                return ServiceResult<bool>.Forbidden("You cannot assign a role with permissions you do not have.");
            }
        }

        employee.RoleDefinitionId = roleId;
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyList<EmployeeResponseDto>>> GetManagedEmployeesAsync(
        ClaimsPrincipal caller,
        CancellationToken cancellationToken)
    {
        var canAccessTeam = await permissionService.HasAsync(caller, EmployeePermissions.ViewTeam, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.CreateEmployees, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.ManageEmployees, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.DeleteEmployees, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.CreateRoles, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.GrantPermissions, cancellationToken)
            || await permissionService.HasAsync(caller, EmployeePermissions.ManageRolePermissions, cancellationToken);
        if (!canAccessTeam)
        {
            return ServiceResult<IReadOnlyList<EmployeeResponseDto>>.Forbidden("You do not have permission to view managed employees.");
        }
        if (!TryGetAccountId(caller, out var accountId))
        {
            return ServiceResult<IReadOnlyList<EmployeeResponseDto>>.Forbidden("The account identity is unavailable.");
        }
        var employees = await repository.GetManagedEmployeesAsync(accountId, cancellationToken);
        return ServiceResult<IReadOnlyList<EmployeeResponseDto>>.Success(employees.Select(employee => new EmployeeResponseDto(
            employee.Id, employee.Username, employee.DisplayName, employee.Department ?? string.Empty,
            employee.Stack ?? string.Empty, employee.Role, employee.IsActive,
            employee.CreatedByEmployeeAccountId, employee.RoleDefinitionId, employee.RoleDefinition?.Name,
            employee.RoleDefinition?.Permissions.Select(item => item.PermissionCode).Order().ToArray() ?? Array.Empty<string>())).ToArray());
    }

    public async Task<ServiceResult<bool>> UpdateManagedEmployeeStatusAsync(
        ClaimsPrincipal caller,
        int employeeId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasAsync(caller, EmployeePermissions.ManageEmployees, cancellationToken))
        {
            return ServiceResult<bool>.Forbidden("You do not have permission to change managed employee status.");
        }
        if (!TryGetAccountId(caller, out var managerId)) return ServiceResult<bool>.Forbidden("The account identity is unavailable.");
        var employee = await repository.GetManagedEmployeeAsync(employeeId, managerId, caller.IsInRole(EmployeeRoles.Admin), cancellationToken);
        if (employee is null) return ServiceResult<bool>.NotFound();
        employee.IsActive = isActive;
        employee.InactiveAtUtc = isActive ? null : DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteManagedEmployeeAsync(
        ClaimsPrincipal caller,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasAsync(caller, EmployeePermissions.DeleteEmployees, cancellationToken))
        {
            return ServiceResult<bool>.Forbidden("You do not have permission to delete managed employees.");
        }
        if (!TryGetAccountId(caller, out var managerId))
        {
            return ServiceResult<bool>.Forbidden("The account identity is unavailable.");
        }

        var employee = await repository.GetManagedEmployeeAsync(
            employeeId, managerId, caller.IsInRole(EmployeeRoles.Admin), cancellationToken);
        if (employee is null) return ServiceResult<bool>.NotFound();
        if (employee.IsActive)
        {
            return ServiceResult<bool>.Conflict("Deactivate the employee before permanently deleting the account.");
        }
        if (await repository.HasEmployeeReferencesAsync(employeeId, cancellationToken))
        {
            return ServiceResult<bool>.Conflict("This employee has managed accounts, candidates, roles, or reviewed work. Reassign or remove those records first.");
        }

        repository.RemoveEmployee(employee);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<EmployeeAccessSummaryDto>> GetManagedEmployeeSummaryAsync(
        ClaimsPrincipal caller,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasAsync(caller, EmployeePermissions.ViewTeam, cancellationToken))
        {
            return ServiceResult<EmployeeAccessSummaryDto>.Forbidden("You do not have permission to view managed employee summaries.");
        }
        if (!TryGetAccountId(caller, out var accountId))
        {
            return ServiceResult<EmployeeAccessSummaryDto>.Forbidden("The account identity is unavailable.");
        }
        var summary = await repository.GetManagedEmployeeSummaryAsync(accountId, cancellationToken);
        return ServiceResult<EmployeeAccessSummaryDto>.Success(new EmployeeAccessSummaryDto(
            summary.ActiveEmployees, summary.InactiveEmployees));
    }

    public static EmployeeRoleResponseDto ToResponse(EmployeeRoleDefinition role) => new(
        role.Id, role.Name, role.IsBuiltIn,
        role.Permissions.Select(item => item.PermissionCode).Order().ToArray());

    private static bool TryGetAccountId(ClaimsPrincipal caller, out int accountId)
    {
        var subject = caller.FindFirstValue("sub") ?? caller.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out accountId);
    }
}
