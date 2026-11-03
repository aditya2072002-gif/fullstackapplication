using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface ICandidateSelfRepository
{
    Task<EmployeeAccount?> GetActiveCandidateWithProfileAsync(int accountId, CancellationToken cancellationToken);

    Task<EmployeeAccount?> GetActiveCandidateAccountAsync(int accountId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
