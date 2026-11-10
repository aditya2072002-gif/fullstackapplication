using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class EmployeeManagementRepository(ApplicationDbContext dbContext) : IEmployeeManagementRepository
{
    public async Task<IReadOnlyList<EmployeeAccount>> GetEmployeesAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccounts.AsNoTracking()
            .Include(account => account.RoleDefinition).ThenInclude(role => role!.Permissions)
            .Where(account => account.Role == EmployeeRoles.Employee)
            .OrderBy(account => account.DisplayName)
            .ToListAsync(cancellationToken);

    public Task<EmployeeRoleDefinition?> GetBuiltInMentorRoleAsync(CancellationToken cancellationToken) =>
        dbContext.EmployeeRoleDefinitions.Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.IsBuiltIn && role.Name == "Mentor", cancellationToken);

    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.AnyAsync(account => account.NormalizedUsername == normalizedUsername, cancellationToken);

    public Task AddAsync(EmployeeAccount employee, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.AddAsync(employee, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
