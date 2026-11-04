using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class EmployeeDirectoryService(IEmployeeDirectoryRepository repository) : IEmployeeDirectoryService
{
    public Task<IReadOnlyList<AccountSummaryResponseDto>> GetAdminsAsync(CancellationToken cancellationToken) =>
        GetAccountsAsync(EmployeeRoles.Admin, cancellationToken);

    public Task<IReadOnlyList<AccountSummaryResponseDto>> GetCandidatesAsync(CancellationToken cancellationToken) =>
        GetAccountsAsync(EmployeeRoles.Candidate, cancellationToken);

    private async Task<IReadOnlyList<AccountSummaryResponseDto>> GetAccountsAsync(
        string role,
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetActiveAccountsByRoleAsync(role, cancellationToken);
        return accounts.Select(account => new AccountSummaryResponseDto(
            account.Id,
            account.Username,
            account.DisplayName,
            account.Role)).ToArray();
    }
}
