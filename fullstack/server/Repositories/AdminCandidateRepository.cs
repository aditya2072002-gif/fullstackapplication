using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class AdminCandidateRepository(ApplicationDbContext dbContext) : IAdminCandidateRepository
{
    public async Task<IReadOnlyList<EmployeeAccount>> GetCandidatesAsync(CancellationToken cancellationToken) =>
        await GetCandidatesQuery().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeAccount>> GetCandidatesCreatedByAsync(
        int employeeAccountId,
        CancellationToken cancellationToken) =>
        await GetCandidatesQuery()
            .Where(account => account.CandidateProfile!.CreatorEmployeeAccountId == employeeAccountId)
            .ToListAsync(cancellationToken);

    private IQueryable<EmployeeAccount> GetCandidatesQuery() => dbContext.EmployeeAccounts
        .AsNoTracking()
        .Where(account => account.Role == EmployeeRoles.Candidate && account.CandidateProfile != null)
        .OrderBy(account => account.CandidateProfile!.LastName)
        .ThenBy(account => account.CandidateProfile!.FirstName)
        .Select(account => new EmployeeAccount
        {
            Id = account.Id,
            Username = account.Username,
            IsActive = account.IsActive,
            InactiveAtUtc = account.InactiveAtUtc,
            CandidateProfile = new CandidateProfile
            {
                EmployeeAccountId = account.CandidateProfile!.EmployeeAccountId,
                FirstName = account.CandidateProfile.FirstName,
                LastName = account.CandidateProfile.LastName,
                Email = account.CandidateProfile.Email,
                PhoneNumber = account.CandidateProfile.PhoneNumber,
                YearsOfExperience = account.CandidateProfile.YearsOfExperience,
                Stack = account.CandidateProfile.Stack,
                PerformanceScore = account.CandidateProfile.PerformanceScore,
                ResumeFileName = account.CandidateProfile.ResumeFileName,
                ResumeContentType = account.CandidateProfile.ResumeContentType,
                CreatorEmployeeAccountId = account.CandidateProfile.CreatorEmployeeAccountId,
                CreatorEmployeeAccount = account.CandidateProfile.CreatorEmployeeAccount == null
                    ? null
                    : new EmployeeAccount { DisplayName = account.CandidateProfile.CreatorEmployeeAccount.DisplayName }
            }
        });

    public Task<EmployeeAccount?> GetCandidateAsync(int id, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Include(account => account.CandidateProfile)
            .ThenInclude(profile => profile!.CreatorEmployeeAccount)
            .SingleOrDefaultAsync(
                account => account.Id == id
                    && account.Role == EmployeeRoles.Candidate
                    && account.CandidateProfile != null,
                cancellationToken);

    public Task<EmployeeAccount?> GetCandidateCreatedByAsync(
        int id,
        int employeeAccountId,
        CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Include(account => account.CandidateProfile)
                .ThenInclude(profile => profile!.CreatorEmployeeAccount)
            .SingleOrDefaultAsync(account => account.Id == id
                && account.Role == EmployeeRoles.Candidate
                && account.CandidateProfile != null
                && account.CandidateProfile.CreatorEmployeeAccountId == employeeAccountId,
                cancellationToken);

    public Task<CandidateProfile?> GetCandidateResumeAsync(int id, CancellationToken cancellationToken) =>
        dbContext.CandidateProfiles
            .AsNoTracking()
            .Where(profile => profile.EmployeeAccountId == id
                && profile.EmployeeAccount.Role == EmployeeRoles.Candidate)
            .Select(profile => new CandidateProfile
            {
                EmployeeAccountId = profile.EmployeeAccountId,
                ResumeContent = profile.ResumeContent,
                ResumeFileName = profile.ResumeFileName,
                ResumeContentType = profile.ResumeContentType
            })
            .SingleOrDefaultAsync(cancellationToken);

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
