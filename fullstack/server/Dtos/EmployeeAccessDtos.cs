using System.ComponentModel.DataAnnotations;

namespace fullstackapplication.Server.Dtos;

public sealed class CreateEmployeeRoleRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateEmployeeRolePermissionsRequest
{
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

public sealed class AssignEmployeeRoleRequest
{
    public int? RoleDefinitionId { get; set; }
}

public sealed class UpdateEmployeeManagedStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public sealed record EmployeeRoleResponseDto(int Id, string Name, bool IsBuiltIn, IReadOnlyList<string> Permissions);

public sealed record PermissionOptionDto(string Code, string Label);

public sealed record EmployeeAccessSummaryDto(int ActiveEmployees, int InactiveEmployees);
