using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class AdminCandidateRepository(ApplicationDbContext dbContext) : IAdminCandidateRepository
{
    public async Task<IReadOnlyList<EmployeeAccount>> GetCandidatesAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccounts
            .AsNoTracking()
            .Include(account => account.CandidateProfile)
            .Where(account => account.Role == EmployeeRoles.Candidate && account.CandidateProfile != null)
            .OrderBy(account => account.CandidateProfile!.LastName)
            .ThenBy(account => account.CandidateProfile!.FirstName)
            .ToListAsync(cancellationToken);

    public Task<EmployeeAccount?> GetCandidateAsync(int id, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Include(account => account.CandidateProfile)
            .SingleOrDefaultAsync(
                account => account.Id == id
                    && account.Role == EmployeeRoles.Candidate
                    && account.CandidateProfile != null,
                cancellationToken);

    public Task<EmployeeAccount?> GetCandidateAccountAsync(int id, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Include(account => account.CandidateProfile)
            .SingleOrDefaultAsync(
                account => account.Id == id && account.Role == EmployeeRoles.Candidate,
                cancellationToken);

    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.AnyAsync(
            account => account.NormalizedUsername == normalizedUsername,
            cancellationToken);

    public Task AddCandidateAsync(EmployeeAccount account, CancellationToken cancellationToken)
    {
        dbContext.EmployeeAccounts.Add(account);
        return Task.CompletedTask;
    }

    public void RemoveCandidate(EmployeeAccount account) => dbContext.EmployeeAccounts.Remove(account);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
