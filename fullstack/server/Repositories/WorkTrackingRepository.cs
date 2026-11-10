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
        string? status, int? ownerEmployeeId, CancellationToken cancellationToken)
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
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(entry => entry.Status == status);
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

    public Task<DateTime?> GetCandidateCreatedAtUtcAsync(
        int candidateId, int? ownerEmployeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .Where(account => account.Id == candidateId
                && account.Role == EmployeeRoles.Candidate
                && account.CandidateProfile != null
                && (!ownerEmployeeId.HasValue || account.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value))
            .Select(account => (DateTime?)account.CreatedAtUtc)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> TrySaveReviewsAsync(
        int candidateId,
        IReadOnlyCollection<int> entryIds,
        int reviewerAccountId,
        string status,
        string? comment,
        DateTime reviewedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CandidateWorkEntries.Where(entry => entry.CandidateProfileId == candidateId
            && entryIds.Contains(entry.Id)
            && entry.Status == CandidateWorkStatuses.Pending);
        if (ownerEmployeeId.HasValue)
        {
            query = query.Where(entry => entry.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value);
        }

        if (!dbContext.Database.IsRelational())
        {
            var pendingEntries = await query.ToListAsync(cancellationToken);
            if (pendingEntries.Count != entryIds.Count) return false;
            foreach (var entry in pendingEntries)
            {
                entry.Status = status;
                entry.AdminFeedback = comment;
                entry.ReviewedAtUtc = reviewedAtUtc;
                entry.ReviewedByEmployeeAccountId = reviewerAccountId;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updatedCount = await query.ExecuteUpdateAsync(updates => updates
            .SetProperty(entry => entry.Status, status)
            .SetProperty(entry => entry.AdminFeedback, comment)
            .SetProperty(entry => entry.ReviewedAtUtc, reviewedAtUtc)
            .SetProperty(entry => entry.ReviewedByEmployeeAccountId, reviewerAccountId), cancellationToken);
        if (updatedCount != entryIds.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TrySaveInterviewDecisionAsync(
        int candidateId,
        string decision,
        string? comment,
        int reviewerAccountId,
        DateTime decidedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CandidateProfiles.Where(profile => profile.EmployeeAccountId == candidateId
            && profile.InterviewDecision == CandidateInterviewDecisions.PendingReview
            && (!ownerEmployeeId.HasValue || profile.CreatorEmployeeAccountId == ownerEmployeeId.Value));
        if (!dbContext.Database.IsRelational())
        {
            var profile = await query.SingleOrDefaultAsync(cancellationToken);
            if (profile is null) return false;
            profile.InterviewDecision = decision;
            profile.InterviewDecisionComment = comment;
            profile.InterviewDecisionAtUtc = decidedAtUtc;
            profile.InterviewDecisionByEmployeeAccountId = reviewerAccountId;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return await query.ExecuteUpdateAsync(updates => updates
            .SetProperty(profile => profile.InterviewDecision, decision)
            .SetProperty(profile => profile.InterviewDecisionComment, comment)
            .SetProperty(profile => profile.InterviewDecisionAtUtc, decidedAtUtc)
            .SetProperty(profile => profile.InterviewDecisionByEmployeeAccountId, reviewerAccountId), cancellationToken) == 1;
    }

    public Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken) =>
        dbContext.CandidateWorkEntries.AddAsync(entry, cancellationToken).AsTask();

    public void Remove(CandidateWorkEntry entry) => dbContext.CandidateWorkEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
