# Fullstack Application — ASP.NET Core API

## Server folder structure

```text
server/
├── Controllers/       API routes, authorization, and HTTP response mapping
├── Hubs/              Authenticated SignalR endpoints
├── Data/
├── Dtos/              Validated API request and response models
├── Migrations/        EF Core database schema history
├── Models/            Persisted entities and role definitions
├── Repositories/      Database queries and persistence
├── Services/          Authentication, JWT, business logic, and notifications
├── Properties/        ASP.NET Core launch profiles
├── Program.cs         Configuration, dependency injection, auth, and startup
├── appsettings*.json  Application and environment configuration
├── *.csproj           Server project and package references
└── README.md          Server API documentation
```

## Application layers

Controllers handle HTTP binding, authorization, and response mapping. Request and response DTOs are in `Dtos`; business rules and orchestration are in `Services`; EF Core queries and persistence are in `Repositories`, backed by `Data\ApplicationDbContext`. Repositories return materialized entities or explicit results rather than exposing `IQueryable` to controllers or services.

## Endpoints

In Development, run the API and open `/swagger` (for example, `https://localhost:7216/swagger`) to explore and call the endpoints interactively. Use `POST /api/auth/login` with the demo credentials, copy `accessToken`, select **Authorize**, and enter `Bearer <accessToken>` to call the protected list endpoints. Swagger UI and its generated Swagger document are not enabled outside Development. The existing OpenAPI JSON endpoint remains at `/openapi/v1.json`.

- `POST /api/auth/login` accepts `{"username":"admin","password":"AdminDemo123!"}` and returns a bearer token plus a safe user summary.
- `GET /api/admins` returns active Admin account summaries.
- `GET /api/candidates` returns active Candidate account summaries.

The list endpoints require an Admin JWT. Candidate accounts can authenticate, but receive `403 Forbidden` when requesting either list. Password hashes are never included in API responses.

### Employee account management

`GET /api/admin/employees` is Admin-only. Admins can create, assign roles to, and change status for employee accounts. Employees with the relevant team permissions can create and manage only the employees they directly created. Passwords are hashed before persistence; responses never include a password or hash. See [Employee access control and delegated management](../docs/EMPLOYEE-ACCESS-CONTROL-PLAN.md) for the permission catalog and implementation details.

| Method and route | Access | Purpose |
|---|---|---|
| `GET /api/admin/employees` | Admin | List employee accounts. |
| `POST /api/admin/employees` | Admin or employee with `Team.CreateEmployees` | Create an Employee account without assigning a permission role. |
| `PATCH /api/admin/employees/{employeeId}/status` | Admin or employee with `Team.ManageEmployees` | Activate or deactivate an employee; employee managers are limited to direct reports. |
| `GET /api/employee/managed-employees` | Employee with team-management access | List only direct reports, with assigned role and status. |
| `GET /api/employee/managed-employees/summary` | Employee with `Team.View` | Return active/inactive direct-report counts. |
| `POST /api/employee/managed-employees` | Employee with `Team.CreateEmployees` | Create a direct report without assigning a permission role. |
| `PATCH /api/employee/managed-employees/{employeeId}/status` | Employee with `Team.ManageEmployees` | Change status for a direct report. |

### Employee permission roles

The base Employee role remains fixed for authentication. `EmployeeRoleDefinitions` and `EmployeeRolePermissions` store custom permission templates, and each employee may be assigned one template. Role creation, permission management, and employee assignment are separate features with separate capabilities: `Team.CreateRoles` allows `POST /api/roles` to create an empty role; `Team.ManageRolePermissions` allows `PUT /api/roles/{roleId}/permissions` to grant or remove permissions on that specific role; and `Team.GrantPermissions` allows `PUT /api/roles/employees/{employeeId}` to assign or clear a role for an existing direct report. `GET /api/roles/permissions` returns the current user's capabilities and only exposes the permission catalog to callers with `Team.ManageRolePermissions`; `GET /api/roles` lists roles the caller may assign or manage. Admins can grant any catalog permission. Employees can only add permissions they already hold, assign roles whose permissions they hold, and manage only their direct reports. Employee creation never assigns a role. An employee's access is checked from the database, so role changes take effect on subsequent requests without changing the JWT.

