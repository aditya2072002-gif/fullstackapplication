# Full-Stack Project Guide

This guide describes the current implementation in `fullstack/client` and `fullstack/server`: the React user experience, frontend libraries, API routes, backend architecture, JWT authentication, and available tests and development commands. It is intended as a source-based guide; it does not describe features that are not currently implemented.

## Contents

- [Solution layout](#solution-layout)
- [Frontend](#frontend)
- [Frontend libraries and tools](#frontend-libraries-and-tools)
- [Backend architecture](#backend-architecture)
- [API endpoints](#api-endpoints)
- [JWT authentication and token lifecycle](#jwt-authentication-and-token-lifecycle)
- [Data and persistence](#data-and-persistence)
- [Testing and verification](#testing-and-verification)
- [Local development and configuration](#local-development-and-configuration)
- [Related documentation](#related-documentation)

## Solution layout

| Path | Responsibility |
|---|---|
| `fullstack/client` | React single-page application, served by Vite during client development and built for production. |
| `fullstack/server` | ASP.NET Core Web API, authentication, business logic, and SQL Server persistence via Entity Framework Core. |
| `fullstack/server.Tests` | xUnit API integration tests using ASP.NET Core's test host. |
| `fullstack/docs` | Project documentation. |

At runtime the browser calls the API over HTTP(S). The client does not connect directly to SQL Server. A typical request travels through a React component, `src/api.js`, an authorized API controller, a service, a repository, EF Core, and the database; the response then updates React state and the UI.

## Frontend

### Startup and application flow

`client/index.html` contains the root DOM element. `src/main.jsx` mounts `App` using React DOM and wraps it in `StrictMode`. `src/App.jsx` initially renders the sign-in form. After `POST /api/auth/login` succeeds, it keeps the returned session in React state and selects the portal according to `session.user.role`.

This is a single-page application: Admin, Employee, and Candidate screens are React views rendered into the same root, not separate server-rendered pages or URL routes.

### Portals and main screens

| Role | Main component | Sections and features |
|---|---|---|
| Admin | `src/AdminDashboard.jsx` | Candidate directory and profile management; candidate resume download; work reviews; employee account management. |
| Employee / mentor | `src/EmployeePortal.jsx` | Lists and manages candidates created by that employee; reviews work entries within the employee's permitted scope. |
| Candidate | `src/CandidatePortal.jsx` | Own profile and technology stack; resume upload/update; password change; work tracking. |

`src/PortalShell.jsx` provides the shared portal header, role-specific section navigation, notifications, profile menu, dark-mode control, and logout action. `src/NotificationsInbox.jsx` displays the signed-in user's notifications. `src/WorkTracking.jsx` handles candidate work entries, and `src/WorkReviews.jsx` provides work-entry filtering, review, grading, and feedback. `src/EmployeeCandidates.jsx` and `src/EmployeeManagement.jsx` implement employee and mentor-management screens. `src/WorkReportChart.jsx` and `src/PortalIcon.jsx` are reusable presentation components.

### Session, themes, and API calls

- The login response is stored in the `session` state in `App.jsx`. Role-specific pages receive that session through props.
- Logout sets the in-memory session to `null`, so the user returns to the sign-in screen. Reloading the browser also clears the session and requires another login.
- The access token is **not** saved in `localStorage` or `sessionStorage`. The only current `localStorage` use is the `portal-dark-mode` display preference in `App.jsx`.
- Authenticated API calls pass `session.accessToken` into `apiRequest` in `src/api.js`. That helper sets the `Authorization: Bearer <token>` header, calls `fetch`, parses JSON or a blob response, handles `204 No Content`, and raises `ApiError` for non-success HTTP responses.
- `src/api/notifications.js` wraps the shared helper for notification list/read/dismiss requests.
- Components use React state and effects for interaction and data loading. Several data-loading effects use `AbortController` to cancel outstanding requests when a view unmounts. The notification inbox refreshes periodically while mounted.
- The theme is created with MUI's `createTheme`; the dark-mode preference is persisted as a string in local storage. Light/dark styling is applied through the MUI theme and application CSS.

### Frontend source map

| File | Responsibility |
|---|---|
| `client/src/main.jsx` | React entry point, global stylesheet import, `StrictMode`, and root mounting. |
| `client/src/App.jsx` | Login form, session and role selection, logout, MUI theme, persisted dark-mode preference. |
| `client/src/api.js` | Shared fetch helper, bearer header, response parsing, and `ApiError`. |
| `client/src/api/notifications.js` | Notification-specific API wrappers. |
| `client/src/AdminDashboard.jsx` | Admin directory, candidate create/update/status/delete, resume download, section selection. |
| `client/src/EmployeePortal.jsx` | Employee navigation and profile dialog. |
| `client/src/EmployeeCandidates.jsx` | Employee-owned candidate list, create/edit, and active-status changes. |
| `client/src/EmployeeManagement.jsx` | Admin employee account list and account creation. |
| `client/src/CandidatePortal.jsx` | Candidate profile, stack update, resume upload, and password change. |
| `client/src/WorkTracking.jsx` | Candidate work-entry create, edit, delete, listing, and chart. |
| `client/src/WorkReviews.jsx` | Employee/admin work-entry search and review. |
| `client/src/NotificationsInbox.jsx` | Current user's notifications, unread count, read and dismiss actions. |
| `client/src/PortalShell.jsx` | Shared portal structure and navigation. |
| `client/src/PortalIcon.jsx`, `client/src/WorkReportChart.jsx` | Reusable icons and work-report visualization. |
| `client/src/index.css`, `client/src/App.css` | Global and application-specific styles. |
| `client/vite.config.js` | Vite React plugin, development HTTPS, and API proxy. |
| `client/package.json` | Frontend dependencies and npm scripts. |

## Frontend libraries and tools

Direct dependencies and scripts are declared in `client/package.json`; the lockfile resolves exact package versions.

| Package/tool | How the project uses it |
|---|---|
| React and React DOM | Component-based UI, state/effect hooks, and browser rendering. |
| Material UI (`@mui/material`) | UI components, dialogs, tables, forms, alerts, icons/layout primitives, and theme support. |
| Emotion (`@emotion/react`, `@emotion/styled`) | Styling engine and styled-component integration used by Material UI. |
| Formik | Form values, validation, and submission for forms including profile/work/review/resume workflows. |
| Vite and `@vitejs/plugin-react` | Local development server, React integration, and production bundling. Vite also proxies `/api` requests to the ASP.NET Core server during development. |
| ESLint and its configured plugins | JavaScript linting, including React Hooks and Fast Refresh rules. |
| Native browser APIs | `fetch`, `FormData`, `AbortController`, `URLSearchParams`, and object URLs are used for HTTP, uploads, request cancellation, query parameters, and resume downloads. |

Client scripts, run from `fullstack/client`:

| Command | Purpose |
|---|---|
| `npm run dev` | Start Vite for client development. |
| `npm run build` | Create the production client build. |
| `npm run preview` | Preview the production build locally. |
| `npm run lint` | Run ESLint on the client code. |

## Backend architecture

The server targets .NET 9 and uses ASP.NET Core controllers. Responsibilities are separated by layer:

1. **Controllers (`server/Controllers`)** bind HTTP input, enforce authentication/role attributes, call a service, and map results to HTTP status codes and response bodies.
2. **DTOs (`server/Dtos`)** define request and response shapes, including validation attributes where applicable. API responses use DTOs rather than returning password hashes or persistence entities indiscriminately.
3. **Services (`server/Services`)** implement application operations and business rules. `ServiceResult` communicates success, invalid input, not-found, and conflict conditions to controllers.
4. **Repositories (`server/Repositories`)** own EF Core query and persistence operations. Interfaces separate service logic from storage implementation.
5. **Data and models (`server/Data`, `server/Models`)** define the EF Core context and persisted entities. `ApplicationDbContext` configures field lengths, indexes, and entity relationships.
6. **Migrations (`server/Migrations`)** track database schema changes.

`server/Program.cs` loads and validates database, JWT, and CORS configuration; registers the DbContext, repositories, services, password hasher, and token service; configures bearer authentication and authorization; applies database migrations at startup; and maps controllers. Swagger UI and OpenAPI development tooling are enabled only in Development. The Vite proxy forwards browser `/api` requests to the API while developing.

### Controller map

| Controller | Route base | Responsibility |
|---|---|---|
| `AuthController` | `/api/auth` | Public login and token issuance. |
| `AdminController` | `/api/admin/candidates` | Admin candidate directory, profile lifecycle, and resume download. |
| `AdminEmployeesController` | `/api/admin/employees` | Admin employee account listing and creation. |
| `AdminWorkTrackingController` | `/api/admin/work-tracking` | Admin/employee work-entry search and review, with role-appropriate scope. |
| `CandidateController` | `/api/candidates/me` | Candidate's own profile, stack, work entries, resume, and password. |
| `EmployeeCandidatesController` | `/api/employee/candidates` | Employee-owned candidate list and management. |
| `EmployeeDirectoryController` | `/api` | Admin-only account summaries. |
| `NotificationsController` | `/api/notifications` | Authenticated user's notifications. |

## API endpoints

Unless noted otherwise, request bodies that contain structured data are JSON. Candidate resume forms and Admin candidate create/update forms use `multipart/form-data`. `401 Unauthorized` generally means a request is missing a valid bearer token; `403 Forbidden` means the authenticated identity lacks the required access. Individual endpoints may also return `400`, `404`, or `409` as listed below.

### Authentication

| Method and route | Access | Purpose and behavior |
|---|---|---|
| `POST /api/auth/login` | Public | Accepts username and password. On success returns the access token, token type, expiry, and safe user summary; invalid credentials return `401`. |

### Admin candidate management

All routes below require the `Admin` role.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/admin/candidates` | List candidate profiles. |
| `GET /api/admin/candidates/{id}` | Get a candidate profile; `404` if it does not exist. |
| `GET /api/admin/candidates/{id}/resume` | Download the candidate's stored resume; `404` if unavailable/not found. |
| `POST /api/admin/candidates` | Create a candidate using multipart form data; may return `400` for invalid input or `409` for a conflict. |
| `PUT /api/admin/candidates/{id}` | Update a candidate profile using multipart form data; may return `400` or `404`. |
| `PATCH /api/admin/candidates/{id}/status` | Change candidate active status. |
| `DELETE /api/admin/candidates/{id}` | Permanently delete an inactive candidate; `404` if missing and `409` if deletion is not permitted (for example, while active). |

### Admin employee management and account directory

All routes below require the `Admin` role.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/admin/employees` | List employee accounts. |
| `POST /api/admin/employees` | Create an employee account; may return `400` for invalid input or `409` for a conflict. |
| `GET /api/admins` | List active Admin account summaries. |
| `GET /api/candidates` | List active Candidate account summaries. |

### Employee / mentor candidate management

All routes below require the `Employee` role. Candidate operations are scoped to candidates created/owned by that employee; the API does not rely on the client-side menu for access control.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/employee/candidates` | List candidates assigned to the signed-in employee. |
| `GET /api/employee/candidates/{candidateId}` | Get one owned candidate; `404` if not found or outside the employee's scope. |
| `POST /api/employee/candidates` | Create a candidate assigned to the signed-in employee; may return `400` or `409`. |
| `PUT /api/employee/candidates/{candidateId}` | Update an owned candidate; may return `400` or `404`. |
| `PATCH /api/employee/candidates/{candidateId}/status` | Activate or deactivate an owned candidate. |

### Candidate self-service

All routes below require the `Candidate` role. The API derives the candidate identity from the validated token, rather than accepting an arbitrary candidate account ID for self-service requests.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/candidates/me` | Read the signed-in candidate's profile. |
| `PUT /api/candidates/me/stack` | Update the candidate's technology stack. |
| `GET /api/candidates/me/work-tracking` | List the candidate's work entries. |
| `POST /api/candidates/me/work-tracking` | Submit a work entry. |
| `PUT /api/candidates/me/work-tracking/{entryId}` | Update and resubmit the candidate's own entry; `404` if not owned/found and `409` if the entry cannot be changed. |
| `DELETE /api/candidates/me/work-tracking/{entryId}` | Delete an eligible own work entry; `404` if not owned/found and `409` if deletion is not allowed. |
| `PUT /api/candidates/me/resume` | Upload or replace the candidate's resume using multipart form data. The server validates the upload and enforces a 10 MB request limit. |
| `PUT /api/candidates/me/password` | Change the signed-in candidate's password. |

### Work-entry search and review

These routes require either the `Admin` or `Employee` role. Admins can search across candidates; an employee's results/actions are constrained by their employee account scope.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/admin/work-tracking` | Search work entries. Optional query parameters: `candidateId`, `name`, `stack`, `dateFrom`, `dateTo`, and `grade`. Invalid date order or a grade outside 0-100 returns `400`. |
| `PUT /api/admin/work-tracking/{entryId}/review` | Record a grade and feedback for an entry. Can return `400`, `404`, or `409` when input/entry/review state disallows the operation. |

### Notifications

All routes below require an authenticated user. Each request is scoped to the account in that user's validated token.

| Method and route | Purpose and behavior |
|---|---|
| `GET /api/notifications` | List notifications for the signed-in user. |
| `POST /api/notifications/read` | Mark the signed-in user's notifications as read; returns `204 No Content`. |
| `DELETE /api/notifications/{id}` | Dismiss one of the signed-in user's notifications; returns `204` or `404`. |

## JWT authentication and token lifecycle

1. The client sends username and password to the public `POST /api/auth/login` endpoint.
2. `AuthService` verifies the account credentials with ASP.NET Core's password hasher and asks `JwtTokenService` to create an access token.
3. `JwtTokenService` creates a signed JWT containing the account subject (`sub`), username (`unique_name`), and role (`role`), plus configured issuer, audience, and expiration.
4. The login response contains `accessToken`, `tokenType` (`Bearer`), `expiresAtUtc`, and a safe user summary.
5. The client keeps that response in React memory. `apiRequest` adds `Authorization: Bearer <accessToken>` to authenticated calls.
6. ASP.NET Core JWT bearer middleware validates signature, issuer, audience, and lifetime. `Program.cs` maps the `role` claim for role authorization; `[Authorize]` and `[Authorize(Roles = ...)]` restrict controller access. Controllers that need account ownership obtain the account ID from the validated subject claim.
7. On logout or browser reload, the in-memory token is discarded. There is no refresh-token endpoint or persistent token storage in the current client implementation; users sign in again after reload or expiry.

`Jwt:SigningKey` must be at least 32 bytes. Production startup requires it to be configured through a secret store/environment configuration. Development can generate a temporary process-local key when none is configured, so development-issued tokens stop validating after the API restarts. `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:LifetimeMinutes` can also be configured; issuer/audience have defaults, and lifetime defaults to 60 minutes and must be positive. See `server/Program.cs` and `server/Services/JwtTokenService.cs` for the implementation.

## Data and persistence

`ApplicationDbContext` exposes EF Core sets for employee accounts, candidate profiles, candidate work entries, and user notifications. The model includes account/profile and profile/work-entry relationships, unique normalized usernames, query indexes, validation-oriented maximum lengths, and resume content stored as SQL Server binary data. Resume files are validated by `CandidateResumeUploadValidator`; the API stores file bytes and returns metadata in profile responses rather than returning the file contents as ordinary profile data.

The server uses SQL Server. During startup, EF Core applies committed migrations when the configured database provider is relational. In Development, `DevelopmentDataSeeder` may create demo accounts and sample data. Seeded credentials are for local development only and must not be used in deployed environments.

## Testing and verification

### Backend integration tests

`fullstack/server.Tests/AuthenticationApiTests.cs` contains xUnit integration tests hosted with `WebApplicationFactory<Program>`. The test host uses an isolated EF Core in-memory database and test JWT configuration. Existing tests cover:

- Login response/token behavior, safe account summaries, Swagger bearer documentation, and role-based account-list access.
- Admin candidate profile operations and candidate self-service restrictions, including resume validation and password behavior.
- Candidate work submission, ownership checks, admin filtering/review, and prevention of changes to reviewed entries.
- Employee mentor candidate ownership, employee/admin access boundaries, review behavior, and related notifications.

From the repository root, run:

```powershell
dotnet test .\fullstack\server.Tests\fullstackapplication.Server.Tests.csproj
```

### Frontend checks

`client/package.json` defines lint, build, dev, and preview scripts. It does **not** currently define an automated frontend test script or declare a frontend test framework. To run the available static checks and production build:

```powershell
Set-Location .\fullstack\client
npm run lint
npm run build
```

## Local development and configuration

The projects can be launched together through the Visual Studio solution/startup configuration. The documented local HTTPS setup uses the ASP.NET Core API at `https://localhost:7216` and the Vite client at `https://localhost:62099`; the HTTP API profile uses `http://localhost:5047`. Vite proxies `/api` requests to the backend. When running Vite separately, `VITE_API_TARGET` can specify the API origin.

The server requires a connection string (`ConnectionStrings:DefaultConnection`), a suitable `Jwt:SigningKey`, and at least one exact frontend origin in `Cors:AllowedOrigins`. In environment variables, ASP.NET Core configuration uses double underscores, for example `ConnectionStrings__DefaultConnection` and `Cors__AllowedOrigins__0`. Local development uses SQL Server LocalDB according to the server's launch/configuration setup.

In Development, Swagger UI is available at `/swagger` and can be used to explore endpoints. Its bearer security definition supports testing protected API calls with a token obtained from login. Avoid placing production secrets or real credentials in source-controlled configuration.

## Related documentation

- [Client flow and implementation](../client/README.md) — component flow, role-based employee/candidate workflows, client files, package descriptions, and scripts.
- [Server API README](../server/README.md) — server configuration, development startup, and existing API notes.
- [Work tracking guide](../WORK-TRACKING-GUIDE.md) — detailed work-tracking behavior, data fields, endpoints, and local verification.
