using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class EmployeeDirectoryRepository(ApplicationDbContext dbContext) : IEmployeeDirectoryRepository
{
    public async Task<IReadOnlyList<EmployeeAccount>> GetActiveAccountsByRoleAsync(
        string role,
        CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccounts
            .AsNoTracking()
            .Where(account => account.Role == role && account.IsActive)
            .OrderBy(account => account.DisplayName)
            .ToListAsync(cancellationToken);
}
