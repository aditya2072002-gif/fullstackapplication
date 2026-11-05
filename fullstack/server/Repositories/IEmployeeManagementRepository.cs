using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface IEmployeeManagementRepository
{
    Task<IReadOnlyList<EmployeeAccount>> GetEmployeesAsync(CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken);

    Task AddAsync(EmployeeAccount employee, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
