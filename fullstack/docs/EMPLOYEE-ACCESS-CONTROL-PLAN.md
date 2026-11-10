# Employee access control and delegated management

## Implementation summary

Employee accounts now keep the existing `Employee` authentication role and receive capabilities through an assigned permission role. This preserves current portal routing and keeps Admin and Candidate as system roles. A manager can create permission templates only from permissions they already hold, assign only roles within their grantable permission set, and manage only employees they directly created.

The `AddEmployeeAccessRoles` migration creates role definitions and permission rows, adds the creator and role references to employee accounts, and assigns the built-in `Mentor` role to existing employees with the capabilities they previously had. Newly created employees receive no capabilities unless a role is explicitly granted through the UI. Legacy Admin API clients that omit `grantAccess` continue to receive the built-in Mentor role; the updated UI explicitly sends the checkbox choice.

## Permission catalog

| Permission | Capability |
|---|---|
| `Candidates.View` | List and view candidates created by the employee. |
| `Candidates.Create` | Create candidates owned by the employee. |
| `Candidates.Update` | Update profiles for candidates owned by the employee. |
| `Candidates.Deactivate` | Activate or deactivate owned candidates. |
| `Candidates.Delete` | Permanently delete an owned candidate after it is inactive. |
| `Reviews.Work` | View and review work entries for owned candidates. |
| `Reviews.InterviewDecision` | Record interview decisions for owned candidates. |
| `Team.View` | View direct reports and their active/inactive summary. |
| `Team.CreateEmployees` | Create direct reports. |
| `Team.ManageEmployees` | Activate or deactivate direct reports. |
| `Team.DeleteEmployees` | Permanently delete inactive direct reports that have no dependent records. |
| `Team.CreateRoles` | Create roles using only the creator's current permissions. |
| `Team.GrantPermissions` | Assign or remove roles for direct reports, without exceeding the caller's permissions. |

Admin accounts are the administrative override. Candidate and Employee base roles are not user-definable, and a custom role cannot elevate an employee to Admin. The UI hides unavailable actions, but the API independently checks each permission and data-ownership boundary.

## Server endpoints

| Method and route | Access | Purpose |
|---|---|---|
| `GET /api/roles/permissions` | Authenticated | Return the caller's effective permissions; include the permission catalog when role creation is allowed. |
| `GET /api/roles` | Admin or employee with `Team.GrantPermissions` | List roles the caller may assign. Employee results exclude roles containing permissions the caller does not have. |
| `POST /api/roles` | Admin or employee with `Team.CreateRoles` | Create a named role with a non-empty subset of the caller's grantable permissions. |
| `PUT /api/roles/employees/{employeeId}` | Admin or employee with `Team.GrantPermissions` | Assign or clear a role on an employee in the caller's direct-report scope. |
| `GET /api/employee/managed-employees` | Employee with a team-management capability | List direct reports only, including role and active status. |
| `GET /api/employee/managed-employees/summary` | Employee with `Team.View` | Return active and inactive counts for direct reports. |
| `POST /api/employee/managed-employees` | Employee with `Team.CreateEmployees` | Create a direct report. A role is granted only when selected and authorized. |
| `PATCH /api/employee/managed-employees/{employeeId}/status` | Employee with `Team.ManageEmployees` | Activate or deactivate a direct report. Deactivation invalidates the employee's JWT on its next request. |
| `DELETE /api/admin/employees/{employeeId}` | Admin with `Team.DeleteEmployees` | Permanently delete an inactive employee with no dependent records. |
| `DELETE /api/employee/managed-employees/{employeeId}` | Employee with `Team.DeleteEmployees` | Permanently delete an inactive direct report with no dependent records. |
| `GET /api/admin/employees` | Admin | List all employee accounts. |
| `POST /api/admin/employees` | Admin or employee with `Team.CreateEmployees` | Create an employee; employee callers are recorded as the creator. |
| `PATCH /api/admin/employees/{employeeId}/status` | Admin or employee with `Team.ManageEmployees` | Change status, with employee callers restricted to their direct reports. |
| `GET /api/employee/candidates` and `GET /api/employee/candidates/{candidateId}` | Employee with `Candidates.View` or `Reviews.Work` | List or view only candidates created by that employee. |
| `POST /api/employee/candidates` | Employee with `Candidates.Create` | Create an owned candidate. |
| `PUT /api/employee/candidates/{candidateId}` | Employee with `Candidates.Update` | Update an owned candidate profile. |
| `PATCH /api/employee/candidates/{candidateId}/status` | Employee with `Candidates.Deactivate` | Activate or deactivate an owned candidate. |
| `DELETE /api/employee/candidates/{candidateId}` | Employee with `Candidates.Delete` | Delete an owned inactive candidate. |
| `GET /api/admin/work-tracking` and `PUT /api/admin/work-tracking/review` | Admin or Employee with `Reviews.Work` | Search and review entries; employee results and writes remain restricted to owned candidates. |
| `PUT /api/admin/candidates/{candidateId}/interview-decision` | Admin or Employee with `Reviews.InterviewDecision` | Record an interview decision; employee writes remain owner-scoped. |

