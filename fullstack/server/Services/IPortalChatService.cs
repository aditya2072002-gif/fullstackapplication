using System.Security.Claims;
using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IPortalChatService
{
    Task<ServiceResult<int>> CountEmployeesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, CancellationToken cancellationToken);

    Task<ServiceResult<int>> CountCandidatesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int? candidateId,
        CancellationToken cancellationToken);

    Task<ServiceResult<int>> CountWorkEntriesAsync(
        ClaimsPrincipal caller, string? status, int? candidateId, CancellationToken cancellationToken);

    Task<ServiceResult<PortalChatAnswerDto>> AskAsync(
        ClaimsPrincipal caller, string message, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<PortalChatEmployeeDto>>> SearchEmployeesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int limit, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<PortalChatCandidateDto>>> SearchCandidatesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int? candidateId,
        int limit, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>> SearchWorkEntriesAsync(
        ClaimsPrincipal caller, string? status, int? candidateId, int limit,
        CancellationToken cancellationToken);
}
