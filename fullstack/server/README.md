# Employee authentication API

## Application layers

The candidate admin, candidate self-service, and employee-directory controllers handle HTTP binding, authorization, and response mapping. Request and response DTOs are in `Dtos`; business rules and orchestration are in `Services`; EF Core queries and persistence are in `Repositories`, backed by `Data\ApplicationDbContext`. Repositories return materialized entities or explicit results rather than exposing `IQueryable` to controllers or services.

## Endpoints

In Development, run the API and open `/swagger` (for example, `https://localhost:7216/swagger`) to explore and call the endpoints interactively. Use `POST /api/auth/login` with the demo credentials, copy `accessToken`, select **Authorize**, and enter `Bearer <accessToken>` to call the protected list endpoints. Swagger UI and its generated Swagger document are not enabled outside Development. The existing OpenAPI JSON endpoint remains at `/openapi/v1.json`.

- `POST /api/auth/login` accepts `{"username":"admin","password":"AdminDemo123!"}` and returns a bearer token plus a safe user summary.
- `GET /api/admins` returns active Admin account summaries.
- `GET /api/candidates` returns active Candidate account summaries.

The list endpoints require an Admin JWT. Candidate accounts can authenticate, but receive `403 Forbidden` when requesting either list. Password hashes are never included in API responses.

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

Set `ConnectionStrings__DefaultConnection` to the SQL Server connection string for the target environment. Set `Jwt__SigningKey` to a random secret of at least 32 bytes (for example via .NET User Secrets locally or a deployment secret store); optionally configure `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__LifetimeMinutes`. Configure `Cors__AllowedOrigins__0` (and additional indexed entries as needed) with the exact frontend origin for each deployed environment; the API does not allow arbitrary origins. The checked-in allowlist includes the local Vite development origins. Production startup fails when the signing key is absent or too short. Development generates a temporary, process-local key when one is not configured, so development tokens stop validating after an API restart.

EF Core code-first migrations live under `Migrations`. Add future migrations from the server project with `dotnet ef migrations add <MigrationName>` and apply them using `dotnet ef database update`; application startup also applies committed migrations.
