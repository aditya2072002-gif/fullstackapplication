using System.ComponentModel.DataAnnotations;
using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Dtos;

public sealed class SearchCandidateWorkEntriesRequest : IValidatableObject
{
    public int? CandidateId { get; set; }

    public string? Name { get; set; }

    public string? Stack { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public string? Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
        {
            yield return new ValidationResult(
                "The start date must be on or before the end date.",
                new[] { nameof(DateTo) });
        }

        if (!string.IsNullOrWhiteSpace(Status) && !CandidateWorkStatuses.All.Contains(Status))
        {
            yield return new ValidationResult("Status must be Pending, Approved, or Rejected.", new[] { nameof(Status) });
        }
    }
}

public sealed class CandidateWorkEntryRequest
{
    [Required]
    public DateOnly WorkDate { get; set; }

    [Required, StringLength(200)]
    public string Task { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.1", "24")]
    public decimal HoursSpent { get; set; }

    [Required, StringLength(2000)]
    public string Accomplishment { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Learnings { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string BlockersAndChallenges { get; set; } = string.Empty;

    [StringLength(500)]
    public string AiToolsUsed { get; set; } = string.Empty;
}

public sealed class ReviewCandidateWorkEntriesRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int CandidateId { get; set; }

    [Required, MinLength(1)]
    public IReadOnlyList<int> EntryIds { get; set; } = Array.Empty<int>();

    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EntryIds.Any(id => id <= 0) || EntryIds.Distinct().Count() != EntryIds.Count)
        {
            yield return new ValidationResult("Entry IDs must be positive and unique.", new[] { nameof(EntryIds) });
        }

        if (Status is not CandidateWorkStatuses.Approved and not CandidateWorkStatuses.Rejected)
        {
            yield return new ValidationResult("Status must be Approved or Rejected.", new[] { nameof(Status) });
        }

        if (Status == CandidateWorkStatuses.Rejected && string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult("A comment is required when rejecting work.", new[] { nameof(Comment) });
        }
    }
}

public sealed class UpdateCandidateInterviewDecisionRequest : IValidatableObject
{
    [Required]
    public string Decision { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Decision is not CandidateInterviewDecisions.InterviewQualified and not CandidateInterviewDecisions.NotQualified)
        {
            yield return new ValidationResult("Decision must be InterviewQualified or NotQualified.", new[] { nameof(Decision) });
        }
    }
}

public sealed record CandidateWorkEntryResponse(
    int Id,
    int CandidateId,
    string CandidateFirstName,
    string CandidateLastName,
    string Stack,
    DateOnly WorkDate,
    string Task,
    decimal HoursSpent,
    string Accomplishment,
    string Learnings,
    string BlockersAndChallenges,
    string AiToolsUsed,
    string Status,
    string? ReviewComment,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    int? ReviewedByEmployeeAccountId,
    string? ReviewedByDisplayName,
    int? MentorEmployeeAccountId,
    string? MentorDisplayName);

public sealed class UpdateCandidateStackRequest
{
    [StringLength(200)]
    public string Stack { get; set; } = string.Empty;
}
