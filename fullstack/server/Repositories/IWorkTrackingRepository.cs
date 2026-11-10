using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface IWorkTrackingRepository
{
    Task<IReadOnlyList<CandidateWorkEntry>> GetCandidateEntriesAsync(int accountId, CancellationToken cancellationToken);

    Task<CandidateWorkEntry?> GetCandidateEntryAsync(int accountId, int entryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CandidateWorkEntry>> SearchEntriesAsync(
        int? candidateId,
        string? name,
        string? stack,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? status,
        int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task<CandidateWorkEntry?> GetEntryAsync(int entryId, CancellationToken cancellationToken);

    Task<DateTime?> GetCandidateCreatedAtUtcAsync(
        int candidateId, int? ownerEmployeeId, CancellationToken cancellationToken);

    Task<bool> TrySaveReviewsAsync(
        int candidateId,
        IReadOnlyCollection<int> entryIds,
        int reviewerAccountId,
        string status,
        string? comment,
        DateTime reviewedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task<bool> TrySaveInterviewDecisionAsync(
        int candidateId,
        string decision,
        string? comment,
        int reviewerAccountId,
        DateTime decidedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken);

    void Remove(CandidateWorkEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
