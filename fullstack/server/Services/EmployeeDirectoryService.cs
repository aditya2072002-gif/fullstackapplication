using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class EmployeeDirectoryService(IEmployeeDirectoryRepository repository) : IEmployeeDirectoryService
{
    public Task<IReadOnlyList<AccountSummaryResponse>> GetAdminsAsync(CancellationToken cancellationToken) =>
        GetAccountsAsync(EmployeeRoles.Admin, cancellationToken);

    public Task<IReadOnlyList<AccountSummaryResponse>> GetCandidatesAsync(CancellationToken cancellationToken) =>
        GetAccountsAsync(EmployeeRoles.Candidate, cancellationToken);

    private async Task<IReadOnlyList<AccountSummaryResponse>> GetAccountsAsync(
        string role,
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetActiveAccountsByRoleAsync(role, cancellationToken);
        return accounts.Select(account => new AccountSummaryResponse(
            account.Id,
            account.Username,
            account.DisplayName,
            account.Role)).ToArray();
    }
}
