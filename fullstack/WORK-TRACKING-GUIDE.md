# Candidate Work Tracking: Frontend and ASP.NET Core API

This feature follows the existing application layout: React components use Material UI and `apiRequest`, while the ASP.NET Core API uses role-protected controllers, DTOs, services, repositories, and Entity Framework Core.

## Frontend

### Candidate experience

The candidate portal now has two MUI tab menus:

- **Profile** preserves the existing account information, password change, and resume upload. Candidates can enter and save a technology stack (up to 200 characters); the saved value is returned by the existing profile endpoint.
- **Work tracking** displays submitted entries in a responsive MUI table. The Add work entry form contains date, task, hours spent, accomplishment, learnings, blockers/challenges, and AI tools used. Blockers/challenges is required and limited to 2,000 characters, like accomplishment and learnings. Selecting a row with its radio control loads that entry into the form; editing a selected entry resubmits it for review.

### Admin experience

The admin portal has **Candidate directory** and **Work reviews** tabs. The directory uses responsive candidate cards, text search, and a selected-candidate details panel; it includes each candidate's stack and supports editing the stack when creating or updating profiles. Work reviews provides:

- Radio selection for all candidates or one candidate.
- Name, stack, date range, and exact-grade filters, passed to the API.
- A selectable task table showing candidate, stack, date, work details, and existing grade.
- A review form with a 0–100 grade dropdown and optional feedback.

Task grades are stored on individual work entries. They do not silently overwrite the existing profile performance score.

### Frontend files

- `fullstack/client/src/CandidatePortal.jsx` — candidate navigation, profile, and stack editing.
- `fullstack/client/src/WorkTracking.jsx` — candidate entry list, create/edit form, and review status.
- `fullstack/client/src/AdminDashboard.jsx` — admin tabs and candidate directory.
- `fullstack/client/src/WorkReviews.jsx` — filters, candidate selection, task review, and grading.
- `fullstack/client/src/api.js` — shared authenticated HTTP requests and API error handling.
- `fullstack/client/src/App.css`, `src/index.css` — responsive navy SaaS layout, full-width workspace, candidate cards, task-entry grid, and review-filter spacing.

All service calls use the signed-in access token. Loading, saving, success, and request-error states are surfaced in the UI.

## ASP.NET Core Web API

### Data model

`CandidateProfile.Stack` stores the candidate's technology stack. `CandidateWorkEntry` is a many-to-one child of `CandidateProfile` and stores:

| Field | Meaning |
| --- | --- |
| `WorkDate` | Date the work was performed (`DateOnly`). |
| `Task` | Short work-item description (up to 200 characters). |
| `HoursSpent` | Decimal hours, from 0.1 through 24. |
| `Accomplishment` | Outcome or work completed (up to 2,000 characters). |
| `Learnings` | Learning notes (up to 2,000 characters). |
| `BlockersAndChallenges` | Blockers or challenges encountered (up to 2,000 characters). |
| `AiToolsUsed` | AI tools used (up to 500 characters; may be empty). |
| `Grade` | Optional admin score from 0 through 100. |
| `AdminFeedback` | Optional reviewer notes (up to 2,000 characters). |
| `SubmittedAtUtc` / `ReviewedAtUtc` | Submission and review timestamps. |

Deleting a candidate profile cascades to its work entries. The database index on candidate and work date supports the candidate's date-ordered view.

### Candidate endpoints

All routes below require a JWT with the `Candidate` role. Candidate identity is read from the validated JWT subject; the API does not accept a candidate ID from the caller for self-service writes.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/candidates/me` | Read profile, including `stack`. |
| `PUT` | `/api/candidates/me/stack` | Update the caller's stack. JSON body: `{ "stack": "React, .NET, SQL Server" }`. |
| `GET` | `/api/candidates/me/work-tracking` | List the caller's work entries, newest date first. |
| `POST` | `/api/candidates/me/work-tracking` | Submit a work entry. |
| `PUT` | `/api/candidates/me/work-tracking/{entryId}` | Edit and resubmit one of the caller's entries. Another candidate's ID returns `404`. |
| `DELETE` | `/api/candidates/me/work-tracking/{entryId}` | Delete one of the caller's unreviewed entries. Another candidate's ID returns `404`; an entry with a grade returns `409 Conflict`. |

Create/update JSON shape:

```json
{
  "workDate": "2026-11-02",
  "task": "Implement candidate work tracking",
  "hoursSpent": 3.5,
  "accomplishment": "Added the entry table and save flow",
  "learnings": "Practiced the existing API and MUI patterns",
  "blockersAndChallenges": "No blockers encountered",
  "aiToolsUsed": "GitHub Copilot"
}
```

Editing a previously graded entry clears its grade, feedback, and review timestamp and updates the submission timestamp so the modified work is clearly awaiting another review.

### Admin endpoints

Admin routes require a JWT with the `Admin` role.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/admin/candidates` | List candidate profiles, including stack, for directory and candidate selection. |
| `GET` | `/api/admin/work-tracking` | Search entries using optional query parameters: `candidateId`, `name`, `stack`, `dateFrom`, `dateTo`, and `grade`. |
| `PUT` | `/api/admin/work-tracking/{entryId}/review` | Set a grade and optional feedback for a work entry. |

Example filter:

```text
GET /api/admin/work-tracking?name=Alex&stack=React&dateFrom=2026-10-01&dateTo=2026-11-02&grade=90
```

Review JSON body:

```json
{
  "grade": 90,
  "adminFeedback": "Clear delivery and useful learning notes."
}
```

A reversed date range and grade outside 0–100 produce `400 Bad Request`. Missing entries return `404 Not Found`. Role restrictions are enforced by ASP.NET Core authorization attributes, not by the frontend menus.

### Backend files

- `fullstack/server/Models/CandidateProfile.cs`, `Models/CandidateWorkEntry.cs` — EF Core entities.
- `fullstack/server/Data/ApplicationDbContext.cs` — table mapping, field lengths, relationship, index, and cascade behavior.
- `fullstack/server/Dtos/CandidateApiDtos.cs`, `Dtos/WorkTrackingDtos.cs` — validated input and API response shapes.
- `fullstack/server/Controllers/CandidateController.cs` — authenticated self-service profile and work-entry routes.
- `fullstack/server/Controllers/AdminWorkTrackingController.cs` — admin-only search and review routes.
- `fullstack/server/Services/IWorkTrackingService.cs`, `Services/WorkTrackingService.cs` — service contract, submission, ownership-scoped edits, filtering delegation, and review behavior.
- `fullstack/server/Repositories/IWorkTrackingRepository.cs`, `Repositories/WorkTrackingRepository.cs` — EF Core persistence and filtering.
- `fullstack/server/Program.cs` — dependency-injection registrations.
- `fullstack/server/Migrations/` — database schema migrations for candidate profile stack, work tracking, and the blockers/challenges column.

## Local verification

From `fullstack/client`, run `npm run lint` and `npm run build`. Build the API with `dotnet build fullstack/server/fullstackapplication.Server.csproj`, and run API integration tests with `dotnet test fullstack/server.Tests/fullstackapplication.Server.Tests.csproj`. Apply the generated migration to the configured database before using the new endpoints against a persistent SQL Server database; application startup already applies pending migrations.
