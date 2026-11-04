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
        int? grade,
        int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task<CandidateWorkEntry?> GetEntryAsync(int entryId, CancellationToken cancellationToken);

    Task<bool> TrySaveReviewAsync(
        int entryId,
        int reviewerAccountId,
        int grade,
        string? feedback,
        DateTime reviewedAtUtc,
        int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken);

    void Remove(CandidateWorkEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
