namespace fullstackapplication.Server.Models;

public sealed class EmployeeRoleDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? CreatedByEmployeeAccountId { get; set; }

    public bool IsBuiltIn { get; set; }

    public EmployeeAccount? CreatedByEmployeeAccount { get; set; }

    public ICollection<EmployeeRolePermission> Permissions { get; set; } = new List<EmployeeRolePermission>();
}
