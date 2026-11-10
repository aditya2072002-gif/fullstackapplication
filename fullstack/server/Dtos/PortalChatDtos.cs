using System.ComponentModel.DataAnnotations;

namespace fullstackapplication.Server.Dtos;

public sealed class PortalChatRequestDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public sealed record PortalChatAnswerDto(string Answer, IReadOnlyList<PortalChatItemDto> Items);

public sealed record PortalChatSearchResultDto<T>(int MatchingCount, IReadOnlyList<T> Records);

public sealed record PortalChatItemDto(
    string Type,
    string Title,
    string Status,
    IReadOnlyDictionary<string, string?> Details);

public sealed record PortalChatEmployeeDto(
    int Id,
    string Username,
    string DisplayName,
    string? Department,
    string? Stack,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PortalChatCandidateDto(
    int Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Stack,
    bool IsActive,
    string InterviewDecision,
    int PendingTaskCount,
    int ApprovedTaskCount,
    int RejectedTaskCount);

public sealed record PortalChatWorkEntryDto(
    int Id,
    int CandidateId,
    string CandidateName,
    DateOnly WorkDate,
    string Task,
    decimal HoursSpent,
    string Status,
    string? ReviewComment,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);
