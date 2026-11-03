using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class CandidateSelfRepository(ApplicationDbContext dbContext) : ICandidateSelfRepository
{
    public Task<EmployeeAccount?> GetActiveCandidateWithProfileAsync(int accountId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Include(account => account.CandidateProfile)
            .SingleOrDefaultAsync(
                account => account.Id == accountId
                    && account.Role == EmployeeRoles.Candidate
                    && account.IsActive,
                cancellationToken);

    public Task<EmployeeAccount?> GetActiveCandidateAccountAsync(int accountId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.SingleOrDefaultAsync(
            account => account.Id == accountId
                && account.Role == EmployeeRoles.Candidate
                && account.IsActive,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
