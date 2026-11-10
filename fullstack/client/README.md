# Fullstack Application — Client Project Flow and Implementation

## 1. Project overview

The client is the React frontend for a work-tracking portal with separate experiences for Admins, Employees/mentors, and Candidates:

- **Admins** get a workforce and task-review overview with date/candidate filters, then manage candidate profiles, create employee accounts, and review submitted work reports.
- **Employees/mentors** can create and manage candidates assigned to them, and review work submitted by those candidates.
- **Candidates** can view and update their profile, manage a resume, change their password, and submit work reports during the first 30 days after account creation.

The client communicates with an ASP.NET Core API. The API handles authentication, authorization, business rules, notifications, and persistence in SQL Server. The frontend does not connect directly to the database.

### Role-based workflow

1. An Admin creates Employee accounts from the employee-management screen. Employee creation is restricted to Admins.
2. An Admin or Employee can create a Candidate. Candidates created by an Employee are linked to that Employee as their owner/mentor; the Employee candidate list and candidate-management API only return or modify candidates linked to the signed-in Employee. Admin candidate-management APIs remain Admin-scoped and can manage the candidate directory.
3. Candidates can submit and edit pending work during their first 30 days after account creation. At the end of the window, the UI keeps entries viewable but disables submissions and edits; the API enforces the same boundary. Existing accounts are migrated as already past the submission window.
4. An Admin or the owning Employee can select one candidate, check multiple pending tasks, and apply one Approved or Rejected decision with a shared comment. Rejections require a comment; reviewed tasks stay locked. The Candidate sees stage chips, review comments, and a donut of Approved, Rejected, Pending, Reviewed, and Not reviewed counts.
5. After the 30-day window, an Admin or the owning Employee can set the separate candidate interview decision to Interview qualified or Not qualified. Candidate creation, task submissions, task decisions, and interview decisions create recipient-specific persisted notifications; SignalR pushes those notifications to the authenticated recipient.

These access boundaries are enforced by API authorization and server-side ownership filters, not only by hiding client navigation.

## 2. Project structure

The solution is divided into two main parts:

- `fullstack/client` — React frontend, built and served during development by Vite.
- `fullstack/server` — ASP.NET Core backend, which exposes API endpoints and accesses SQL Server through Entity Framework Core.

## 3. Client startup and component tree

```text
index.html
└── src/main.jsx
	└── App
		├── Sign-in screen
		├── AdminDashboard (when the signed-in role is Admin)
		│   ├── PortalShell
		│   ├── AdminOverview (when the overview section is selected)
		│   ├── EmployeeManagement
		│   └── WorkReviews (when the reviews section is selected)
		├── EmployeePortal (when the signed-in role is Employee)
		│   ├── PortalShell
		│   ├── EmployeeCandidates
		│   └── WorkReviews (scoped to the employee's candidates)
		└── CandidatePortal (when the signed-in role is Candidate)
			├── PortalShell
			└── WorkTracking (when the work section is selected)
```

1. `index.html` provides the browser page and its `<div id="root">` mount point.
2. `src/main.jsx` imports the global CSS and `App.jsx`, then asks React to render `<App />` into the root element. It wraps the app in React `StrictMode` during development checks.
3. `src/App.jsx` initially renders the sign-in screen. After a successful login, it reads the user's role and displays `AdminDashboard`, `EmployeePortal`, or `CandidatePortal`.
4. Each portal uses `PortalShell` for shared layout, navigation, notifications, profile menu, logout, and dark-mode controls.
5. The selected portal section determines which screen is displayed: employee management and candidate/work-review screens for Admins, owned candidates and scoped work reviews for Employees, and profile/work tracking for Candidates.

The component files are not separate browser pages; React renders and updates them inside the root element.

## 4. Component lifecycle

- **Render:** React runs a component function to produce its UI from its current props and state.
- **Mount:** When a component first appears, React adds its UI to the page. Components such as `AdminDashboard`, `CandidatePortal`, `WorkTracking`, and `WorkReviews` use `useEffect` to load data when needed.
- **Update:** User actions or API responses update component state. React then renders the affected UI with the new values.
- **Unmount:** When a component is removed, its effect cleanup runs. Data-loading effects use `AbortController` to cancel requests that are still in progress.

