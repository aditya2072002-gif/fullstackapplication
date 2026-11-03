using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class WorkTrackingRepository(ApplicationDbContext dbContext) : IWorkTrackingRepository
{
    public async Task<IReadOnlyList<CandidateWorkEntry>> GetCandidateEntriesAsync(
        int accountId, CancellationToken cancellationToken) =>
        await dbContext.CandidateWorkEntries.AsNoTracking()
            .Include(entry => entry.CandidateProfile)
            .Where(entry => entry.CandidateProfileId == accountId)
            .OrderByDescending(entry => entry.WorkDate).ThenByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);

    public Task<CandidateWorkEntry?> GetCandidateEntryAsync(
        int accountId, int entryId, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries
            .Include(entry => entry.CandidateProfile)
            .SingleOrDefaultAsync(
                entry => entry.Id == entryId && entry.CandidateProfileId == accountId,
                cancellationToken);

    public async Task<IReadOnlyList<CandidateWorkEntry>> SearchEntriesAsync(
        int? candidateId, string? name, string? stack, DateOnly? dateFrom, DateOnly? dateTo,
        int? grade, CancellationToken cancellationToken)
    {
        var query = dbContext.CandidateWorkEntries.AsNoTracking()
            .Include(entry => entry.CandidateProfile)
            .ThenInclude(profile => profile.EmployeeAccount)
            .AsQueryable();
        if (candidateId.HasValue) query = query.Where(entry => entry.CandidateProfileId == candidateId.Value);
        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            query = query.Where(entry => (entry.CandidateProfile.FirstName + " " + entry.CandidateProfile.LastName).Contains(term)
                || entry.CandidateProfile.EmployeeAccount.Username.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(stack)) query = query.Where(entry => entry.CandidateProfile.Stack.Contains(stack.Trim()));
        if (dateFrom.HasValue) query = query.Where(entry => entry.WorkDate >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(entry => entry.WorkDate <= dateTo.Value);
        if (grade.HasValue) query = query.Where(entry => entry.Grade == grade.Value);
        return await query.OrderByDescending(entry => entry.WorkDate)
            .ThenBy(entry => entry.CandidateProfile.LastName)
            .ThenBy(entry => entry.CandidateProfile.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<CandidateWorkEntry?> GetEntryAsync(int entryId, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries.Include(entry => entry.CandidateProfile)
            .SingleOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries.AddAsync(entry, cancellationToken).AsTask();

    public void Remove(CandidateWorkEntry entry) => dbContext.CandidateWorkEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
