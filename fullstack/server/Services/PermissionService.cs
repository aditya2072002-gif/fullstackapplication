using System.Globalization;
using System.Security.Claims;
using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Services;

public interface IPermissionService
{
    Task<bool> HasAsync(ClaimsPrincipal caller, string permission, CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> GetPermissionsAsync(ClaimsPrincipal caller, CancellationToken cancellationToken);
}

public sealed class PermissionService(ApplicationDbContext dbContext) : IPermissionService
{
    public async Task<bool> HasAsync(ClaimsPrincipal caller, string permission, CancellationToken cancellationToken)
    {
        if (caller.IsInRole(EmployeeRoles.Admin)) return true;
        if (!TryGetEmployeeId(caller, out var employeeId)) return false;

        return await dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Id == employeeId && account.IsActive && account.Role == EmployeeRoles.Employee)
            .AnyAsync(account => account.RoleDefinitionId != null
                && account.RoleDefinition!.Permissions.Any(item => item.PermissionCode == permission), cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        ClaimsPrincipal caller,
        CancellationToken cancellationToken)
    {
        if (caller.IsInRole(EmployeeRoles.Admin)) return EmployeePermissions.Catalog.Keys.ToHashSet(StringComparer.Ordinal);
        if (!TryGetEmployeeId(caller, out var employeeId)) return new HashSet<string>(StringComparer.Ordinal);

        var permissions = await dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Id == employeeId && account.IsActive
                && account.Role == EmployeeRoles.Employee && account.RoleDefinitionId != null)
            .SelectMany(account => account.RoleDefinition!.Permissions.Select(item => item.PermissionCode))
            .ToListAsync(cancellationToken);
        return permissions.ToHashSet(StringComparer.Ordinal);
    }

    private static bool TryGetEmployeeId(ClaimsPrincipal caller, out int employeeId)
    {
        var subject = caller.FindFirstValue("sub") ?? caller.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out employeeId);
    }
}
