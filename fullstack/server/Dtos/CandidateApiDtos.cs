using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace fullstackapplication.Server.Dtos;

public sealed class CreateCandidateRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, 99)]
    public int YearsOfExperience { get; set; }

    [StringLength(200)]
    public string Stack { get; set; } = string.Empty;

    [Range(0, 100)]
    public int PerformanceScore { get; set; }

    public IFormFile? Resume { get; set; }
}

public sealed class UpdateCandidateRequest
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Range(1, 99)]
    public int YearsOfExperience { get; set; }

    [StringLength(200)]
    public string Stack { get; set; } = string.Empty;

    [Range(0, 100)]
    public int PerformanceScore { get; set; }

    [Required]
    public bool? IsActive { get; set; }

    public IFormFile? Resume { get; set; }
}

public sealed class UpdateCandidateStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public sealed record CandidateProfileResponse(
    int Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    int? YearsOfExperience,
    string Stack,
    int PerformanceScore,
    bool IsActive,
    DateTime? InactiveAtUtc,
    string? ResumeFileName,
    string? ResumeContentType,
    int? CreatorEmployeeAccountId = null,
    string? CreatorDisplayName = null,
    DateTime? AccountCreatedAtUtc = null,
    string InterviewDecision = "PendingReview",
    string? InterviewDecisionComment = null,
    DateTime? InterviewDecisionAtUtc = null,
    int? InterviewDecisionByEmployeeAccountId = null,
    DateTime? WorkSubmissionEndsAtUtc = null,
    bool CanSubmitWork = false,
    bool CanSetInterviewDecision = false);

public sealed class ResumeUploadRequest
{
    [Required]
    public IFormFile? Resume { get; set; }
}

public sealed class ChangeCandidatePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; set; } = string.Empty;
}
