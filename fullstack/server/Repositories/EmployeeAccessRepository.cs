using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class EmployeeAccessRepository(ApplicationDbContext dbContext)
{
    public Task<EmployeeRoleDefinition?> GetRoleAsync(int roleId, CancellationToken cancellationToken) =>
        dbContext.EmployeeRoleDefinitions.Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<bool> RoleNameExistsAsync(string normalizedName, CancellationToken cancellationToken) =>
        dbContext.EmployeeRoleDefinitions.AnyAsync(role => role.Name.ToUpper() == normalizedName, cancellationToken);

    public Task AddRoleAsync(EmployeeRoleDefinition role, CancellationToken cancellationToken) =>
        dbContext.EmployeeRoleDefinitions.AddAsync(role, cancellationToken).AsTask();

    public async Task<IReadOnlyList<EmployeeRoleDefinition>> GetRolesAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeRoleDefinitions.AsNoTracking().Include(role => role.Permissions)
            .OrderBy(role => role.Name).ToListAsync(cancellationToken);

    public Task<EmployeeAccount?> GetManagedEmployeeAsync(
        int employeeId, int managerId, bool isAdmin, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.SingleOrDefaultAsync(account => account.Id == employeeId
            && account.Role == EmployeeRoles.Employee
            && (isAdmin || account.CreatedByEmployeeAccountId == managerId), cancellationToken);

    public async Task<IReadOnlyList<EmployeeAccount>> GetManagedEmployeesAsync(
        int managerId, CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccounts.AsNoTracking()
            .Include(account => account.RoleDefinition).ThenInclude(role => role!.Permissions)
            .Where(account => account.Role == EmployeeRoles.Employee && account.CreatedByEmployeeAccountId == managerId)
            .OrderBy(account => account.DisplayName).ToListAsync(cancellationToken);

    public async Task<EmployeeAccessSummary> GetManagedEmployeeSummaryAsync(
        int managerId, CancellationToken cancellationToken)
    {
        var counts = await dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Role == EmployeeRoles.Employee && account.CreatedByEmployeeAccountId == managerId)
            .GroupBy(account => account.IsActive)
            .Select(group => new { IsActive = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return new EmployeeAccessSummary(
            counts.SingleOrDefault(item => item.IsActive)?.Count ?? 0,
            counts.SingleOrDefault(item => !item.IsActive)?.Count ?? 0);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}

public sealed record EmployeeAccessSummary(int ActiveEmployees, int InactiveEmployees);
