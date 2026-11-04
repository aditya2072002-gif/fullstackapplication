using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface IAuthRepository
{
    Task<EmployeeAccount?> GetActiveAccountByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
