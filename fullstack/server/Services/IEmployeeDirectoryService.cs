using fullstackapplication.Server.Contracts;

namespace fullstackapplication.Server.Services;

public interface IEmployeeDirectoryService
{
    Task<IReadOnlyList<AccountSummaryResponse>> GetAdminsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountSummaryResponse>> GetCandidatesAsync(CancellationToken cancellationToken);
}
