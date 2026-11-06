# Fullstack Application — ASP.NET Core API

## Server folder structure

```text
server/
├── Controllers/       API routes, authorization, and HTTP response mapping
├── Data/              EF Core DbContext and development data seeding
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

These routes require an Admin JWT. An Admin can list employee accounts and create a new Employee with a username, password, display name, department, and stack. Passwords are hashed before persistence; the response contains an employee summary and never includes a password or hash.

| Method and route | Purpose |
|---|---|
| `GET /api/admin/employees` | List employee accounts. |
| `POST /api/admin/employees` | Create an Employee account; returns `201`, `400` for invalid input, or `409` if the username is already in use. |

### Employee candidate management

These routes require an Employee JWT. Employees can create candidates and list, view, update, or change the status of only candidates they created. Candidate ownership is checked by the API and repository; attempts to access another Employee's candidate return `404`.

| Method and route | Purpose |
|---|---|
| `GET /api/employee/candidates` | List candidates created by the signed-in Employee. |
| `GET /api/employee/candidates/{candidateId}` | Get an owned candidate. |
| `POST /api/employee/candidates` | Create a Candidate assigned to the signed-in Employee. |
| `PUT /api/employee/candidates/{candidateId}` | Update an owned candidate's profile. |
| `PATCH /api/employee/candidates/{candidateId}/status` | Activate or deactivate an owned candidate. |

Admins manage the broader candidate directory through `/api/admin/candidates`. Admins and Employees can use `/api/admin/work-tracking` to search and review work; Employee results and review actions are restricted to candidates they created.

### JWT authentication implementation

1. `Controllers/AuthController.cs` exposes the public `POST /api/auth/login` endpoint.
2. `Services/AuthService.cs` finds an active account, verifies its password hash, and delegates token creation after successful authentication.
3. `Services/JwtTokenService.cs` signs a JWT with HMAC-SHA256. The token includes the account ID (`sub`), username (`unique_name`), role (`role`), issuer, audience, and expiry.
4. `Program.cs` configures JWT bearer authentication to validate the issuer, audience, signing key, and token lifetime, then enables role-based authorization for controllers.
5. Authenticated API requests provide the token in the `Authorization: Bearer <token>` header. Controllers derive the caller's account identity from validated claims; role attributes and ownership filters enforce access.

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

## Development startup

Start the server with its `https` launch profile in Visual Studio. The API listens at `https://localhost:7216`, while the React/Vite client opens at `https://localhost:62099` and proxies `/api` requests to the API. The corresponding `http` profile listens at `http://localhost:5047`. Vite detects the ASP.NET launch URLs; when running Vite independently, set `VITE_API_TARGET` to the API origin, such as `https://localhost:7216`. API listen URLs belong in `Properties/launchSettings.json`, not `appsettings.json`.

## Development database and sample accounts

Development uses SQL Server LocalDB (`(localdb)\\mssqllocaldb`) and applies EF Core migrations when the API starts.

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `AdminDemo123!` | Admin |
| `candidate`, `candidate01`–`candidate09` | `CandidateDemo123!` | Candidate |

The Development seeder creates ten candidate accounts in total, including `candidate`, for the admin candidate-list screen. These credentials are for local development only; do not use them in deployed environments. The seeder runs only in Development, hashes passwords using ASP.NET Core's password hasher, and adds missing accounts without replacing existing ones.

## Configuration

Set `ConnectionStrings__DefaultConnection` to the SQL Server connection string for the target environment. Set `Jwt__SigningKey` to a random secret of at least 32 bytes (for example via .NET User Secrets locally or a deployment secret store); optionally configure `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__LifetimeMinutes`. In Production, set `CORS_ALLOWED_ORIGINS` to the exact frontend origin, or a comma-separated list of exact origins; do not include URL paths or a trailing slash. Development uses the `Cors:AllowedOrigins` list in `appsettings.json`. Production startup fails when the signing key is absent or too short. Development generates a temporary, process-local key when one is not configured, so development tokens stop validating after an API restart. The server listens on the `PORT` environment variable and falls back to port `10000` when it is absent.

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

Do not place credentials in `appsettings.json` or `appsettings.Development.json` in the image. `appsettings.Development.json` is excluded by `.dockerignore`.

EF Core code-first migrations live under `Migrations`. Development applies committed migrations on startup; Production does not run migrations automatically. Before changing a production schema, review the migration operations and generate an idempotent SQL script with `dotnet ef migrations script --idempotent --project fullstack/server/fullstackapplication.Server.csproj`. Back up the database, verify its current migration state, and have an operator review and apply the SQL script manually. Use a migration credential with the required DDL permissions for this operation rather than granting those permissions to the runtime application login. Add future migrations from the server project with `dotnet ef migrations add <MigrationName>`.
