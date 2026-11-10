namespace fullstackapplication.Server.Models;

public sealed class CandidateProfile
{
    public int EmployeeAccountId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string LegacyExperienceDetails { get; set; } = string.Empty;

    public int? YearsOfExperience { get; set; }

    public string Stack { get; set; } = string.Empty;

    public int PerformanceScore { get; set; }

    public string InterviewDecision { get; set; } = CandidateInterviewDecisions.PendingReview;

    public string? InterviewDecisionComment { get; set; }

    public DateTime? InterviewDecisionAtUtc { get; set; }

    public int? InterviewDecisionByEmployeeAccountId { get; set; }

    public int? CreatorEmployeeAccountId { get; set; }

    public EmployeeAccount? CreatorEmployeeAccount { get; set; }

    public ICollection<CandidateWorkEntry> WorkEntries { get; set; } = new List<CandidateWorkEntry>();

    public byte[]? ResumeContent { get; set; }

    public string? ResumeFileName { get; set; }

    public string? ResumeContentType { get; set; }

    public EmployeeAccount EmployeeAccount { get; set; } = null!;
}
