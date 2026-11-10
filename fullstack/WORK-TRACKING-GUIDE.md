# Candidate Work Tracking: Frontend and ASP.NET Core API

This feature follows the existing application layout: React components use Material UI and `apiRequest`, while the ASP.NET Core API uses role-protected controllers, DTOs, services, repositories, and Entity Framework Core.

## Frontend

### Candidate experience

The candidate portal includes profile and work-tracking views:

- **Profile** preserves account details, password change, resume upload, and technology-stack editing. It also shows interview qualification, any interview decision comment, and the work-submission deadline.
- **Work tracking** displays submitted entries and their Pending, Approved, or Rejected stages, reviewer comments, and reviewer identity. A donut summarizes Approved, Rejected, Pending, Reviewed, and Not reviewed counts. New entries and edits are available only during the first 30 days after account creation; pending entries can be edited in that window. Once reviewed, an entry is locked. The form collects date, task, hours, accomplishment, learnings, blockers/challenges, and AI tools used.

### Admin experience

The admin portal has candidate-directory, overview, and work-review surfaces. The reviewer selects one candidate to see that candidate’s submitted tasks and interview stage. Work reviews provides:

- Candidate, name, stack, date-range, and status filters.
- Checkboxes on pending tasks only; one review can approve or reject multiple selected tasks for the same candidate.
- A shared comment for all selected tasks. Rejection requires a non-empty comment; approval comments are optional.
- Locked status chips and comments for already reviewed work.
- Donut charts for task stages and candidate interview qualification.
- Candidate-level interview decisions after the candidate’s 30-day submission window.

Employees use the same review screen but the API restricts candidate lists, task searches, and decisions to their own candidates. Admins can view across candidates.

### Employee / mentor experience

Employees can create candidates from their portal, then view, update, and activate/deactivate only candidates created by their own account. Employee work reviews are limited to those candidates. Admins can create Employee accounts from employee management and retain broader candidate and review access. These restrictions are enforced by authenticated API routes and repository ownership filters, not just by the UI.

When a Candidate submits work, active Admins and the Employee who created that Candidate receive notifications. Task reviews and interview decisions notify the Candidate with the status/decision and comment. Notifications are persisted, private to each recipient, and pushed in real time through the authenticated SignalR notification hub; existing read and dismiss actions remain available.

### Frontend files

- `fullstack/client/src/CandidatePortal.jsx` — candidate navigation, profile, and stack editing.
- `fullstack/client/src/WorkTracking.jsx` — candidate entry list, create/edit form, and review status.
- `fullstack/client/src/AdminDashboard.jsx` — admin candidate directory, employee management, and review navigation.
- `fullstack/client/src/EmployeePortal.jsx`, `EmployeeCandidates.jsx` — employee-owned candidate management and scoped review navigation.
- `fullstack/client/src/EmployeeManagement.jsx` — Admin employee listing and account creation.
- `fullstack/client/src/WorkReviews.jsx` — candidate/status filters, pending-task multi-selection, shared-comment decisions, interview outcomes, and stage charts for Admins and Employees.
- `fullstack/client/src/StatusDonut.jsx` — reusable MUI donut chart for work and interview stages.
- `fullstack/client/src/NotificationsInbox.jsx`, `src/PortalShell.jsx` — recipient-specific persisted notifications and SignalR updates with reconnect handling.
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
| `Status` | `Pending`, `Approved`, or `Rejected`; reviewed entries are locked. |
| `AdminFeedback` | Shared reviewer comment (up to 2,000 characters), returned as `reviewComment`. Rejection requires a comment. |
| `SubmittedAtUtc` / `ReviewedAtUtc` | Submission and review timestamps. |
| `ReviewedByEmployeeAccountId` | Reviewer account that made the decision. |

`Grade` is retained only as legacy database history. The migration maps entries with a prior grade to `Approved` and ungraded entries to `Pending`, while preserving the original grade, feedback, reviewer, and review timestamp where present.

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
| `DELETE` | `/api/candidates/me/work-tracking/{entryId}` | Delete one of the caller's pending entries. Another candidate's ID returns `404`; reviewed entries return `409 Conflict`. |

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

Candidates can submit or edit pending entries only during the first 30 days after account creation. Reviewed entries—including rejected entries—cannot be edited, deleted, or resubmitted. Existing accounts are backfilled as already outside the submission window; their review queues remain available. The profile response exposes `canSubmitWork` and `workSubmissionEndsAtUtc`; after the window, work remains viewable but create and update endpoints return `409 Conflict`.

### Employee endpoints

These routes require a JWT with the `Employee` role. Candidate IDs are checked against the signed-in Employee's ownership on reads, updates, and status changes.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/employee/candidates` | List candidates created by the signed-in Employee only. |
| `GET` | `/api/employee/candidates/{candidateId}` | Read an owned candidate; other employees' candidates return `404`. |
| `POST` | `/api/employee/candidates` | Create a Candidate owned by the signed-in Employee. |
| `PUT` | `/api/employee/candidates/{candidateId}` | Update an owned candidate. |
| `PATCH` | `/api/employee/candidates/{candidateId}/status` | Activate or deactivate an owned candidate. |

