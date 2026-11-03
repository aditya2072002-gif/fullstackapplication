namespace fullstackapplication.Server.Models;

public sealed class CandidateProfile
{
    public int EmployeeAccountId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Experience { get; set; } = string.Empty;

    public string Stack { get; set; } = string.Empty;

    public int PerformanceScore { get; set; }

    public ICollection<CandidateWorkEntry> WorkEntries { get; set; } = new List<CandidateWorkEntry>();

    public byte[]? ResumeContent { get; set; }

    public string? ResumeFileName { get; set; }

    public string? ResumeContentType { get; set; }

    public EmployeeAccount EmployeeAccount { get; set; } = null!;
}
