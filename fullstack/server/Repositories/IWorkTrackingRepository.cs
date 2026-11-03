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
        CancellationToken cancellationToken);

    Task<CandidateWorkEntry?> GetEntryAsync(int entryId, CancellationToken cancellationToken);

    Task AddAsync(CandidateWorkEntry entry, CancellationToken cancellationToken);

    void Remove(CandidateWorkEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
