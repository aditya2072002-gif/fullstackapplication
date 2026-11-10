# Fullstack Application

A full-stack work-tracking portal for managing employee and candidate workflows. Admins and employees can manage candidates and review submitted work; candidates can manage their profiles and submit work entries. The application uses role-based access and stores application data in SQL Server.

## Features

- **Role-based portals** for Admins, Employees/mentors, and Candidates.
- **Candidate management**, including profile lifecycle and resume upload/download.
- **Candidate work tracking** with a 30-day submission window and Pending, Approved, or Rejected review stages; reviewers can decide multiple pending tasks with one shared comment.
- **Interview qualification** as a separate candidate-level decision after the 30-day submission window.
- **Persisted, real-time notifications** with read and dismiss actions, delivered to the authenticated recipient through SignalR.
- **Review dashboards and status charts** for task and interview stages, scoped to employee-owned candidates or the admin-wide directory.
- **JWT authentication** and API-side authorization, candidate ownership, submission-window, and review-lock checks; the client keeps the token tab-scoped so reload restores the current page, while Back to login clears the session.
- **Role-scoped floating chat** and authenticated read-only MCP lookup tools for employee, candidate, active/inactive, and work-review details.
- **Responsive React interface** with light/dark theme support.

## Technology

- **Frontend:** React, Vite, Material UI, Formik, Emotion, and the SignalR JavaScript client.
- **Backend:** ASP.NET Core Web API on .NET 9, C#, Entity Framework Core, and the .NET MCP C# SDK.
- **Database:** SQL Server; schema managed through EF Core migrations.
- **Tests and quality:** xUnit integration tests and ESLint.

## Repository layout

```text
fullstack/
├── client/       React application
├── server/       ASP.NET Core API and database layer
├── server.Tests/ API integration tests
├── docs/         Project documentation
└── WORK-TRACKING-GUIDE.md
```

## Run locally

### Prerequisites

- .NET 9 SDK
- Node.js and npm
- SQL Server (the Development configuration is set up for SQL Server LocalDB on Windows)

### Start the application

1. Clone the repository and open `fullstackapplication.slnx` in Visual Studio, or use a terminal from the repository root.
2. Install the frontend dependencies:

   ```powershell
   Set-Location .\fullstack\client
   npm ci
   Set-Location ..\..
   ```

3. Configure the API's `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey` (at least 32 bytes outside Development), and `Cors:AllowedOrigins` for your environment. Keep real secrets out of source control. The checked-in Development configuration uses LocalDB.
4. Start the API in Development:

   ```powershell
   dotnet run --project .\fullstack\server\fullstackapplication.Server.csproj
   ```

   The development setup launches the Vite client through ASP.NET Core's SPA proxy. The API's configured HTTPS URL is `https://localhost:7216`; Swagger UI is available at `/swagger` in Development. If running the client separately, use `npm run dev` from `fullstack/client`; Vite proxies `/api` to the API, configurable with `VITE_API_TARGET`.

The API applies EF Core migrations at startup. Development demo accounts are documented in the [server README](fullstack/server/README.md); they are for local use only.

## Verification

Run the backend integration tests from the repository root:

```powershell
dotnet test .\fullstack\server.Tests\fullstackapplication.Server.Tests.csproj
```

Run the frontend lint and production build:

```powershell
Set-Location .\fullstack\client
npm run lint
npm run build
```

## Documentation

- [Full project guide](fullstack/docs/PROJECT-GUIDE.md) — architecture, API endpoints, persistence, authentication, setup, and testing.
- [Client project flow](fullstack/client/README.md) — frontend structure and request flow.
- [Server README](fullstack/server/README.md) — API configuration, endpoints, and local development details.
- [Work tracking guide](fullstack/WORK-TRACKING-GUIDE.md) — submission-window rules, multi-task review, interview qualification, notifications, and end-to-end behavior.

The [server README](fullstack/server/README.md) documents the work-review and interview-decision endpoints. The [client README](fullstack/client/README.md) documents the candidate and reviewer portal implementation.

## Development workflow tools

GitHub Copilot's **Ask Agent** and **Plan mode** were used as AI-assisted tools during project planning and documentation. They assist development and are not runtime components of the application.