In development, React `StrictMode` can run effect setup and cleanup an additional time to help reveal unsafe side effects. This is a development check, not an extra production feature.

## 5. End-to-end request flow

```text
User action
  → React page/component
  → src/api.js
  → ASP.NET Core controller
  → service
  → repository / Entity Framework Core
  → SQL Server
  → API response
  → React state update and UI render
```

For example, when a candidate submits a report, `WorkTracking.jsx` submits the form through `api.js`. The API authorizes the request, persists the report through the work-tracking service and repository, and creates notification rows for active Admins and the candidate's creating Employee (if any). When an Admin or owning Employee reviews the report, the API enforces that reviewer scope and notifies the Candidate. The response updates the React view.

## 6. Frontend file responsibilities

| File | Responsibility |
|---|---|
| `index.html` | HTML page that hosts the React application and provides the root mount point. |
| `src/main.jsx` | React startup point; renders `App` and imports global CSS. |
| `src/App.jsx` | Sign-in screen, role-based portal selection, tab-scoped session restoration, portal URL history, and session expiry/logout handling. |
| `src/authSession.js` | Stores the JWT/user response only in `sessionStorage`, validates the saved expiry, and maps role menus to reloadable portal hash routes. |
| `src/api.js` | Shared API request helper; adds a bearer token when provided, disables response caching, handles unsuccessful HTTP responses, and signals the app to clear expired sessions. |
| `src/PortalChat.jsx` | Floating chat window for role-scoped employee, candidate, and work-review queries. |
| `src/AdminDashboard.jsx` | Admin candidate directory and profile management, section selection, and overview/review/employee screen composition. |
| `src/AdminOverview.jsx` | Admin workforce KPIs, candidate and employee status donuts, candidate task review donuts, date trends, and filtered roster activity. |
| `src/ProfileAvatar.jsx` | Reusable profile-photo upload and initials avatar; compressed JPEG photos are saved to the account through the API. |
| `src/EmployeePortal.jsx` | Loads server-derived employee permissions and renders only the authorized candidate, review, and direct-report team sections. |
| `src/EmployeeCandidates.jsx` | Employee-owned candidate list with create, edit, activate/deactivate, and inactive-delete controls shown according to assigned permissions. |
| `src/EmployeeManagement.jsx` | Admin employee directory and delegated direct-report management; includes permission-role creation/assignment, grant-access checkbox, employee status controls, and direct-report active/inactive donut. |
| `src/CandidatePortal.jsx` | Candidate profile, resume, password, and portal screens. |
| `src/WorkTracking.jsx` | Candidate task forms, submission-window information, status/comment list, and review-count donut. |
| `src/WorkReviews.jsx` | Candidate selection, status/date filters, pending-task multi-selection, shared-comment reviews, interview decisions gated by employee permissions, and Admin/Employee stage donuts; ownership is enforced by the API. |
| `src/StatusDonut.jsx` | Reusable MUI donut chart for work review, interview qualification, and direct-report active/inactive counts. |
| `src/NotificationsInbox.jsx` | Displays recipient-scoped persisted notifications, supports read/dismiss actions, and receives SignalR events with reconnect refresh. |
| `src/PortalShell.jsx` | Shared portal layout, history-aware navigation, notifications, profile menu, logout, dark-mode controls, and floating chat. |
| `src/PortalIcon.jsx` | Reusable icons used by portal screens. |
| `src/WorkReportChart.jsx` | Reusable chart display for report data. |

Employee access management is surfaced in **Admin workspace → Employees** and, when granted a team-management capability, **Employee workspace → My team**. Managers see only their direct reports; the team donut shows active and inactive direct reports, not the full descendant hierarchy. Candidate and review controls are permission-aware in the UI and are independently enforced by the API. The server endpoints and permission catalog are documented in [Employee access control and delegated management](../docs/EMPLOYEE-ACCESS-CONTROL-PLAN.md).
| `src/index.css` | Global styles. |
| `src/App.css` | Application-specific styles. |
| `src/assets/` | Static images and SVG assets. |
| `vite.config.js` | Vite configuration, React plugin, development HTTPS, and proxy forwarding for API routes. |
| `package.json` | Client scripts and declared direct npm dependencies. |
| `package-lock.json` | Exact resolved dependency versions for reproducible installs. |

