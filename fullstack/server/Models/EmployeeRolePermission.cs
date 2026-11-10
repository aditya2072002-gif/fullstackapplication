namespace fullstackapplication.Server.Models;

public sealed class EmployeeRolePermission
{
    public int EmployeeRoleDefinitionId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public EmployeeRoleDefinition RoleDefinition { get; set; } = null!;
}
