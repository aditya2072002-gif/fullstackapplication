namespace fullstackapplication.Server.Models;

public sealed class CandidateWorkEntry
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string Task { get; set; } = string.Empty;

    public decimal HoursSpent { get; set; }

    public string Accomplishment { get; set; } = string.Empty;

    public string Learnings { get; set; } = string.Empty;

    public string BlockersAndChallenges { get; set; } = string.Empty;

    public string AiToolsUsed { get; set; } = string.Empty;

    public int? Grade { get; set; }

    public string? AdminFeedback { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = null!;
}