### JWT authentication in the frontend

The frontend handles the login request and uses the access token returned by the API; it does not create or cryptographically validate JWTs. Browser Back from a portal subpage returns to the role's dashboard; returning to login clears the token. Reload restores the previous portal section from the URL hash and tab-scoped session storage.

| File | JWT responsibility |
|---|---|
| `src/App.jsx` | Sends credentials to `POST /api/auth/login`, stores the returned session in tab-scoped `sessionStorage`, and restores the current role/menu after reload while the server-issued token is valid (60 minutes by default). Logging out, an expired/unauthorized request, or navigating Back to `#/login` removes the stored session. |
| `src/api.js` | Adds the session token as a Bearer authorization header to authenticated API requests. |
| `src/api/notifications.js` and other API modules | Pass the token through the shared `apiRequest` helper when calling protected endpoints. |

The API creates the signed token in `server/Services/JwtTokenService.cs` and configures JWT bearer validation in `server/Program.cs`. Portal sections are represented in the URL hash so reload restores the current section; browser Back from a subpage returns to that role's default dashboard, and Back to the login route clears the token. Protected API and MCP responses use `no-store`. See [JWT authentication and token lifecycle](../docs/PROJECT-GUIDE.md#jwt-authentication-and-token-lifecycle) for the complete flow.

### Floating chat assistant

`PortalShell` includes the shared floating `PortalChat` for Admin, Employee, and Candidate accounts. It sends supported questions to `POST /api/chat/ask` with the current bearer token. Admins can query the employee directory and global candidate/work summaries; Employees are limited to candidates they created; Candidates see only their own profile and work entries. The assistant displays active/inactive state, task review stages, and review comments where appropriate. Chat history remains in component memory and is discarded when the portal is left or reloaded.

The backend also exposes the read-only MCP tools at `/mcp` for compatible MCP clients; the chat panel uses the API chat endpoint and does not require an external model provider.

## 7. Backend flow and responsibilities

The frontend is part of a full-stack solution. The following backend files are relevant to the request path:

- `server/Program.cs` configures dependency injection, SQL Server, CORS, authentication, authorization, controllers, and development-time OpenAPI/Swagger.
- `server/Controllers/` defines HTTP API endpoints. `AdminEmployeesController` restricts employee-account creation to Admins; `EmployeeCandidatesController` scopes candidate operations to the signed-in Employee; `AdminWorkTrackingController` allows Admins and Employees to search/review work with server-enforced ownership checks.
- `server/Services/` contains operations and business rules, such as creating, updating, and reviewing work reports and issuing notifications.
- `server/Repositories/` handles data-access operations.
- `server/Models/` defines persisted entities, including `CandidateProfile` and `CandidateWorkEntry`.
- `server/Data/ApplicationDbContext.cs` configures Entity Framework Core entities and relationships.
- `server/Migrations/` contains database schema changes tracked by Entity Framework Core.

After login, the API returns a JWT access token and user information. The client sends the token with protected requests. The API validates the token and checks the user's role before allowing access to protected operations.

## 8. Main API routes used by the client

- `POST /api/auth/login` — authenticate and obtain a token and user role.
- `/api/admin/candidates` — Admin candidate-management operations.
- `/api/admin/employees` — Admin-only employee listing and account creation.
- `/api/employee/candidates` — Employee candidate listing and create/update/status operations, scoped to candidates created by that Employee.
- `POST /api/chat/ask` — role-scoped question lookup for employees, candidates, and work-review details.
- `POST /mcp` — authenticated Streamable HTTP MCP tools for employee, candidate, and review lookup.
- `GET /api/admin/work-tracking` — task search by candidate, name, stack, date range, and status; Employee results are ownership-scoped.
- `PUT /api/admin/work-tracking/review` — decide multiple pending entries for one candidate using a shared status and comment.
- `PUT /api/admin/candidates/{candidateId}/interview-decision` — set the separate post-day-30 interview outcome; Employee ownership is enforced.
- `/api/notifications` — authenticated-user notification list, mark-read, and dismiss operations.
- SignalR `/hubs/notifications` — authenticated real-time notifications; reconnect reloads the persisted inbox.
- `/api/candidates/me` — candidate profile, interview stage, 30-day deadline, and submission eligibility.
- `/api/candidates/me/work-tracking` — candidate work-report operations; create/update are accepted only in the first 30 days.
- `/api/candidates/me/resume` — candidate resume upload/update operation.
- `/api/candidates/me/password` — candidate password change operation.