Employee work review uses `/api/admin/work-tracking` and `/api/admin/work-tracking/review`; the API applies the Employee's account ID as an ownership filter. Interview decisions use `/api/admin/candidates/{candidateId}/interview-decision` with the same ownership restriction.

### Admin endpoints

Admin routes require a JWT with the `Admin` role. Admins can create employee accounts through `POST /api/admin/employees`, manage candidates through `/api/admin/candidates`, and review work across candidates. Employee creation is not available to Employees.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/admin/candidates` | List candidate profiles, including stack, for directory and candidate selection. |
| `GET` | `/api/admin/employees` | List employee accounts. |
| `POST` | `/api/admin/employees` | Create an Employee account; may return `400` for invalid input or `409` for a conflict. |
| `GET` | `/api/admin/work-tracking` | Search entries using optional query parameters: `candidateId`, `name`, `stack`, `dateFrom`, `dateTo`, and `status` (`Pending`, `Approved`, or `Rejected`). |
| `PUT` | `/api/admin/work-tracking/review` | Atomically decide multiple pending entries for one candidate using `candidateId`, `entryIds`, `status` (`Approved` or `Rejected`), and optional shared `comment`. Rejection requires a non-empty comment. |

Example filter:

```text
GET /api/admin/work-tracking?name=Alex&stack=React&dateFrom=2026-10-01&dateTo=2026-11-02&status=Pending
```

Bulk review JSON body:

```json
{
  "candidateId": 42,
  "entryIds": [101, 102],
  "status": "Approved",
  "comment": "Clear delivery and useful learning notes."
}
```

The response is the updated entry list. The request fails atomically with `409 Conflict` if any selected entry is already reviewed, belongs to another candidate, or is outside the Employee reviewer’s ownership scope. A blank rejection comment and invalid statuses return `400 Bad Request`.

Interview decisions are a separate stage, available after the 30-day work-submission period:

| Method | Route | Purpose |
| --- | --- | --- |
| `PUT` | `/api/admin/candidates/{candidateId}/interview-decision` | Set the candidate’s decision to `InterviewQualified` or `NotQualified` with an optional comment. Admins can decide across candidates; Employees can decide only for candidates they own. |

The interview decision can be set once from `PendingReview`; early decisions return `400`, already-decided stages return `409`, and inaccessible candidates return `404`.

A reversed date range or invalid status produces `400 Bad Request`. Missing candidates return `404 Not Found`. Role restrictions and Employee ownership are enforced by ASP.NET Core authorization and repository filters, not only by frontend menus.

### Notifications

Candidate work submissions create notifications for active Admins and, when the Candidate has an Employee owner, that Employee. Candidate creation by an Employee notifies active Admins. A task or interview decision notifies the Candidate with its status/decision and comment. Each notification is stored before it is sent over SignalR to the recipient’s authenticated user group.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/notifications` | List the signed-in user's active notifications. |
| `POST` | `/api/notifications/read` | Mark the signed-in user's notifications as read. |
| `DELETE` | `/api/notifications/{id}` | Dismiss one of the signed-in user's notifications. |
| SignalR | `/hubs/notifications` | Authenticated connection; receives `notification` events only for the account identified by the JWT. Reconnect triggers a client refresh from the persisted notification endpoint. |

### Backend files

- `fullstack/server/Models/CandidateProfile.cs`, `Models/CandidateWorkEntry.cs` — EF Core entities.
- `fullstack/server/Data/ApplicationDbContext.cs` — table mapping, field lengths, relationship, index, and cascade behavior.
- `fullstack/server/Dtos/CandidateApiDtos.cs`, `Dtos/WorkTrackingDtos.cs` — validated input and API response shapes.
- `fullstack/server/Controllers/CandidateController.cs` — authenticated self-service profile and work-entry routes.
- `fullstack/server/Controllers/AdminWorkTrackingController.cs` — admin-only search and review routes.
- `fullstack/server/Services/IWorkTrackingService.cs`, `Services/WorkTrackingService.cs` — service contract, submission, ownership-scoped edits, filtering delegation, and review behavior.
- `fullstack/server/Repositories/IWorkTrackingRepository.cs`, `Repositories/WorkTrackingRepository.cs` — EF Core persistence and filtering.
- `fullstack/server/Hubs/NotificationHub.cs`, `server/Program.cs` — authenticated user-scoped SignalR notification delivery and endpoint registration.
- `fullstack/server/Program.cs` — dependency-injection registrations.
- `fullstack/server/Migrations/` — database schema migrations, including creation-time backfill and legacy grade-to-status mapping.

## Local verification

From `fullstack/client`, run `npm run lint` and `npm run build`. Build the API with `dotnet build fullstack/server/fullstackapplication.Server.csproj`, and run API integration tests with `dotnet test fullstack/server.Tests/fullstackapplication.Server.Tests.csproj`. Apply the generated migration to the configured database before using the new endpoints against a persistent SQL Server database; application startup already applies pending migrations.
