namespace fullstackapplication.Server.Models;

public static class EmployeePermissions
{
    public const string ViewCandidates = "Candidates.View";
    public const string CreateCandidates = "Candidates.Create";
    public const string UpdateCandidates = "Candidates.Update";
    public const string DeactivateCandidates = "Candidates.Deactivate";
    public const string DeleteCandidates = "Candidates.Delete";
    public const string ReviewWork = "Reviews.Work";
    public const string DecideInterviews = "Reviews.InterviewDecision";
    public const string ViewTeam = "Team.View";
    public const string CreateEmployees = "Team.CreateEmployees";
    public const string ManageEmployees = "Team.ManageEmployees";
    public const string DeleteEmployees = "Team.DeleteEmployees";
    public const string CreateRoles = "Team.CreateRoles";
    public const string GrantPermissions = "Team.GrantPermissions";
    public const string ManageRolePermissions = "Team.ManageRolePermissions";

    public static IReadOnlyDictionary<string, string> Catalog { get; } = new Dictionary<string, string>
    {
        [ViewCandidates] = "View owned candidates",
        [CreateCandidates] = "Create candidates",
        [UpdateCandidates] = "Update candidate profiles",
        [DeactivateCandidates] = "Activate or deactivate candidates",
        [DeleteCandidates] = "Delete inactive candidates",
        [ReviewWork] = "Review candidate work",
        [DecideInterviews] = "Record interview decisions",
        [ViewTeam] = "View directly managed employees",
        [CreateEmployees] = "Create directly managed employees",
        [ManageEmployees] = "Activate or deactivate directly managed employees",
        [DeleteEmployees] = "Delete inactive directly managed employees",
        [CreateRoles] = "Create permission roles",
        [GrantPermissions] = "Assign roles to directly managed employees",
        [ManageRolePermissions] = "Grant permissions to roles"
    };
}
