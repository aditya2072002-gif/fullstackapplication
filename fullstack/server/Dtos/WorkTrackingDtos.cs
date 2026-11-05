using System.ComponentModel.DataAnnotations;

namespace fullstackapplication.Server.Dtos;

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

public sealed class ReviewCandidateWorkEntryRequest
{
    [Range(0, 100)]
    public int Grade { get; set; }

    [StringLength(2000)]
    public string? AdminFeedback { get; set; }
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
    int? Grade,
    string? AdminFeedback,
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
