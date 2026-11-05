using System.ComponentModel.DataAnnotations;

namespace fullstackapplication.Server.Dtos;

public sealed class LoginRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