The `AddEmployeeAccessRoles` migration assigns existing employees the built-in Mentor role with their legacy candidate-creation and review capabilities. Newly created employees have no assigned permissions until an authorized user assigns a role after creation. Deactivated Employee accounts are rejected during JWT validation on subsequent API, MCP, and SignalR requests.

### Employee candidate management

These routes require an Employee JWT and the matching permission. `Candidates.View` (or `Reviews.Work` for review workflows) allows listing and viewing only candidates created by the caller. `Candidates.Create`, `Candidates.Update`, and `Candidates.Deactivate` independently gate those operations; `Candidates.Delete` permits deletion only for an owned inactive candidate. Candidate ownership is enforced by the service/repository even when a caller has the permission.

| Method and route | Purpose |
|---|---|
| `GET /api/employee/candidates` | List candidates created by the signed-in Employee. |
| `GET /api/employee/candidates/{candidateId}` | Get an owned candidate. |
| `POST /api/employee/candidates` | Create a Candidate assigned to the signed-in Employee. |
| `PUT /api/employee/candidates/{candidateId}` | Update an owned candidate's profile. |
| `PATCH /api/employee/candidates/{candidateId}/status` | Activate or deactivate an owned candidate. |
| `DELETE /api/employee/candidates/{candidateId}` | Delete an owned inactive candidate when `Candidates.Delete` is granted. |

Admins manage the broader candidate directory through `/api/admin/candidates`. Admins and Employees with `Reviews.Work` can search `/api/admin/work-tracking`; Employees see only candidates they created. Task review requires `Reviews.Work`, and interview decisions require `Reviews.InterviewDecision`.

### Portal chat and MCP lookup

The authenticated floating portal assistant sends natural-language questions to `POST /api/chat/ask` with `{"message":"Show pending work entries"}`. It supports employee, candidate, active/inactive, and Pending/Approved/Rejected work lookups. Explicit count requests return counts only. Name searches show matching names; list/details requests show matching records (up to 25 per lookup). Requests to create, insert, update, delete, approve, reject, or otherwise change data receive an access-denied assistant reply. Chat lookups use a small server-side intent matcher and do not send prompts or application data to an external AI provider.

