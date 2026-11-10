using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IWorkTrackingService
{
    Task<IReadOnlyList<CandidateWorkEntryResponse>> GetCandidateEntriesAsync(int accountId, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateWorkEntryResponse>> CreateAsync(int accountId, CandidateWorkEntryRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateWorkEntryResponse>> UpdateCandidateEntryAsync(
        int accountId, int entryId, CandidateWorkEntryRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteCandidateEntryAsync(int accountId, int entryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CandidateWorkEntryResponse>> SearchAsync(
        int? candidateId, string? name, string? stack, DateOnly? dateFrom, DateOnly? dateTo,
        string? status, int reviewerAccountId, bool isAdmin, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<CandidateWorkEntryResponse>>> ReviewAsync(
        int reviewerAccountId,
        bool isAdmin,
        ReviewCandidateWorkEntriesRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> UpdateInterviewDecisionAsync(
        int reviewerAccountId,
        bool isAdmin,
        int candidateId,
        UpdateCandidateInterviewDecisionRequest request,
        CancellationToken cancellationToken);
}
