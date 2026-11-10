namespace fullstackapplication.Server.Models;

public sealed class EmployeeAccount
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? Stack { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime? InactiveAtUtc { get; set; }

    public CandidateProfile? CandidateProfile { get; set; }
}
