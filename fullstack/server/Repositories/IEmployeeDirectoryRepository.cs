using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface IEmployeeDirectoryRepository
{
    Task<IReadOnlyList<EmployeeAccount>> GetActiveAccountsByRoleAsync(string role, CancellationToken cancellationToken);
}