The API also exposes an authenticated, stateless Streamable HTTP MCP endpoint at `POST /mcp` using `ModelContextProtocol.AspNetCore` (the .NET 9-compatible C# SDK package). Registered read-only tools return count-only results for broad queries and details only for specific matches:

| MCP tool | Access and results |
|---|---|
| `search_employees` | Admin sees all; employees with `Team.View` see only direct reports. Returns matching counts; employee records are included only when a specific search term identifies exactly one match. |
| `search_candidates` | Admin sees all; Employee requires `Candidates.View` and sees only candidates they created; Candidate sees only their own profile. Returns matching counts; records are included for a specific candidate ID or a search term identifying exactly one candidate. |
| `search_work_reviews` | Admin sees all; Employee requires `Reviews.Work` and sees entries for owned candidates; Candidate sees only their own entries. Without a candidate ID, returns only a matching count; a specific candidate ID returns matching entries. |

Both chat and MCP tools use injected services and repositories. Controllers only bind validated DTOs, call services, and map results; EF Core/SQL Server access stays in repositories. Caller ID, role, and current database-backed permissions determine access; arbitrary SQL or write operations are not exposed. API and MCP responses are marked `no-store`. No chat history is stored; employee-role persistence is introduced by the `AddEmployeeAccessRoles` migration.

### JWT authentication implementation

1. `Controllers/AuthController.cs` exposes the public `POST /api/auth/login` endpoint.
2. `Services/AuthService.cs` finds an active account, verifies its password hash, and delegates token creation after successful authentication.
3. `Services/JwtTokenService.cs` signs a JWT with HMAC-SHA256. The token includes the account ID (`sub`), username (`unique_name`), role (`role`), issuer, audience, and expiry.
4. `Program.cs` configures JWT bearer authentication to validate the issuer, audience, signing key, and token lifetime, rejects inactive Employee accounts, then enables role-based authorization for controllers.
5. Authenticated API requests provide the token in the `Authorization: Bearer <token>` header. Controllers derive the caller's account identity from validated claims; role attributes, database-backed permission checks, and ownership filters enforce access.

The signing key must be at least 32 bytes. Production requires `Jwt__SigningKey` to be supplied through a secret store or environment configuration. Development can generate a process-local key when none is set, so tokens issued in that mode stop validating after an API restart. Tokens are access tokens; the current API does not provide a refresh-token endpoint.

## Candidate profile API

- `GET /api/admin/candidates` and `GET /api/admin/candidates/{id}` return candidate profile details, including active status and `inactiveAtUtc`; both require an Admin JWT.
- `POST /api/admin/candidates` creates a candidate with username, initial password, first name, last name, email, phone number, experience, performance score (0–100), and optional resume. Submit as `multipart/form-data`.
- `PUT /api/admin/candidates/{id}` updates profile fields, including email and active status, and optionally the resume. Username and password are not update fields. Setting `isActive` to `false` records `inactiveAtUtc`; reactivation clears the timestamp.
- `DELETE /api/admin/candidates/{id}` permanently deletes an inactive candidate and their profile. Active candidates receive `409 Conflict` and must first be deactivated through PUT.
- `GET /api/candidates/me` lets an authenticated candidate fetch only their own profile.
- `PUT /api/candidates/me/resume` lets an authenticated candidate upload/replace only their own resume (`multipart/form-data`, form field `Resume`).
- `PUT /api/candidates/me/password` lets a candidate change their own password by supplying `currentPassword` and `newPassword` as JSON. A candidate cannot change their username.

Resume uploads accept PDF or legacy DOC files up to 10 MB. The API validates the extension and file signature, stores resume bytes in SQL Server, and returns resume metadata only. Passwords and password hashes are never returned or included in candidate profile responses. Candidate management APIs require JWT authentication and the appropriate Admin or Candidate role.

## Profile photo API

- `GET /api/profile-photos/{employeeAccountId}` returns the account's profile photo, or `204 No Content` if no photo is saved.
- `PUT /api/profile-photos/{employeeAccountId}` accepts a JPEG in the `photo` multipart form field, up to 5 MB, and saves it to the account.

Both routes require a JWT. Admins may view and update any account photo; Employees and Candidates may access only their own. Photo bytes and content type are stored on the `EmployeeAccounts` row in SQL Server. Development applies the `AddProfilePhotos` migration at startup; Production requires the migration to be reviewed and applied using the documented migration process.

## Candidate work review and interview decision API

New candidate accounts receive a UTC `CreatedAtUtc`. The profile response includes `workSubmissionEndsAtUtc`, `canSubmitWork`, and the interview-decision stage. Candidates may create or update pending work only during the first 30 days after account creation. The API returns `409 Conflict` after the window ends; work stays available for viewing and review. Reviewed work cannot be edited or deleted.

| Method and route | Access | Purpose |
|---|---|---|
| `GET /api/candidates/me/work-tracking` | Candidate | List only the caller’s work, with `status`, `reviewComment`, submission/review timestamps, and reviewer. |
| `POST /api/candidates/me/work-tracking` | Candidate | Submit work during the 30-day window; new entries are `Pending`. |
| `PUT /api/candidates/me/work-tracking/{entryId}` | Candidate | Update a caller-owned pending entry during the window. |
| `DELETE /api/candidates/me/work-tracking/{entryId}` | Candidate | Delete a caller-owned pending entry. Reviewed entries return `409`. |
| `GET /api/admin/work-tracking` | Admin or Employee with `Reviews.Work` | Search by `candidateId`, `name`, `stack`, `dateFrom`, `dateTo`, and `status`. Employee results are restricted to their owned candidates. |
| `PUT /api/admin/work-tracking/review` | Admin or Employee with `Reviews.Work` | Atomically approve or reject multiple pending entries for one candidate. |
| `PUT /api/admin/candidates/{candidateId}/interview-decision` | Admin or Employee with `Reviews.InterviewDecision` | Set the separate interview decision after day 30; Employee ownership is enforced. |

Bulk review request:

```json
{
  "candidateId": 42,
  "entryIds": [101, 102],
  "status": "Rejected",
  "comment": "Please clarify the reported outcome."
}
```

`status` is `Approved` or `Rejected`; rejection requires a non-blank comment. The shared comment is limited to 2,000 characters. Reviewed entries cannot be selected again. Invalid input returns `400`, inaccessible candidates return `404`, and entries that are no longer pending return `409`.

Interview decision request:

```json
{
  "decision": "InterviewQualified",
  "comment": "Ready for the next stage."
}
```

Allowed decisions are `InterviewQualified` and `NotQualified`. Decisions start at `PendingReview` and can be set once, after 30 days. Early requests return `400`; duplicate decisions return `409`.

### Notifications and SignalR

Existing persisted endpoints remain authenticated and scoped to the JWT subject:

- `GET /api/notifications` lists the caller’s active notifications.
- `POST /api/notifications/read` marks the caller’s notifications read.
- `DELETE /api/notifications/{id}` dismisses only the caller’s notification.
- `/hubs/notifications` is an authenticated SignalR hub. It joins connections to a group derived from the validated JWT account ID and emits the `notification` event only to its intended recipient. The frontend refreshes from `GET /api/notifications` after reconnect.

Notifications are saved before SignalR delivery. Task decisions and interview decisions notify the candidate; task submissions notify active Admins and the candidate’s Employee owner, when present.

The `AddCandidateReviewStages` migration backfills existing account creation times to more than 30 days ago, maps graded work to `Approved` and ungraded work to `Pending`, and preserves legacy grade and review-history columns.

## Development startup

Start the server with its `https` or `http` launch profile in Visual Studio. The browser opens the React/Vite client at `https://localhost:62099`; Vite proxies `/api/` requests to the API. The HTTPS API listens at `https://localhost:7216` (and `http://localhost:5047`), while the HTTP profile listens at `http://localhost:5047`. Vite detects the ASP.NET launch URLs; when running Vite independently, set `VITE_API_TARGET` to the API origin, such as `https://localhost:7216`. API listen URLs belong in `Properties/launchSettings.json`, not `appsettings.json`.

## Development database and sample accounts

Development uses SQL Server LocalDB (`(localdb)\\mssqllocaldb`) and applies EF Core migrations when the API starts.

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `AdminDemo123!` | Admin |
| `candidate`, `candidate01`–`candidate09` | `CandidateDemo123!` | Candidate |

The Development seeder creates ten candidate accounts in total, including `candidate`, for the admin candidate-list screen. These credentials are for local development only; do not use them in deployed environments. The seeder runs only in Development, hashes passwords using ASP.NET Core's password hasher, and adds missing accounts without replacing existing ones.

## Configuration

Set `ConnectionStrings__DefaultConnection` to the SQL Server connection string for the target environment. Set `Jwt__SigningKey` to a random secret of at least 32 bytes (for example via .NET User Secrets locally or a deployment secret store); optionally configure `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__LifetimeMinutes`. Development CORS origins are isolated in `appsettings.Development.json`. In Production, set `CORS_ALLOWED_ORIGINS` to the exact frontend origin, or a comma-separated list of exact origins; do not include URL paths or a trailing slash. Production startup fails when the signing key is absent or too short. Development generates a temporary, process-local key when one is not configured, so development tokens stop validating after an API restart. Non-Development hosting listens on the `PORT` environment variable and falls back to port `10000` when it is absent; Development uses the API URLs in `Properties/launchSettings.json`.

## Render and Vercel deployment

The API Dockerfile is `server/Dockerfile`, built with the official `mcr.microsoft.com/dotnet/sdk:9.0` image and run with the official `mcr.microsoft.com/dotnet/aspnet:9.0` image. Create a Render Docker Web Service using `fullstack` as the root/build context and `server/Dockerfile` as the Dockerfile path. Render supplies `PORT`; the API listens on it. Render terminates public TLS, so the container serves HTTP.

Set these Render environment variables:

| Variable | Required value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Reachable SQL Server connection string |
| `Jwt__SigningKey` | Secret key containing at least 32 bytes |
| `CORS_ALLOWED_ORIGINS` | Exact Vercel origin, such as `https://your-project.vercel.app` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Optional JWT settings are `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__LifetimeMinutes`. Configure `VITE_API_BASE_URL` in the Vercel project to the public Render API origin, then redeploy the frontend. The React client uses relative API paths locally and prefixes them with this variable when configured. Production Swagger UI is disabled; Swagger remains available in Development.

### Azure SQL database for Render

The API uses EF Core's SQL Server provider (`UseSqlServer`), which is compatible with Azure SQL Database. Render does not need a database Dockerfile when using Azure SQL; only the API is containerized. Render Postgres is not compatible with the current provider.

1. Create an Azure SQL logical server and database in a region appropriate for the app. Configure a dedicated application SQL login/user with only the permissions needed by the API; do not use the server administrator login for normal application traffic. Store credentials in a secret manager, not in source control or appsettings files.
2. In Render, open the API service's **Connect → Outbound** details and copy its current outbound IP ranges for the service's region. In the Azure SQL server networking/firewall settings, allow only those Render egress ranges. If using a Render dedicated outbound IP feature, allowlist those fixed IPs instead and confirm the service uses them. Do not use the broad "Allow Azure services and resources to access this server" rule as a shortcut. Render's shared outbound ranges can change with service/region configuration; recheck and update the Azure SQL firewall rules if the Render service moves or its outbound ranges change.
3. Keep the Azure SQL public endpoint firewall-restricted. The demo remains public through the Vercel frontend and Render API; browser clients connect to the API, never directly to the database. Do not configure an Azure SQL Private Endpoint unless you have separately arranged private network connectivity from Render to that Azure virtual network; Render's outbound IP allowlisting is for the public endpoint.
4. In Render, set `ConnectionStrings__DefaultConnection` to the Azure SQL connection string as a secret environment variable. Use the Azure SQL server hostname, database name, dedicated application login, and password. Require encryption and certificate validation. The following is only a placeholder format; replace every placeholder in the Render dashboard and never commit real credentials:

   ```text
   Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<application-login>;Password=<secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

5. Set the other API variables (`Jwt__SigningKey`, `CORS_ALLOWED_ORIGINS`, and `ASPNETCORE_ENVIRONMENT=Production`) and Vercel's `VITE_API_BASE_URL` as described above. Verify the API can reach Azure SQL after adding the Render egress firewall rules.

Official references: [Render outbound IP addresses](https://render.com/docs/outbound-ip-addresses), [Azure SQL firewall rules](https://learn.microsoft.com/azure/azure-sql/database/firewall-configure), and [Azure SQL connectivity](https://learn.microsoft.com/azure/azure-sql/database/connect-query-content-reference-guide).

### Amazon RDS for SQL Server with Render

Amazon RDS for Microsoft SQL Server is also compatible with the API's existing EF Core SQL Server provider. No database Dockerfile or provider change is needed; only the API uses the existing Dockerfile. RDS is managed by AWS, unlike running a self-hosted database container.

1. Create an RDS for SQL Server DB instance, database, and a dedicated application login. Select a supported SQL Server edition/version and review AWS licensing and instance costs. Keep automated backups enabled and choose an appropriate retention period.
2. For the simplest Render-to-RDS setup, make the RDS instance publicly accessible but restrict its VPC security group's inbound rule for the selected SQL Server TCP port (typically `1433`) to only the Render service's outbound IP CIDRs. Get the current ranges from Render's **Connect → Outbound** panel. Render's shared ranges can change; update the security group if the service region/configuration changes. For fixed egress, consider Render dedicated outbound IPs if available. Do not allow `0.0.0.0/0` or expose the database to browsers. If the RDS instance must remain private, a network path such as a VPN between Render and the AWS VPC is required; ordinary public DNS/connection-string configuration alone cannot reach a private RDS endpoint.
3. Set `ConnectionStrings__DefaultConnection` in Render as a secret environment variable. Replace the placeholders below with the RDS endpoint, its configured port, database name, and dedicated application login credentials; never commit real values:

   ```text
   Server=tcp:<rds-endpoint>,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<application-login>;Password=<secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

4. Confirm the application runtime can validate the RDS server certificate when using `TrustServerCertificate=False`; follow AWS's current RDS CA guidance and ensure its CA chain is trusted by the runtime. AWS also supports requiring SSL for all connections by setting `rds.force_ssl` in a custom DB parameter group; check the engine/version support and reboot requirements before changing it. Keep encrypted connections enabled in the client connection string.
5. Keep the demo public only through Vercel and Render. The browser calls the API; only the API reaches the RDS endpoint. Configure the API's JWT and CORS settings as above, and test the connection from the deployed Render service.

Official references: [Amazon RDS for SQL Server](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_SQLServer.html), [RDS SQL Server connectivity](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_ConnectToMicrosoftSQLServerInstance.html), [RDS SQL Server SSL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/SQLServer.Concepts.General.SSL.Using.html), and [Render outbound IP addresses](https://render.com/docs/outbound-ip-addresses).

Do not place credentials in `appsettings.json` or `appsettings.Development.json` in the image. `appsettings.Development.json` and local `.env` files are excluded by `.dockerignore`.

### Temporary Docker + Cloudflare demo

From the `fullstack` directory, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy-demo.ps1`. If `.env` does not exist, the Windows helper creates it with cryptographically random values for `SQL_SA_PASSWORD`, `JWT_SIGNING_KEY`, `DEMO_ADMIN_PASSWORD`, and `DEMO_CANDIDATE_PASSWORD`; it never overwrites an existing `.env`. Do not commit or share `.env`. The helper starts the private SQL Server container, runs EF Core migrations through a one-shot migrator container, starts the Production API with explicitly enabled demo seeding, and creates a temporary Cloudflare Quick Tunnel. It captures the generated HTTPS origin and updates only `CORS_ALLOWED_ORIGINS` in `.env` before restarting the API. It prints the temporary public URL when ready; it never prints the generated secrets. The database is not published to the host. Demo credentials are randomly generated and stored only in `.env`; share them only with intended demo viewers.

The tunnel URL is temporary and changes when the tunnel restarts. Stop the demo with `docker compose --env-file .env down` from `fullstack`; this leaves the SQL data volume intact. Remove demo data as well only when you intentionally want to permanently delete the database volume, using `docker compose --env-file .env down --volumes`.

EF Core code-first migrations live under `Migrations`. Development applies committed migrations on startup; Production does not run migrations automatically. Before changing a production schema, review the migration operations and generate an idempotent SQL script with `dotnet ef migrations script --idempotent --project fullstack/server/fullstackapplication.Server.csproj`. Back up the database, verify its current migration state, and have an operator review and apply the SQL script manually. Use a migration credential with the required DDL permissions for this operation rather than granting those permissions to the runtime application login. Add future migrations from the server project with `dotnet ef migrations add <MigrationName>`.