The existing authenticated `/mcp` tools and `/api/chat/ask` assistant remain read-only. Employee lookups require `Team.View` and return direct reports only; candidate and review lookups apply the corresponding view/review permission and creator scope. MCP does not provide account, role, candidate, or review write operations.

## Client pages and behavior

- **Admin workspace → Employees** uses `EmployeeManagement.jsx` to create employee accounts, check **Grant a permission role**, select from assignable roles, create roles with permission checkboxes, reassign roles, and activate/deactivate employees.
- **Employee workspace → My team** uses the same component in manager mode to manage direct reports within the caller's scope and display the direct-report active/inactive `StatusDonut`.
- **Employee workspace navigation** is populated from `/api/roles/permissions`. Candidate, review, and team sections appear only when the account has the corresponding capability.
- **My candidates** hides create, edit, activate/deactivate, and delete controls unless the relevant permission is present. Delete is offered only for inactive candidates and still requires server authorization.
- **Work reviews** hides interview-decision controls unless `Reviews.InterviewDecision` is granted. Review reads and writes require `Reviews.Work` on the API.

## Data and authorization flow

1. Admins create the initial role templates and grant only intended permissions to employees.
2. An employee with `Team.CreateEmployees` can create direct reports. The account's `CreatedByEmployeeAccountId` records the manager; an optional role is accepted only with `Team.GrantPermissions`.
3. Role permissions are stored in `EmployeeRoleDefinitions` and `EmployeeRolePermissions`; each employee references one role definition. A role update takes effect on the next API authorization check and does not require reissuing a token.
4. Employee candidate, team, MCP, chat, work-review, and interview operations combine permission checks with creator scoping in server services/repositories. UI visibility is not treated as a security boundary.
5. Deactivating an employee causes JWT validation to reject that employee on subsequent requests, including MCP and SignalR endpoints.
6. Team charts count only direct reports, not the full descendant hierarchy. Candidate and review charts continue using the existing candidate-owner scope.

## Implementation and verification

- Models and permissions: `server/Models/EmployeeAccount.cs`, `EmployeeRoleDefinition.cs`, `EmployeeRolePermission.cs`, `EmployeePermissions.cs`.
- Persistence: `server/Data/ApplicationDbContext.cs`, `server/Migrations/*AddEmployeeAccessRoles*`, and `server/Repositories/EmployeeAccessRepository.cs`.
- Authorization/business logic: `server/Services/PermissionService.cs`, `EmployeeAccessService.cs`, `EmployeeManagementService.cs`, and the candidate, work-review, and MCP services.
- HTTP surface: `server/Controllers/EmployeeRolesController.cs`, `EmployeeAccessController.cs`, `AdminEmployeesController.cs`, and existing candidate/review controllers.
- UI: `client/src/EmployeeManagement.jsx`, `EmployeePortal.jsx`, `PortalShell.jsx`, `EmployeeCandidates.jsx`, and `WorkReviews.jsx`.

Production databases must apply the new EF migration using the repository's documented reviewed migration procedure. The focused delegated-role integration test covers role assignment, candidate permission enforcement, direct-report counts, and immediate deactivation. The client production build and server build should also pass before deployment.