During local development, `vite.config.js` proxies `/api` requests to the ASP.NET Core server.

## 9. npm dependencies and libraries

The versions below are the **resolved direct package versions recorded in `package-lock.json`**. `package.json` declares version ranges; the lockfile records the versions installed by a lockfile-based install. These are direct dependencies, not every transitive package installed underneath them.

### Runtime dependencies

| Package | Locked version | Purpose |
|---|---:|---|
| `react` | `19.3.1` | Builds the interface from reusable components. |
| `react-dom` | `19.3.1` | Renders React into the browser DOM. |
| `@microsoft/signalr` | See `package-lock.json` | Maintains authenticated real-time notification delivery and automatic reconnect. |
| `@mui/material` | `9.5.0` | Material UI component library: buttons, text fields, dialogs, tables, layouts, and other UI components. |
| `@emotion/react` | `11.14.0` | Styling engine used by Material UI. |
| `@emotion/styled` | `11.14.1` | Styled-component support used by Material UI. |
| `formik` | `2.4.9` | Manages form values and form submissions. |

### Development dependencies

| Package | Locked version | Purpose |
|---|---:|---|
| `vite` | `8.3.5` | Development server and production build tool. |
| `@vitejs/plugin-react` | `6.1.2` | React support for Vite. |
| `eslint` | `10.13.0` | Reports common JavaScript code and style issues. |
| `@eslint/js` | `10.0.1` | JavaScript rules used by ESLint configuration. |
| `eslint-plugin-react-hooks` | `7.1.1` | Checks rules and usage patterns for React Hooks. |
| `eslint-plugin-react-refresh` | `0.5.7` | ESLint support for React Fast Refresh. |
| `globals` | `17.13.0` | Provides known environment globals to ESLint. |
| `@types/react` | `19.3.0` | React type definitions for development tooling. |
| `@types/react-dom` | `19.3.0` | React DOM type definitions for development tooling. |

### How the UI and form libraries are used

- **Material UI (MUI)** supplies the visual building blocks used throughout the screens, including buttons, cards, dialogs, alerts, menus, tables, and text fields. The app also uses MUI's theme provider to support light and dark themes.
- **Formik** manages form state and submissions in forms such as candidate profile editing, work tracking, work reviews, and resume upload.
- **Emotion** provides the styling integration required by Material UI.
- **React** organizes the screens into components and uses state and effects to respond to user actions and API results.

## 10. Client scripts

Run these commands from `fullstack/client`:

- `npm run dev` — start the Vite development server.
- `npm run build` — create a production build.
- `npm run preview` — preview the production build locally.
- `npm run lint` — run ESLint on the client code.

## 11. Demo workflow

1. Sign in as an Admin, create an Employee account, and optionally create a Candidate from the Admin directory.
2. Sign in as the Employee, create a Candidate, and verify that the Employee's candidate list contains only candidates assigned to that Employee.
3. Sign in as that Candidate and submit a work entry during the submission window. Verify task-submission notifications appear for active Admins and the creating Employee.
4. As the owning Employee, select that candidate and approve or reject pending tasks; verify multiple pending tasks share a decision/comment, rejection requires a comment, and another Employee cannot access the candidate. Admins retain cross-candidate review access.
5. After the candidate’s 30-day window, set an interview qualification decision as the owner or Admin. Sign in as the Candidate and verify work status/comment, interview stage, donuts, submission deadline, and real-time notification.

## 12. Short technical summary

The application separates UI, API, business logic, and persistence. React components own the screens and interaction state; `api.js` centralizes HTTP communication; ASP.NET Core controllers expose endpoints; services implement operations; repositories and Entity Framework Core access SQL Server. JWT authentication and role-based authorization protect operations, while server-side ownership filters constrain Employee candidate and work-review access.
