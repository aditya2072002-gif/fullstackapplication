using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Repositories;

public interface IPortalChatRepository
{
    Task<int> CountEmployeesAsync(string? search, bool? isActive, int? managerEmployeeId, CancellationToken cancellationToken);

    Task<int> CountCandidatesAsync(
        string? search, bool? isActive, int? candidateId, int? ownerEmployeeId,
        CancellationToken cancellationToken);

    Task<int> CountWorkEntriesAsync(
        string? status, int? candidateId, int? ownerEmployeeId, int? candidateAccountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalChatEmployeeDto>> SearchEmployeesAsync(
        string? search, bool? isActive, int? managerEmployeeId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalChatCandidateDto>> SearchCandidatesAsync(
        string? search, bool? isActive, int? candidateId, int? ownerEmployeeId,
        int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalChatWorkEntryDto>> SearchWorkEntriesAsync(
        string? status, int? candidateId, int? ownerEmployeeId, int? candidateAccountId,
        int limit, CancellationToken cancellationToken);
}
