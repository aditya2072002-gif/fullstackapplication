using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class AuthRepository(ApplicationDbContext dbContext) : IAuthRepository
{
    public Task<EmployeeAccount?> GetActiveAccountByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.SingleOrDefaultAsync(
            account => account.NormalizedUsername == normalizedUsername && account.IsActive,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
