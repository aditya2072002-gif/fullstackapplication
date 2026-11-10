using System.ComponentModel.DataAnnotations;

namespace fullstackapplication.Server.Dtos;

public sealed class CreateEmployeeRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Stack { get; set; } = string.Empty;
}

public sealed record EmployeeResponseDto(
    int Id,
    string Username,
    string DisplayName,
    string Department,
    string Stack,
    string Role,
    bool IsActive,
    int? CreatedByEmployeeAccountId = null,
    int? RoleDefinitionId = null,
    string? RoleDefinitionName = null,
    IReadOnlyList<string>? Permissions = null);

public sealed class UpdateEmployeeCandidateRequest
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(200)]
    public string Stack { get; set; } = string.Empty;
}

public sealed class UpdateEmployeeCandidateStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public sealed class CreateEmployeeCandidateRequest
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

    [StringLength(200)]
    public string Stack { get; set; } = string.Empty;
}
