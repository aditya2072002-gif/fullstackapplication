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
                .ThenInclude(profile => profile.CreatorEmployeeAccount)
            .Include(entry => entry.ReviewedByEmployeeAccount)
            .Where(entry => entry.CandidateProfileId == accountId)
            .OrderByDescending(entry => entry.WorkDate).ThenByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);

    public Task<CandidateWorkEntry?> GetCandidateEntryAsync(
        int accountId, int entryId, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries
            .Include(entry => entry.CandidateProfile)
                .ThenInclude(profile => profile.CreatorEmployeeAccount)
            .Include(entry => entry.ReviewedByEmployeeAccount)
            .SingleOrDefaultAsync(
                entry => entry.Id == entryId && entry.CandidateProfileId == accountId,
                cancellationToken);

    public async Task<IReadOnlyList<CandidateWorkEntry>> SearchEntriesAsync(
        int? candidateId, string? name, string? stack, DateOnly? dateFrom, DateOnly? dateTo,
        int? grade, int? ownerEmployeeId, CancellationToken cancellationToken)
    {
        var query = dbContext.CandidateWorkEntries.AsNoTracking()
            .Include(entry => entry.CandidateProfile)
                .ThenInclude(profile => profile.EmployeeAccount)
            .Include(entry => entry.CandidateProfile)
                .ThenInclude(profile => profile.CreatorEmployeeAccount)
            .Include(entry => entry.ReviewedByEmployeeAccount)
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
        if (ownerEmployeeId.HasValue)
        {
            query = query.Where(entry => entry.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value);
        }
        return await query.OrderByDescending(entry => entry.WorkDate)
            .ThenBy(entry => entry.CandidateProfile.LastName)
            .ThenBy(entry => entry.CandidateProfile.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<CandidateWorkEntry?> GetEntryAsync(int entryId, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries
            .AsNoTracking()
            .Include(entry => entry.CandidateProfile)
                .ThenInclude(profile => profile.CreatorEmployeeAccount)
            .Include(entry => entry.ReviewedByEmployeeAccount)
            .SingleOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public async Task<bool> TrySaveReviewAsync(
        int entryId,
        int reviewerAccountId,
        int grade,
        string? feedback,
        DateTime reviewedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            var inMemoryEntry = await dbContext.CandidateWorkEntries
                .Include(entry => entry.CandidateProfile)
                .SingleOrDefaultAsync(entry => entry.Id == entryId && entry.Grade == null
                    && (!ownerEmployeeId.HasValue || entry.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value),
                    cancellationToken);
            if (inMemoryEntry is null) return false;
            inMemoryEntry.Grade = grade;
            inMemoryEntry.AdminFeedback = feedback;
            inMemoryEntry.ReviewedAtUtc = reviewedAtUtc;
            inMemoryEntry.ReviewedByEmployeeAccountId = reviewerAccountId;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var query = dbContext.CandidateWorkEntries.Where(entry => entry.Id == entryId && entry.Grade == null);
        if (ownerEmployeeId.HasValue)
        {
            query = query.Where(entry => entry.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value);
        }

        return await query.ExecuteUpdateAsync(updates => updates
            .SetProperty(entry => entry.Grade, grade)
            .SetProperty(entry => entry.AdminFeedback, feedback)
            .SetProperty(entry => entry.ReviewedAtUtc, reviewedAtUtc)
            .SetProperty(entry => entry.ReviewedByEmployeeAccountId, reviewerAccountId), cancellationToken) == 1;
    }

    public Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries.AddAsync(entry, cancellationToken).AsTask();

    public void Remove(CandidateWorkEntry entry) => dbContext.CandidateWorkEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
