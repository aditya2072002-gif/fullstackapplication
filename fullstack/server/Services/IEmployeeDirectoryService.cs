using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IEmployeeDirectoryService
{
    Task<IReadOnlyList<AccountSummaryResponseDto>> GetAdminsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountSummaryResponseDto>> GetCandidatesAsync(CancellationToken cancellationToken);
}
