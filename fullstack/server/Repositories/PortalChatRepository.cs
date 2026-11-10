using fullstackapplication.Server.Data;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class PortalChatRepository(ApplicationDbContext dbContext) : IPortalChatRepository
{
    public Task<int> CountEmployeesAsync(string? search, bool? isActive, CancellationToken cancellationToken) =>
        QueryEmployees(search, isActive).CountAsync(cancellationToken);

    public Task<int> CountCandidatesAsync(
        string? search, bool? isActive, int? candidateId, int? ownerEmployeeId,
        CancellationToken cancellationToken) =>
        QueryCandidates(search, isActive, candidateId, ownerEmployeeId).CountAsync(cancellationToken);

    public Task<int> CountWorkEntriesAsync(
        string? status, int? candidateId, int? ownerEmployeeId, int? candidateAccountId,
        CancellationToken cancellationToken) =>
        QueryWorkEntries(status, candidateId, ownerEmployeeId, candidateAccountId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<PortalChatEmployeeDto>> SearchEmployeesAsync(
        string? search, bool? isActive, int limit, CancellationToken cancellationToken)
    {
        var query = QueryEmployees(search, isActive);

        return await query.OrderBy(account => account.DisplayName)
            .Select(account => new PortalChatEmployeeDto(
                account.Id,
                account.Username,
                account.DisplayName,
                account.Department,
                account.Stack,
                account.IsActive,
                account.CreatedAtUtc))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalChatCandidateDto>> SearchCandidatesAsync(
        string? search, bool? isActive, int? candidateId, int? ownerEmployeeId,
        int limit, CancellationToken cancellationToken)
    {
        var query = QueryCandidates(search, isActive, candidateId, ownerEmployeeId);

        return await query.OrderBy(account => account.CandidateProfile!.LastName)
            .ThenBy(account => account.CandidateProfile!.FirstName)
            .Select(account => new PortalChatCandidateDto(
                account.Id,
                account.Username,
                account.CandidateProfile!.FirstName,
                account.CandidateProfile.LastName,
                account.CandidateProfile.Email,
                account.CandidateProfile.PhoneNumber,
                account.CandidateProfile.Stack,
                account.IsActive,
                account.CandidateProfile.InterviewDecision,
                account.CandidateProfile.WorkEntries.Count(entry => entry.Status == CandidateWorkStatuses.Pending),
                account.CandidateProfile.WorkEntries.Count(entry => entry.Status == CandidateWorkStatuses.Approved),
                account.CandidateProfile.WorkEntries.Count(entry => entry.Status == CandidateWorkStatuses.Rejected)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalChatWorkEntryDto>> SearchWorkEntriesAsync(
        string? status, int? candidateId, int? ownerEmployeeId, int? candidateAccountId,
        int limit, CancellationToken cancellationToken)
    {
        var query = QueryWorkEntries(status, candidateId, ownerEmployeeId, candidateAccountId);

        return await query.OrderByDescending(entry => entry.WorkDate)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new PortalChatWorkEntryDto(
                entry.Id,
                entry.CandidateProfileId,
                entry.CandidateProfile.FirstName + " " + entry.CandidateProfile.LastName,
                entry.WorkDate,
                entry.Task,
                entry.HoursSpent,
                entry.Status,
                entry.AdminFeedback,
                entry.SubmittedAtUtc,
                entry.ReviewedAtUtc))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<EmployeeAccount> QueryEmployees(string? search, bool? isActive)
    {
        var query = dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Role == EmployeeRoles.Employee);
        if (isActive.HasValue) query = query.Where(account => account.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(account => account.Username.Contains(term)
                || account.DisplayName.Contains(term)
                || (account.Department != null && account.Department.Contains(term))
                || (account.Stack != null && account.Stack.Contains(term)));
        }

        return query;
    }

    private IQueryable<EmployeeAccount> QueryCandidates(
        string? search, bool? isActive, int? candidateId, int? ownerEmployeeId)
    {
        var query = dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Role == EmployeeRoles.Candidate && account.CandidateProfile != null);
        if (isActive.HasValue) query = query.Where(account => account.IsActive == isActive.Value);
        if (candidateId.HasValue) query = query.Where(account => account.Id == candidateId.Value);
        if (ownerEmployeeId.HasValue)
        {
            query = query.Where(account => account.CandidateProfile!.CreatorEmployeeAccountId == ownerEmployeeId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(account => account.Username.Contains(term)
                || account.CandidateProfile!.FirstName.Contains(term)
                || (account.CandidateProfile.FirstName + " " + account.CandidateProfile.LastName).Contains(term)
                || account.CandidateProfile.LastName.Contains(term)
                || account.CandidateProfile.Email.Contains(term)
                || account.CandidateProfile.Stack.Contains(term));
        }

        return query;
    }

    private IQueryable<CandidateWorkEntry> QueryWorkEntries(
        string? status, int? candidateId, int? ownerEmployeeId, int? candidateAccountId)
    {
        var query = dbContext.CandidateWorkEntries.AsNoTracking()
            .Where(entry => entry.CandidateProfile.EmployeeAccount.Role == EmployeeRoles.Candidate);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(entry => entry.Status == status);
        if (candidateId.HasValue) query = query.Where(entry => entry.CandidateProfileId == candidateId.Value);
        if (ownerEmployeeId.HasValue)
        {
            query = query.Where(entry => entry.CandidateProfile.CreatorEmployeeAccountId == ownerEmployeeId.Value);
        }
        if (candidateAccountId.HasValue)
        {
            query = query.Where(entry => entry.CandidateProfileId == candidateAccountId.Value);
        }

        return query;
    }
}
