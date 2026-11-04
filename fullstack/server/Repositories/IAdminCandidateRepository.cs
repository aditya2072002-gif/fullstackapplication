using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface IAdminCandidateRepository
{
    Task<IReadOnlyList<EmployeeAccount>> GetCandidatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeAccount>> GetCandidatesCreatedByAsync(int employeeAccountId, CancellationToken cancellationToken);

    Task<EmployeeAccount?> GetCandidateAsync(int id, CancellationToken cancellationToken);

    Task<EmployeeAccount?> GetCandidateCreatedByAsync(
        int id,
        int employeeAccountId,
        CancellationToken cancellationToken);

    Task<CandidateProfile?> GetCandidateResumeAsync(int id, CancellationToken cancellationToken);

    Task<EmployeeAccount?> GetCandidateAccountAsync(int id, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken);

    Task AddCandidateAsync(EmployeeAccount account, CancellationToken cancellationToken);

    void RemoveCandidate(EmployeeAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
