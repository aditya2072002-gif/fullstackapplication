using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class PortalChatService(IPortalChatRepository repository) : IPortalChatService
{
    private const int DefaultResultLimit = 25;
    private const int MaximumResultLimit = 50;

    public async Task<ServiceResult<int>> CountEmployeesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, CancellationToken cancellationToken)
    {
        if (!IsAdmin(caller)) return ServiceResult<int>.Forbidden("Employee details are available to Admin accounts only.");
        return ServiceResult<int>.Success(await repository.CountEmployeesAsync(search, isActive, cancellationToken));
    }

    public async Task<ServiceResult<int>> CountCandidatesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int? candidateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetScope(caller, candidateId, out var requestedCandidateId, out var ownerEmployeeId, out var error))
        {
            return ServiceResult<int>.Forbidden(error);
        }

        return ServiceResult<int>.Success(await repository.CountCandidatesAsync(
            search, isActive, requestedCandidateId, ownerEmployeeId, cancellationToken));
    }

    public async Task<ServiceResult<int>> CountWorkEntriesAsync(
        ClaimsPrincipal caller, string? status, int? candidateId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) && !IsKnownWorkStatus(status))
        {
            return ServiceResult<int>.Invalid("Status must be Pending, Approved, or Rejected.");
        }
        status = NormalizeWorkStatus(status);
        if (!TryGetScope(caller, candidateId, out var requestedCandidateId, out var ownerEmployeeId, out var error))
        {
            return ServiceResult<int>.Forbidden(error);
        }

        var candidateAccountId = IsCandidate(caller) ? GetAccountId(caller) : null;
        return ServiceResult<int>.Success(await repository.CountWorkEntriesAsync(
            status, requestedCandidateId, ownerEmployeeId, candidateAccountId, cancellationToken));
    }

    public async Task<ServiceResult<IReadOnlyList<PortalChatEmployeeDto>>> SearchEmployeesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int limit, CancellationToken cancellationToken)
    {
        if (!IsAdmin(caller)) return ServiceResult<IReadOnlyList<PortalChatEmployeeDto>>.Forbidden("Employee details are available to Admin accounts only.");
        if (!TryNormalizeLimit(limit, out var normalizedLimit))
        {
            return ServiceResult<IReadOnlyList<PortalChatEmployeeDto>>.Invalid("Result limit must be between 1 and 50.");
        }

        return ServiceResult<IReadOnlyList<PortalChatEmployeeDto>>.Success(
            await repository.SearchEmployeesAsync(search, isActive, normalizedLimit, cancellationToken));
    }

    public async Task<ServiceResult<IReadOnlyList<PortalChatCandidateDto>>> SearchCandidatesAsync(
        ClaimsPrincipal caller, string? search, bool? isActive, int? candidateId,
        int limit, CancellationToken cancellationToken)
    {
        if (!TryGetScope(caller, candidateId, out var requestedCandidateId, out var ownerEmployeeId, out var error))
        {
            return ServiceResult<IReadOnlyList<PortalChatCandidateDto>>.Forbidden(error);
        }
        if (!TryNormalizeLimit(limit, out var normalizedLimit))
        {
            return ServiceResult<IReadOnlyList<PortalChatCandidateDto>>.Invalid("Result limit must be between 1 and 50.");
        }

        return ServiceResult<IReadOnlyList<PortalChatCandidateDto>>.Success(
            await repository.SearchCandidatesAsync(
                search, isActive, requestedCandidateId, ownerEmployeeId, normalizedLimit, cancellationToken));
    }

    public async Task<ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>> SearchWorkEntriesAsync(
        ClaimsPrincipal caller, string? status, int? candidateId, int limit,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) && !IsKnownWorkStatus(status))
        {
            return ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>.Invalid("Status must be Pending, Approved, or Rejected.");
        }
        status = NormalizeWorkStatus(status);
        if (!TryGetScope(caller, candidateId, out var requestedCandidateId, out var ownerEmployeeId, out var error))
        {
            return ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>.Forbidden(error);
        }
        if (!TryNormalizeLimit(limit, out var normalizedLimit))
        {
            return ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>.Invalid("Result limit must be between 1 and 50.");
        }

        var candidateAccountId = IsCandidate(caller) ? GetAccountId(caller) : null;
        return ServiceResult<IReadOnlyList<PortalChatWorkEntryDto>>.Success(
            await repository.SearchWorkEntriesAsync(
                status, requestedCandidateId, ownerEmployeeId, candidateAccountId, normalizedLimit, cancellationToken));
    }

    public async Task<ServiceResult<PortalChatAnswerDto>> AskAsync(
        ClaimsPrincipal caller, string message, CancellationToken cancellationToken)
    {
        var question = message.Trim();
        if (question.Length == 0)
        {
            return ServiceResult<PortalChatAnswerDto>.Invalid("Enter a question about employee, candidate, or work-review details.");
        }

        var normalized = question.ToLowerInvariant();
        if (Regex.IsMatch(normalized, @"\b(creat(e|ed|ing)|insert(ed|ing)?|add(ed|ing)?|updat(e|ed|ing)|edit(ed|ing)?|modif(y|ied|ying)|delet(e|ed|ing)|remov(e|ed|ing)|chang(e|ed|ing)|approve|reject|schedul(e|ed|ing)|cancel(ed|led|ing|ling)?|activat(e|ed|ing)|deactivat(e|ed|ing)|set|setting|assign(ed|ing)?|submit(ted|ting)?|sav(e|ed|ing)|upload(ed|ing)?)\b"))
        {
            return ServiceResult<PortalChatAnswerDto>.Success(new PortalChatAnswerDto(
                "Access denied. The chat assistant only supports read-only lookups; it cannot create, change, or delete records.",
                Array.Empty<PortalChatItemDto>()));
        }

        var mentionsEmployees = ContainsAny(normalized, "employee", "employees", "staff", "mentor");
        var mentionsCandidates = ContainsAny(normalized, "candidate", "candidates");
        var mentionsWork = ContainsAny(normalized, "work", "task", "tasks", "review", "reviews", "pending", "approved", "rejected");
        if (!mentionsEmployees && !mentionsCandidates && !mentionsWork)
        {
            return ServiceResult<PortalChatAnswerDto>.Success(new PortalChatAnswerDto(
                "I couldn’t match that question to a supported lookup. Try asking about employees, candidates, or work reviews.",
                Array.Empty<PortalChatItemDto>()));
        }

        var activeOnly = ParseActiveFilter(normalized);
        var countOnly = IsCountRequest(normalized);
        var items = new List<PortalChatItemDto>();
        var answerParts = new List<string>();

        if (mentionsEmployees)
        {
            var employeeSearch = ExtractSpecificSearchTerm(normalized);
            if (!IsAdmin(caller))
            {
                answerParts.Add("Employee directory details are restricted to Admin accounts.");
            }
            else
            {
                var count = await CountEmployeesAsync(caller, employeeSearch, activeOnly, cancellationToken);
                if (count.Kind != ServiceResultKind.Success)
                {
                    return ServiceResult<PortalChatAnswerDto>.Invalid(count.Error ?? "Employee count could not be loaded.");
                }

                if (countOnly)
                {
                    answerParts.Add($"{count.Value} employee record(s) match the requested filters.");
                }
                else
                {
                    var result = await SearchEmployeesAsync(caller, employeeSearch, activeOnly, DefaultResultLimit, cancellationToken);
                    if (result.Kind != ServiceResultKind.Success || result.Value is null)
                    {
                        return ServiceResult<PortalChatAnswerDto>.Invalid(result.Error ?? "Employee details could not be loaded.");
                    }

                    var namesOnly = employeeSearch is not null && !IsListDetailsRequest(normalized);
                    items.AddRange(result.Value.Select(employee => namesOnly ? ToNameItem(employee) : ToItem(employee)));
                    answerParts.Add(count.Value == 0
                        ? "No matching employee records found."
                        : count.Value > result.Value.Count
                            ? $"Showing {result.Value.Count} of {count.Value} matching employee record(s)."
                            : $"Found {count.Value} matching employee record(s).");
                }
            }
        }

        if (mentionsCandidates)
        {
            var candidateSearch = ExtractSpecificSearchTerm(normalized);
            var candidateId = ParseCandidateId(normalized);
            var count = await CountCandidatesAsync(caller, candidateSearch, activeOnly, candidateId, cancellationToken);
            if (count.Kind == ServiceResultKind.Forbidden)
            {
                answerParts.Add(count.Error ?? "Candidate details are not available for this account.");
            }
            else if (count.Kind != ServiceResultKind.Success)
            {
                return ServiceResult<PortalChatAnswerDto>.Invalid(count.Error ?? "Candidate count could not be loaded.");
            }
            else if (countOnly)
            {
                answerParts.Add($"{count.Value} candidate record(s) match the requested filters.");
            }
            else
            {
                var result = await SearchCandidatesAsync(caller, candidateSearch, activeOnly, candidateId, DefaultResultLimit, cancellationToken);
                if (result.Kind == ServiceResultKind.Forbidden)
                {
                    answerParts.Add(result.Error ?? "Candidate details are not available for this account.");
                }
                else if (result.Kind != ServiceResultKind.Success || result.Value is null)
                {
                    return ServiceResult<PortalChatAnswerDto>.Invalid(result.Error ?? "Candidate details could not be loaded.");
                }
                else
                {
                    var namesOnly = candidateSearch is not null && !candidateId.HasValue && !IsListDetailsRequest(normalized);
                    items.AddRange(result.Value.Select(candidate => namesOnly ? ToNameItem(candidate) : ToItem(candidate)));
                    answerParts.Add(count.Value == 0
                        ? "No matching candidate records found."
                        : count.Value > result.Value.Count
                            ? $"Showing {result.Value.Count} of {count.Value} matching candidate record(s)."
                            : $"Found {count.Value} matching candidate record(s).");
                }
            }
        }

        if (mentionsWork)
        {
            var status = ParseSingleStatus(normalized);
            var candidateId = ParseCandidateId(normalized);
            var count = await CountWorkEntriesAsync(caller, status, candidateId, cancellationToken);
            if (count.Kind == ServiceResultKind.Forbidden)
            {
                answerParts.Add(count.Error ?? "Work-review details are not available for this account.");
            }
            else if (count.Kind != ServiceResultKind.Success)
            {
                return ServiceResult<PortalChatAnswerDto>.Invalid(count.Error ?? "Work-review count could not be loaded.");
            }
            else if (countOnly)
            {
                answerParts.Add($"{count.Value} work entry/entries match the requested filters.");
            }
            else
            {
                var result = await SearchWorkEntriesAsync(caller, status, candidateId, DefaultResultLimit, cancellationToken);
                if (result.Kind == ServiceResultKind.Forbidden)
                {
                    answerParts.Add(result.Error ?? "Work-review details are not available for this account.");
                }
                else if (result.Kind != ServiceResultKind.Success || result.Value is null)
                {
                    return ServiceResult<PortalChatAnswerDto>.Invalid(result.Error ?? "Work-review details could not be loaded.");
                }
                else
                {
                    items.AddRange(result.Value.Select(ToItem));
                    answerParts.Add(count.Value == 0
                        ? "No matching work entries found."
                        : count.Value > result.Value.Count
                            ? $"Showing {result.Value.Count} of {count.Value} matching work entry/entries."
                            : $"Found {count.Value} matching work entry/entries.");
                }
            }
        }

        if (answerParts.Count == 0)
        {
            return ServiceResult<PortalChatAnswerDto>.Invalid(
                "Ask about employee, candidate, or work-review details. For example: 'Show active employees' or 'Show pending work'.");
        }

        return ServiceResult<PortalChatAnswerDto>.Success(new PortalChatAnswerDto(
            string.Join(" ", answerParts), items));
    }

    private static bool TryGetScope(
        ClaimsPrincipal caller,
        int? candidateId,
        out int? requestedCandidateId,
        out int? ownerEmployeeId,
        out string error)
    {
        requestedCandidateId = candidateId;
        ownerEmployeeId = null;
        error = "This account is not permitted to access these candidate records.";

        if (IsAdmin(caller)) return true;
        if (!TryGetAccountId(caller, out var accountId)) return false;

        if (IsEmployee(caller))
        {
            ownerEmployeeId = accountId;
            return true;
        }

        if (IsCandidate(caller))
        {
            if (candidateId.HasValue && candidateId.Value != accountId) return false;
            requestedCandidateId = accountId;
            return true;
        }

        return false;
    }

    private static bool TryGetAccountId(ClaimsPrincipal caller, out int accountId)
    {
        var subject = caller.FindFirstValue("sub") ?? caller.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out accountId);
    }

    private static int? GetAccountId(ClaimsPrincipal caller) =>
        TryGetAccountId(caller, out var accountId) ? accountId : null;

    private static bool IsAdmin(ClaimsPrincipal caller) => caller.IsInRole(EmployeeRoles.Admin);

    private static bool IsEmployee(ClaimsPrincipal caller) => caller.IsInRole(EmployeeRoles.Employee);

    private static bool IsCandidate(ClaimsPrincipal caller) => caller.IsInRole(EmployeeRoles.Candidate);

    private static bool IsKnownWorkStatus(string status) =>
        status.Equals(CandidateWorkStatuses.Pending, StringComparison.OrdinalIgnoreCase)
        || status.Equals(CandidateWorkStatuses.Approved, StringComparison.OrdinalIgnoreCase)
        || status.Equals(CandidateWorkStatuses.Rejected, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeWorkStatus(string? status) =>
        status is null ? null
        : status.Equals(CandidateWorkStatuses.Pending, StringComparison.OrdinalIgnoreCase) ? CandidateWorkStatuses.Pending
        : status.Equals(CandidateWorkStatuses.Approved, StringComparison.OrdinalIgnoreCase) ? CandidateWorkStatuses.Approved
        : status.Equals(CandidateWorkStatuses.Rejected, StringComparison.OrdinalIgnoreCase) ? CandidateWorkStatuses.Rejected
        : status;

    private static bool TryNormalizeLimit(int limit, out int normalizedLimit)
    {
        normalizedLimit = limit == 0 ? DefaultResultLimit : limit;
        return normalizedLimit is >= 1 and <= MaximumResultLimit;
    }

    private static bool? ParseActiveFilter(string question)
    {
        var mentionsActive = Regex.IsMatch(question, @"\bactive\b");
        var mentionsInactive = Regex.IsMatch(question, @"\binactive\b");
        return mentionsActive == mentionsInactive ? null : !mentionsInactive;
    }

    private static string? ParseSingleStatus(string question)
    {
        var matches = new[]
        {
            (CandidateWorkStatuses.Pending, Regex.IsMatch(question, @"\bpending\b")),
            (CandidateWorkStatuses.Approved, Regex.IsMatch(question, @"\bapproved\b")),
            (CandidateWorkStatuses.Rejected, Regex.IsMatch(question, @"\brejected\b"))
        }.Where(item => item.Item2).Select(item => item.Item1).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.Ordinal));

    private static bool IsCountRequest(string question) =>
        Regex.IsMatch(question, @"\bhow many\b")
        || Regex.IsMatch(question, @"^\s*(?:please\s+)?(?:count|counts|total)\b")
        || Regex.IsMatch(question, @"\b(?:show|get|give|what is|tell me)\s+(?:the\s+)?(?:count|total)\b")
        || Regex.IsMatch(question, @"\b(?:count|counts|total)\s+(?:of\s+)?(?:employees?|candidates?|work|tasks?|reviews?)\b")
        || Regex.IsMatch(question, @"\b(?:employees?|candidates?)\s+counts?\b");

    private static bool IsListDetailsRequest(string question) =>
        Regex.IsMatch(question, @"\b(list|details?|full details)\b");

    private static int? ParseCandidateId(string question)
    {
        var match = Regex.Match(question, @"\bcandidate\s+(?:id\s*)?#?(\d+)\b");
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }

    private static string? ExtractSpecificSearchTerm(string question)
    {
        var ignoredTerms = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "about", "account", "accounts", "active", "all", "and", "are", "by", "candidate", "candidates",
            "count", "counts", "details", "employee", "employees", "entries", "entry", "find", "for", "get",
            "has", "have", "how", "id", "inactive", "in", "is", "list", "many", "me", "mentor", "my", "name", "names", "of", "on",
            "or", "pending", "please", "record", "records", "rejected", "review", "reviews", "search", "show",
            "staff", "status", "task", "tasks", "the", "this", "what", "with", "work", "approved", "called", "named",
            "number", "scheduled", "entries"
        };
        var terms = Regex.Matches(question, @"[a-z0-9@._-]+")
            .Select(match => match.Value)
            .Where(term => !ignoredTerms.Contains(term) && !int.TryParse(term, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            .ToArray();
        return terms.Length == 0 ? null : string.Join(' ', terms);
    }

    private static PortalChatItemDto ToNameItem(PortalChatEmployeeDto employee) => new(
        "Employee",
        employee.DisplayName,
        employee.IsActive ? "Active" : "Inactive",
        new Dictionary<string, string?>());

    private static PortalChatItemDto ToNameItem(PortalChatCandidateDto candidate) => new(
        "Candidate",
        $"{candidate.FirstName} {candidate.LastName}",
        candidate.IsActive ? "Active" : "Inactive",
        new Dictionary<string, string?>());

    private static PortalChatItemDto ToItem(PortalChatEmployeeDto employee) => new(
        "Employee",
        employee.DisplayName,
        employee.IsActive ? "Active" : "Inactive",
        new Dictionary<string, string?>
        {
            ["Username"] = employee.Username,
            ["Department"] = employee.Department,
            ["Technology stack"] = employee.Stack,
            ["Created (UTC)"] = employee.CreatedAtUtc.ToString("u", CultureInfo.InvariantCulture)
        });

    private static PortalChatItemDto ToItem(PortalChatCandidateDto candidate) => new(
        "Candidate",
        $"{candidate.FirstName} {candidate.LastName}",
        candidate.IsActive ? "Active" : "Inactive",
        new Dictionary<string, string?>
        {
            ["Username"] = candidate.Username,
            ["Email"] = candidate.Email,
            ["Phone"] = candidate.PhoneNumber,
            ["Technology stack"] = candidate.Stack,
            ["Interview decision"] = candidate.InterviewDecision,
            ["Pending tasks"] = candidate.PendingTaskCount.ToString(CultureInfo.InvariantCulture),
            ["Approved tasks"] = candidate.ApprovedTaskCount.ToString(CultureInfo.InvariantCulture),
            ["Rejected tasks"] = candidate.RejectedTaskCount.ToString(CultureInfo.InvariantCulture)
        });

    private static PortalChatItemDto ToItem(PortalChatWorkEntryDto entry) => new(
        $"Work entry #{entry.Id}",
        $"{entry.CandidateName}: {entry.Task}",
        entry.Status,
        new Dictionary<string, string?>
        {
            ["Work date"] = entry.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["Hours"] = entry.HoursSpent.ToString(CultureInfo.InvariantCulture),
            ["Review comment"] = entry.ReviewComment,
            ["Submitted (UTC)"] = entry.SubmittedAtUtc.ToString("u", CultureInfo.InvariantCulture),
            ["Reviewed (UTC)"] = entry.ReviewedAtUtc?.ToString("u", CultureInfo.InvariantCulture)
        });
}
