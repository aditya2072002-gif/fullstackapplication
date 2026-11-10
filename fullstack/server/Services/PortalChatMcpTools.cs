using System.ComponentModel;
using System.Security.Claims;
using fullstackapplication.Server.Dtos;
using ModelContextProtocol.Server;

namespace fullstackapplication.Server.Services;

[McpServerToolType]
public static class PortalChatMcpTools
{
    [McpServerTool, Description("Read-only employee lookup. Admins can search all employees; employees with Team.View can search only their direct reports. Returns matching counts; employee records are included only when a specific search term identifies exactly one match.")]
    public static async Task<PortalChatSearchResultDto<PortalChatEmployeeDto>> SearchEmployees(
        [Description("Optional specific text to match against username, display name, department, or technology stack. Leave blank for a count only.")] string? search,
        [Description("Set true for active accounts, false for inactive accounts, or null for all employees.")] bool? isActive,
        ClaimsPrincipal caller,
        IPortalChatService chatService,
        CancellationToken cancellationToken)
    {
        var count = Require(await chatService.CountEmployeesAsync(caller, search, isActive, cancellationToken));
        var records = !string.IsNullOrWhiteSpace(search) && count == 1
            ? Require(await chatService.SearchEmployeesAsync(caller, search, isActive, 25, cancellationToken))
            : Array.Empty<PortalChatEmployeeDto>();
        return new PortalChatSearchResultDto<PortalChatEmployeeDto>(count, records);
    }

    [McpServerTool, Description("Read-only candidate lookup. Admins see all; Employees see candidates they created; Candidates see only their own profile. Returns matching counts; candidate records are included only for a specific candidate ID or a search term identifying exactly one match.")]
    public static async Task<PortalChatSearchResultDto<PortalChatCandidateDto>> SearchCandidates(
        [Description("Optional specific text to match against candidate name, username, email, or technology stack. Leave blank for a count only.")] string? search,
        [Description("Set true for active accounts, false for inactive accounts, or null for all candidates.")] bool? isActive,
        [Description("Optional specific candidate account ID. The caller's role and ownership still limit the result.")] int? candidateId,
        ClaimsPrincipal caller,
        IPortalChatService chatService,
        CancellationToken cancellationToken)
    {
        var count = Require(await chatService.CountCandidatesAsync(caller, search, isActive, candidateId, cancellationToken));
        var hasSpecificMatch = candidateId.HasValue || (!string.IsNullOrWhiteSpace(search) && count == 1);
        var records = hasSpecificMatch
            ? Require(await chatService.SearchCandidatesAsync(caller, search, isActive, candidateId, 25, cancellationToken))
            : Array.Empty<PortalChatCandidateDto>();
        return new PortalChatSearchResultDto<PortalChatCandidateDto>(count, records);
    }

    [McpServerTool, Description("Read-only work-review lookup. Without a specific candidate ID, returns only the matching count. A specific candidate ID returns that candidate's matching work entries and their count. Admins see all; Employees see entries for candidates they created; Candidates see only their own entries.")]
    public static async Task<PortalChatSearchResultDto<PortalChatWorkEntryDto>> SearchWorkReviews(
        [Description("Optional status filter: Pending, Approved, or Rejected.")] string? status,
        [Description("Optional specific candidate account ID. Leave blank for a count only. The caller's role and ownership still limit the result.")] int? candidateId,
        ClaimsPrincipal caller,
        IPortalChatService chatService,
        CancellationToken cancellationToken)
    {
        var count = Require(await chatService.CountWorkEntriesAsync(caller, status, candidateId, cancellationToken));
        var records = candidateId.HasValue
            ? Require(await chatService.SearchWorkEntriesAsync(caller, status, candidateId, 25, cancellationToken))
            : Array.Empty<PortalChatWorkEntryDto>();
        return new PortalChatSearchResultDto<PortalChatWorkEntryDto>(count, records);
    }

    private static T Require<T>(ServiceResult<T> result) => result.Kind switch
    {
        ServiceResultKind.Success when result.Value is not null => result.Value,
        ServiceResultKind.Forbidden => throw new UnauthorizedAccessException(result.Error),
        ServiceResultKind.Invalid => throw new ArgumentException(result.Error),
        _ => throw new InvalidOperationException("The requested MCP data could not be loaded.")
    };
}
