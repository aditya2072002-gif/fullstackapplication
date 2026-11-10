using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using fullstackapplication.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace fullstackapplication.Server.Tests;

public sealed class AuthenticationApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public AuthenticationApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LoginIssuesRoleTokenAndOnlyAdminsCanReadAccountLists()
    {
        var swaggerUi = await _client.GetAsync("/swagger/index.html");
        swaggerUi.EnsureSuccessStatusCode();
        var swaggerDocument = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("Bearer", swaggerDocument, StringComparison.OrdinalIgnoreCase);

        var candidateLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "candidate",
            password = "CandidateDemo123!"
        });
        candidateLogin.EnsureSuccessStatusCode();
        using var candidateJson = JsonDocument.Parse(await candidateLogin.Content.ReadAsStringAsync());
        var candidateToken = candidateJson.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(candidateToken));
        Assert.False(candidateJson.RootElement.GetProperty("user").TryGetProperty("passwordHash", out _));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var deniedList = await _client.GetAsync("/api/admins");
        Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);

        var adminLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "AdminDemo123!"
        });
        adminLogin.EnsureSuccessStatusCode();
        using var adminJson = JsonDocument.Parse(await adminLogin.Content.ReadAsStringAsync());
        var adminToken = adminJson.RootElement.GetProperty("accessToken").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var adminsResponse = await _client.GetAsync("/api/admins");
        adminsResponse.EnsureSuccessStatusCode();
        var adminsBody = await adminsResponse.Content.ReadAsStringAsync();
        Assert.Contains("admin", adminsBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", adminsBody, StringComparison.OrdinalIgnoreCase);

        var candidatesResponse = await _client.GetAsync("/api/candidates");
        candidatesResponse.EnsureSuccessStatusCode();
        var candidatesBody = await candidatesResponse.Content.ReadAsStringAsync();
        Assert.Contains("candidate", candidatesBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", candidatesBody, StringComparison.OrdinalIgnoreCase);
        using var candidatesJson = JsonDocument.Parse(candidatesBody);
        Assert.True(candidatesJson.RootElement.GetArrayLength() >= 10);
        Assert.Contains(candidatesJson.RootElement.EnumerateArray(),
            account => account.GetProperty("username").GetString() == "candidate09");
    }

    [Fact]
    public async Task AdminManagesCandidateProfileWhileCandidateCanOnlyUpdateResumeAndPassword()
    {
        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var invalidCreateForm = new MultipartFormDataContent();
        AddFormValue(invalidCreateForm, "Username", "ab");
        AddFormValue(invalidCreateForm, "Password", "short");
        AddFormValue(invalidCreateForm, "FirstName", "New");
        AddFormValue(invalidCreateForm, "LastName", "Candidate");
        AddFormValue(invalidCreateForm, "Email", "new@example.com");
        AddFormValue(invalidCreateForm, "PhoneNumber", "555-0101");
        AddFormValue(invalidCreateForm, "Experience", "Three years of experience");
        AddFormValue(invalidCreateForm, "PerformanceScore", "87");
        var invalidCreateResponse = await _client.PostAsync("/api/admin/candidates", invalidCreateForm);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateResponse.StatusCode);

        using var createForm = new MultipartFormDataContent();
        AddFormValue(createForm, "Username", "newcandidate");
        AddFormValue(createForm, "Password", "InitialCandidate123!");
        AddFormValue(createForm, "FirstName", "New");
        AddFormValue(createForm, "LastName", "Candidate");
        AddFormValue(createForm, "Email", "new@example.com");
        AddFormValue(createForm, "PhoneNumber", "555-0101");
        AddFormValue(createForm, "YearsOfExperience", "3");
        AddFormValue(createForm, "PerformanceScore", "87");
        var createResponse = await _client.PostAsync("/api/admin/candidates", createForm);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var candidateId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("new@example.com", createdJson.RootElement.GetProperty("email").GetString());
        Assert.False(createdJson.RootElement.TryGetProperty("password", out _));
        Assert.False(createdJson.RootElement.TryGetProperty("passwordHash", out _));
        var activeDeleteResponse = await _client.DeleteAsync($"/api/admin/candidates/{candidateId}");
        Assert.Equal(HttpStatusCode.Conflict, activeDeleteResponse.StatusCode);

        var adminListResponse = await _client.GetAsync("/api/admin/candidates");
        adminListResponse.EnsureSuccessStatusCode();
        using var adminListJson = JsonDocument.Parse(await adminListResponse.Content.ReadAsStringAsync());
        Assert.Contains(adminListJson.RootElement.EnumerateArray(),
            profile => profile.GetProperty("id").GetInt32() == candidateId);

        var candidateToken = await LoginAndGetTokenAsync("newcandidate", "InitialCandidate123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var selfResponse = await _client.GetAsync("/api/candidates/me");
        selfResponse.EnsureSuccessStatusCode();
        using var selfJson = JsonDocument.Parse(await selfResponse.Content.ReadAsStringAsync());
        Assert.Equal("New", selfJson.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("new@example.com", selfJson.RootElement.GetProperty("email").GetString());
        Assert.False(selfJson.RootElement.TryGetProperty("passwordHash", out _));

        using var invalidResumeForm = new MultipartFormDataContent();
        invalidResumeForm.Add(new ByteArrayContent([1, 2, 3]), "Resume", "candidate.exe");
        var invalidResumeResponse = await _client.PutAsync("/api/candidates/me/resume", invalidResumeForm);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResumeResponse.StatusCode);

        using var oversizedResumeForm = new MultipartFormDataContent();
        var oversizedPdf = new byte[10 * 1024 * 1024 + 1];
        System.Text.Encoding.ASCII.GetBytes("%PDF-").CopyTo(oversizedPdf, 0);
        oversizedResumeForm.Add(new ByteArrayContent(oversizedPdf), "Resume", "oversized.pdf");
        var oversizedResumeResponse = await _client.PutAsync("/api/candidates/me/resume", oversizedResumeForm);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedResumeResponse.StatusCode);

        using var resumeForm = new MultipartFormDataContent();
        var pdfContent = new ByteArrayContent(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7 sample pdf"));
        resumeForm.Add(pdfContent, "Resume", "candidate.pdf");
        var resumeResponse = await _client.PutAsync("/api/candidates/me/resume", resumeForm);
        resumeResponse.EnsureSuccessStatusCode();
        using var resumeJson = JsonDocument.Parse(await resumeResponse.Content.ReadAsStringAsync());
        Assert.Equal("candidate.pdf", resumeJson.RootElement.GetProperty("resumeFileName").GetString());

        var candidateCannotList = await _client.GetAsync("/api/admin/candidates");
        Assert.Equal(HttpStatusCode.Forbidden, candidateCannotList.StatusCode);
        var candidateCannotChangeUsername = await _client.PutAsJsonAsync("/api/candidates/me", new { username = "changedname" });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, candidateCannotChangeUsername.StatusCode);
        var wrongPassword = await _client.PutAsJsonAsync("/api/candidates/me/password", new
        {
            currentPassword = "wrong-password",
            newPassword = "UpdatedCandidate123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongPassword.StatusCode);
        var passwordChange = await _client.PutAsJsonAsync("/api/candidates/me/password", new
        {
            currentPassword = "InitialCandidate123!",
            newPassword = "UpdatedCandidate123!"
        });
        Assert.Equal(HttpStatusCode.NoContent, passwordChange.StatusCode);
        var updatedToken = await LoginAndGetTokenAsync("newcandidate", "UpdatedCandidate123!");
        Assert.False(string.IsNullOrWhiteSpace(updatedToken));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deactivateResponse = await _client.PatchAsJsonAsync(
            $"/api/admin/candidates/{candidateId}/status", new { isActive = false });
        deactivateResponse.EnsureSuccessStatusCode();
        var deactivatedResponse = await _client.GetAsync($"/api/admin/candidates/{candidateId}");
        deactivatedResponse.EnsureSuccessStatusCode();
        using var deactivatedJson = JsonDocument.Parse(await deactivatedResponse.Content.ReadAsStringAsync());
        Assert.False(deactivatedJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal("New", deactivatedJson.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("new@example.com", deactivatedJson.RootElement.GetProperty("email").GetString());
        Assert.Equal("555-0101", deactivatedJson.RootElement.GetProperty("phoneNumber").GetString());
        Assert.NotEqual(JsonValueKind.Null, deactivatedJson.RootElement.GetProperty("inactiveAtUtc").ValueKind);
        var inactiveCandidateLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "newcandidate",
            password = "UpdatedCandidate123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, inactiveCandidateLogin.StatusCode);

        using var reactivateForm = CreateCandidateUpdateForm(isActive: true);
        var reactivateResponse = await _client.PutAsync($"/api/admin/candidates/{candidateId}", reactivateForm);
        reactivateResponse.EnsureSuccessStatusCode();
        using var reactivatedJson = JsonDocument.Parse(await reactivateResponse.Content.ReadAsStringAsync());
        Assert.True(reactivatedJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, reactivatedJson.RootElement.GetProperty("inactiveAtUtc").ValueKind);
        var unchangedPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "newcandidate",
            password = "UpdatedCandidate123!"
        });
        unchangedPasswordLogin.EnsureSuccessStatusCode();
        var adminResetLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "newcandidate",
            password = "AdminResetAttempt123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, adminResetLogin.StatusCode);

        var deactivateAgainResponse = await _client.PatchAsJsonAsync(
            $"/api/admin/candidates/{candidateId}/status", new { isActive = false });
        deactivateAgainResponse.EnsureSuccessStatusCode();
        var deleteResponse = await _client.DeleteAsync($"/api/admin/candidates/{candidateId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var deletedResponse = await _client.GetAsync($"/api/admin/candidates/{candidateId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task CandidateSubmitsOwnWorkAndAdminCanFilterAndReviewIt()
    {
        var candidateToken = await LoginAndGetTokenAsync("candidate", "CandidateDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        using var candidateProfile = JsonDocument.Parse(await _client.GetStringAsync("/api/candidates/me"));
        var candidateId = candidateProfile.RootElement.GetProperty("id").GetInt32();

        var stackResponse = await _client.PutAsJsonAsync("/api/candidates/me/stack", new { stack = "React, ASP.NET Core" });
        stackResponse.EnsureSuccessStatusCode();
        using var stackJson = JsonDocument.Parse(await stackResponse.Content.ReadAsStringAsync());
        Assert.Equal("React, ASP.NET Core", stackJson.RootElement.GetProperty("stack").GetString());

        var missingBlockersResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 2), task = "Missing blockers field", hoursSpent = 1m,
            accomplishment = "Attempt submission", learnings = "Test validation", aiToolsUsed = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingBlockersResponse.StatusCode);

        var createResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 2),
            task = "Implement work tracker",
            hoursSpent = 3.5m,
            accomplishment = "Added candidate task submission",
            learnings = "Used the existing API structure",
            blockersAndChallenges = "No blockers",
            aiToolsUsed = "GitHub Copilot"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var entryId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("Pending", createdJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("No blockers", createdJson.RootElement.GetProperty("blockersAndChallenges").GetString());
        var secondCreateResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 3), task = "Second review task", hoursSpent = 1m,
            accomplishment = "Another delivery", learnings = "More learning", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        secondCreateResponse.EnsureSuccessStatusCode();
        using var secondCreatedJson = JsonDocument.Parse(await secondCreateResponse.Content.ReadAsStringAsync());
        var secondEntryId = secondCreatedJson.RootElement.GetProperty("id").GetInt32();
        var thirdCreateResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 4), task = "Third review task", hoursSpent = 1m,
            accomplishment = "Third delivery", learnings = "Third learning", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        thirdCreateResponse.EnsureSuccessStatusCode();
        using var thirdCreatedJson = JsonDocument.Parse(await thirdCreateResponse.Content.ReadAsStringAsync());
        var thirdEntryId = thirdCreatedJson.RootElement.GetProperty("id").GetInt32();

        var pendingDeleteResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 1), task = "Entry to delete", hoursSpent = 1m,
            accomplishment = "Pending entry", learnings = "Delete test", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        pendingDeleteResponse.EnsureSuccessStatusCode();
        using var pendingDeleteJson = JsonDocument.Parse(await pendingDeleteResponse.Content.ReadAsStringAsync());
        var pendingDeleteId = pendingDeleteJson.RootElement.GetProperty("id").GetInt32();
        var deletePendingResponse = await _client.DeleteAsync($"/api/candidates/me/work-tracking/{pendingDeleteId}");
        Assert.Equal(HttpStatusCode.NoContent, deletePendingResponse.StatusCode);

        var otherCandidateToken = await LoginAndGetTokenAsync("candidate09", "CandidateDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherCandidateToken);
        var otherCandidateUpdate = await _client.PutAsJsonAsync($"/api/candidates/me/work-tracking/{entryId}", new
        {
            workDate = new DateOnly(2026, 11, 2), task = "Unauthorized edit", hoursSpent = 1m,
            accomplishment = "Should not be saved", learnings = "Unauthorized learning", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        Assert.Equal(HttpStatusCode.NotFound, otherCandidateUpdate.StatusCode);
        var otherCandidateDelete = await _client.DeleteAsync($"/api/candidates/me/work-tracking/{entryId}");
        Assert.Equal(HttpStatusCode.NotFound, otherCandidateDelete.StatusCode);
        var candidateReviewAttempt = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new { candidateId, entryIds = new[] { entryId, secondEntryId }, status = "Approved", comment = "Good" });
        Assert.Equal(HttpStatusCode.Forbidden, candidateReviewAttempt.StatusCode);

        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var filteredResponse = await _client.GetAsync("/api/admin/work-tracking?stack=ASP.NET%20Core&dateFrom=2026-11-02&dateTo=2026-11-02");
        filteredResponse.EnsureSuccessStatusCode();
        using var filteredJson = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync());
        Assert.Contains(filteredJson.RootElement.EnumerateArray(), entry => entry.GetProperty("id").GetInt32() == entryId);
        var reversedDateRangeResponse = await _client.GetAsync(
            "/api/admin/work-tracking?dateFrom=2026-11-03&dateTo=2026-11-02");
        Assert.Equal(HttpStatusCode.BadRequest, reversedDateRangeResponse.StatusCode);
        var invalidStatusResponse = await _client.GetAsync("/api/admin/work-tracking?status=unknown");
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatusResponse.StatusCode);
        var missingRejectionComment = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new { candidateId, entryIds = new[] { entryId }, status = "Rejected", comment = " " });
        Assert.Equal(HttpStatusCode.BadRequest, missingRejectionComment.StatusCode);

        var reviewRequest = new { candidateId, entryIds = new[] { entryId, secondEntryId }, status = "Approved", comment = "Clear delivery." };
        var reviewResponse = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", reviewRequest);
        reviewResponse.EnsureSuccessStatusCode();
        using var reviewJson = JsonDocument.Parse(await reviewResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, reviewJson.RootElement.GetArrayLength());
        Assert.All(reviewJson.RootElement.EnumerateArray(), entry => Assert.Equal("Approved", entry.GetProperty("status").GetString()));
        Assert.Equal("Demo Administrator", reviewJson.RootElement[0].GetProperty("reviewedByDisplayName").GetString());
        var duplicateReviewResponse = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", reviewRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateReviewResponse.StatusCode);
        var mixedStateReview = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new
        {
            candidateId, entryIds = new[] { entryId, thirdEntryId }, status = "Rejected", comment = "Needs changes."
        });
        Assert.Equal(HttpStatusCode.Conflict, mixedStateReview.StatusCode);
        var rejectPendingReview = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new
        {
            candidateId, entryIds = new[] { thirdEntryId }, status = "Rejected", comment = "Please revise."
        });
        rejectPendingReview.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var entriesResponse = await _client.GetAsync("/api/candidates/me/work-tracking");
        entriesResponse.EnsureSuccessStatusCode();
        using var entriesJson = JsonDocument.Parse(await entriesResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, entriesJson.RootElement.GetArrayLength());
        Assert.Equal(2, entriesJson.RootElement.EnumerateArray().Count(entry => entry.GetProperty("status").GetString() == "Approved"));
        Assert.Equal(1, entriesJson.RootElement.EnumerateArray().Count(entry => entry.GetProperty("status").GetString() == "Rejected"));
        var approvedEntry = Assert.Single(entriesJson.RootElement.EnumerateArray(), entry => entry.GetProperty("id").GetInt32() == entryId);
        Assert.Equal("Clear delivery.", approvedEntry.GetProperty("reviewComment").GetString());
        Assert.Equal("Demo Administrator", approvedEntry.GetProperty("reviewedByDisplayName").GetString());
        Assert.Equal("No blockers", approvedEntry.GetProperty("blockersAndChallenges").GetString());
        var editReviewedResponse = await _client.PutAsJsonAsync($"/api/candidates/me/work-tracking/{entryId}", new
        {
            workDate = new DateOnly(2026, 11, 2), task = "Changed after review", hoursSpent = 1m,
            accomplishment = "Attempt to modify reviewed entry", learnings = "Should remain locked", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        Assert.Equal(HttpStatusCode.Conflict, editReviewedResponse.StatusCode);
        var deleteReviewedResponse = await _client.DeleteAsync($"/api/candidates/me/work-tracking/{entryId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteReviewedResponse.StatusCode);
        var editRejectedResponse = await _client.PutAsJsonAsync($"/api/candidates/me/work-tracking/{thirdEntryId}", new
        {
            workDate = new DateOnly(2026, 11, 4), task = "Attempt rejected edit", hoursSpent = 1m,
            accomplishment = "Should remain locked", learnings = "Locked rejection", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        Assert.Equal(HttpStatusCode.Conflict, editRejectedResponse.StatusCode);
    }

    [Fact]
    public async Task EmployeeMentorCanManageOwnedCandidatesAndSubmitOnlyOneReview()
    {
        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var employeeResponse = await _client.PostAsJsonAsync("/api/admin/employees", new
        {
            username = "mentor-test",
            password = "MentorInitial123!",
            displayName = "Mentor Test",
            department = "Engineering",
            stack = ".NET, React"
        });
        Assert.Equal(HttpStatusCode.Created, employeeResponse.StatusCode);
        using var employeeJson = JsonDocument.Parse(await employeeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Employee", employeeJson.RootElement.GetProperty("role").GetString());
        Assert.Equal("Engineering", employeeJson.RootElement.GetProperty("department").GetString());
        Assert.Equal(JsonValueKind.Null, employeeJson.RootElement.GetProperty("roleDefinitionId").ValueKind);
        var employeeId = employeeJson.RootElement.GetProperty("id").GetInt32();
        var roles = await _client.GetFromJsonAsync<JsonElement>("/api/roles");
        var mentorRoleId = roles.EnumerateArray().Single(role => role.GetProperty("name").GetString() == "Mentor").GetProperty("id").GetInt32();
        var assignMentorRole = await _client.PutAsJsonAsync(
            $"/api/roles/employees/{employeeId}", new { roleDefinitionId = mentorRoleId });
        Assert.Equal(HttpStatusCode.NoContent, assignMentorRole.StatusCode);

        var employeeToken = await LoginAndGetTokenAsync("mentor-test", "MentorInitial123!");
        using var employeeLogin = JsonDocument.Parse(await (await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "mentor-test", password = "MentorInitial123!"
        })).Content.ReadAsStringAsync());
        Assert.Equal("Employee", employeeLogin.RootElement.GetProperty("user").GetProperty("role").GetString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var createCandidateResponse = await _client.PostAsJsonAsync("/api/employee/candidates", new
        {
            username = "mentor-candidate",
            password = "CandidateInitial123!",
            firstName = "Mentored",
            lastName = "Candidate",
            email = "mentored@example.com",
            phoneNumber = "5550102030",
            stack = "React"
        });
        Assert.Equal(HttpStatusCode.Created, createCandidateResponse.StatusCode);
        using var candidateJson = JsonDocument.Parse(await createCandidateResponse.Content.ReadAsStringAsync());
        var candidateId = candidateJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("Mentor Test", candidateJson.RootElement.GetProperty("creatorDisplayName").GetString());
        Assert.Equal(JsonValueKind.Null, candidateJson.RootElement.GetProperty("yearsOfExperience").ValueKind);

        var employeeCandidates = await _client.GetAsync("/api/employee/candidates");
        employeeCandidates.EnsureSuccessStatusCode();
        using var employeeCandidatesJson = JsonDocument.Parse(await employeeCandidates.Content.ReadAsStringAsync());
        Assert.Equal(candidateId, Assert.Single(employeeCandidatesJson.RootElement.EnumerateArray()).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/employees")).StatusCode);

        var candidateToken = await LoginAndGetTokenAsync("mentor-candidate", "CandidateInitial123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var workResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = new DateOnly(2026, 11, 4), task = "Mentor assignment", hoursSpent = 2m,
            accomplishment = "Completed work", learnings = "Learned mentoring flow", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        workResponse.EnsureSuccessStatusCode();
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var entryId = workJson.RootElement.GetProperty("id").GetInt32();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var employeeWork = await _client.GetAsync("/api/admin/work-tracking");
        employeeWork.EnsureSuccessStatusCode();
        using var employeeWorkJson = JsonDocument.Parse(await employeeWork.Content.ReadAsStringAsync());
        Assert.Equal(entryId, Assert.Single(employeeWorkJson.RootElement.EnumerateArray()).GetProperty("id").GetInt32());
        var mentorReview = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new { candidateId, entryIds = new[] { entryId }, status = "Approved", comment = "Strong progress." });
        mentorReview.EnsureSuccessStatusCode();
        using var mentorReviewJson = JsonDocument.Parse(await mentorReview.Content.ReadAsStringAsync());
        Assert.Equal("Mentor Test", mentorReviewJson.RootElement[0].GetProperty("reviewedByDisplayName").GetString());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminReview = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new { candidateId, entryIds = new[] { entryId }, status = "Approved", comment = "Second review." });
        Assert.Equal(HttpStatusCode.Conflict, adminReview.StatusCode);
        var adminNotifications = await _client.GetStringAsync("/api/notifications");
        Assert.Contains("Mentored Candidate was created by Mentor Test", adminNotifications, StringComparison.Ordinal);
        Assert.Contains("Mentored Candidate submitted", adminNotifications, StringComparison.Ordinal);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var candidateNotifications = await _client.GetStringAsync("/api/notifications");
        Assert.Contains("Mentor Test reviewed", candidateNotifications, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmissionClosesAtThirtyDaysAndInterviewDecisionOpensAfterward()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("admin", "AdminDemo123!"));
        using var form = new MultipartFormDataContent();
        AddFormValue(form, "Username", "window-candidate");
        AddFormValue(form, "Password", "WindowCandidate123!");
        AddFormValue(form, "FirstName", "Window");
        AddFormValue(form, "LastName", "Candidate");
        AddFormValue(form, "Email", "window@example.com");
        AddFormValue(form, "PhoneNumber", "5550102030");
        AddFormValue(form, "YearsOfExperience", "1");
        AddFormValue(form, "Stack", "React");
        AddFormValue(form, "PerformanceScore", "0");
        var createResponse = await _client.PostAsync("/api/admin/candidates", form);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var candidateJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var candidateId = candidateJson.RootElement.GetProperty("id").GetInt32();

        var candidateToken = await LoginAndGetTokenAsync("window-candidate", "WindowCandidate123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var submission = new
        {
            workDate = DateOnly.FromDateTime(DateTime.UtcNow), task = "Window test", hoursSpent = 1m,
            accomplishment = "Testing submission window", learnings = "Boundary", blockersAndChallenges = "None", aiToolsUsed = ""
        };
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", submission)).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = await dbContext.EmployeeAccounts.SingleAsync(item => item.Id == candidateId);
        account.CreatedAtUtc = DateTime.UtcNow.AddDays(-30);
        await dbContext.SaveChangesAsync();

        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", submission)).StatusCode);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("admin", "AdminDemo123!"));
        var interviewResponse = await _client.PutAsJsonAsync(
            $"/api/admin/candidates/{candidateId}/interview-decision",
            new { decision = "InterviewQualified", comment = "Ready for interview." });
        Assert.Equal(HttpStatusCode.NoContent, interviewResponse.StatusCode);
        var duplicateDecision = await _client.PutAsJsonAsync(
            $"/api/admin/candidates/{candidateId}/interview-decision",
            new { decision = "NotQualified", comment = "Second decision." });
        Assert.Equal(HttpStatusCode.Conflict, duplicateDecision.StatusCode);
    }

    [Fact]
    public async Task PortalChatRequiresAuthenticationAndScopesCandidateAndWorkDataByRole()
    {
        var anonymousResponse = await _client.PostAsJsonAsync("/api/chat/ask", new { message = "Show candidates" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        var anonymousMcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "tools/list" })
        };
        anonymousMcpRequest.Headers.Accept.ParseAdd("application/json");
        var anonymousMcpResponse = await _client.SendAsync(anonymousMcpRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousMcpResponse.StatusCode);

        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var employeeResponse = await _client.PostAsJsonAsync("/api/admin/employees", new
        {
            username = "chat-mentor",
            password = "ChatMentorInitial123!",
            displayName = "Chat Mentor",
            department = "Engineering",
            stack = ".NET"
        });
        Assert.Equal(HttpStatusCode.Created, employeeResponse.StatusCode);
        using var employeeJson = JsonDocument.Parse(await employeeResponse.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, employeeJson.RootElement.GetProperty("roleDefinitionId").ValueKind);
        var employeeId = employeeJson.RootElement.GetProperty("id").GetInt32();
        var roles = await _client.GetFromJsonAsync<JsonElement>("/api/roles");
        var mentorRoleId = roles.EnumerateArray().Single(role => role.GetProperty("name").GetString() == "Mentor").GetProperty("id").GetInt32();
        var assignMentorRole = await _client.PutAsJsonAsync(
            $"/api/roles/employees/{employeeId}", new { roleDefinitionId = mentorRoleId });
        Assert.Equal(HttpStatusCode.NoContent, assignMentorRole.StatusCode);
        var employeeToken = await LoginAndGetTokenAsync("chat-mentor", "ChatMentorInitial123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var createCandidateResponse = await _client.PostAsJsonAsync("/api/employee/candidates", new
        {
            username = "chat-candidate",
            password = "ChatCandidateInitial123!",
            firstName = "Chat",
            lastName = "Candidate",
            email = "chat-candidate@example.com",
            phoneNumber = "5550102030",
            stack = "React"
        });
        Assert.Equal(HttpStatusCode.Created, createCandidateResponse.StatusCode);
        using var candidateJson = JsonDocument.Parse(await createCandidateResponse.Content.ReadAsStringAsync());
        var candidateId = candidateJson.RootElement.GetProperty("id").GetInt32();

        var mcpInitialize = JsonContent.Create(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new { protocolVersion = "2025-11-25", capabilities = new { }, clientInfo = new { name = "integration-test", version = "1.0" } }
        });
        var initializeRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = mcpInitialize };
        initializeRequest.Headers.Accept.ParseAdd("application/json");
        initializeRequest.Headers.Accept.ParseAdd("text/event-stream");
        var initializeMcpResponse = await _client.SendAsync(initializeRequest);
        initializeMcpResponse.EnsureSuccessStatusCode();
        var toolsListRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 2, method = "tools/list" })
        };
        toolsListRequest.Headers.Accept.ParseAdd("application/json");
        toolsListRequest.Headers.Accept.ParseAdd("text/event-stream");
        toolsListRequest.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-11-25");
        var toolsListResponse = await _client.SendAsync(toolsListRequest);
        toolsListResponse.EnsureSuccessStatusCode();
        var toolsListBody = await toolsListResponse.Content.ReadAsStringAsync();
        Assert.True(toolsListBody.Contains("search_candidates", StringComparison.OrdinalIgnoreCase), toolsListBody);
        var callToolRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 3,
                ["method"] = "tools/call",
                ["params"] = new
                {
                    name = "search_candidates",
                    arguments = new { search = (string?)null, isActive = (bool?)null, candidateId = (int?)candidateId }
                }
            })
        };
        callToolRequest.Headers.Accept.ParseAdd("application/json");
        callToolRequest.Headers.Accept.ParseAdd("text/event-stream");
        callToolRequest.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-11-25");
        var callToolResponse = await _client.SendAsync(callToolRequest);
        callToolResponse.EnsureSuccessStatusCode();
        var callToolBody = await callToolResponse.Content.ReadAsStringAsync();
        Assert.True(callToolBody.Contains("chat-candidate", StringComparison.Ordinal), callToolBody);
        Assert.DoesNotContain("Avery Morgan", callToolBody, StringComparison.Ordinal);

        var employeeQuestion = await _client.PostAsJsonAsync("/api/chat/ask", new { message = "Show candidate details" });
        employeeQuestion.EnsureSuccessStatusCode();
        Assert.Contains("no-store", employeeQuestion.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        using var employeeAnswer = JsonDocument.Parse(await employeeQuestion.Content.ReadAsStringAsync());
        var employeeItems = employeeAnswer.RootElement.GetProperty("items");
        Assert.Equal(1, employeeItems.GetArrayLength());
        Assert.Equal("Chat Candidate", employeeItems[0].GetProperty("title").GetString());

        var employeeDirectoryQuestion = await _client.PostAsJsonAsync("/api/chat/ask", new { message = "Show active employees" });
        employeeDirectoryQuestion.EnsureSuccessStatusCode();
        using var employeeDirectoryAnswer = JsonDocument.Parse(await employeeDirectoryQuestion.Content.ReadAsStringAsync());
        Assert.Empty(employeeDirectoryAnswer.RootElement.GetProperty("items").EnumerateArray());
        Assert.Contains("restricted to Admin", employeeDirectoryAnswer.RootElement.GetProperty("answer").GetString(), StringComparison.Ordinal);

        var candidateToken = await LoginAndGetTokenAsync("chat-candidate", "ChatCandidateInitial123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var workResponse = await _client.PostAsJsonAsync("/api/candidates/me/work-tracking", new
        {
            workDate = DateOnly.FromDateTime(DateTime.UtcNow), task = "Candidate chat work", hoursSpent = 1m,
            accomplishment = "Completed a scoped chat test", learnings = "Role scoped details", blockersAndChallenges = "None", aiToolsUsed = ""
        });
        workResponse.EnsureSuccessStatusCode();
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var entryId = workJson.RootElement.GetProperty("id").GetInt32();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var reviewResponse = await _client.PutAsJsonAsync("/api/admin/work-tracking/review", new
        {
            candidateId,
            entryIds = new[] { entryId },
            status = "Approved",
            comment = "Approved in chat scope test."
        });
        reviewResponse.EnsureSuccessStatusCode();
        var adminQuestion = await _client.PostAsJsonAsync("/api/chat/ask", new { message = "Show active employees and candidates" });
        adminQuestion.EnsureSuccessStatusCode();
        using var adminAnswer = JsonDocument.Parse(await adminQuestion.Content.ReadAsStringAsync());
        var adminItems = adminAnswer.RootElement.GetProperty("items");
        Assert.Contains(adminItems.EnumerateArray(), item => item.GetProperty("title").GetString() == "Chat Mentor");
        Assert.Contains(adminItems.EnumerateArray(), item => item.GetProperty("title").GetString() == "Chat Candidate");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var approvedQuestion = await _client.PostAsJsonAsync("/api/chat/ask", new { message = "Show my approved work entries" });
        approvedQuestion.EnsureSuccessStatusCode();
        using var approvedAnswer = JsonDocument.Parse(await approvedQuestion.Content.ReadAsStringAsync());
        var approvedItems = approvedAnswer.RootElement.GetProperty("items");
        Assert.Equal(1, approvedItems.GetArrayLength());
        Assert.Equal("Approved", approvedItems[0].GetProperty("status").GetString());
        Assert.Contains("Approved in chat scope test", approvedItems[0].GetProperty("details").GetProperty("Review comment").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProfilePhotosPersistAndRestrictAccessToOwnerOrAdmins()
    {
        var candidateLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "candidate",
            password = "CandidateDemo123!"
        });
        candidateLogin.EnsureSuccessStatusCode();
        using var candidateJson = JsonDocument.Parse(await candidateLogin.Content.ReadAsStringAsync());
        var candidateId = candidateJson.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        var candidateToken = candidateJson.RootElement.GetProperty("accessToken").GetString();
        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var admins = await _client.GetFromJsonAsync<JsonElement>("/api/admins");
        var adminId = admins[0].GetProperty("id").GetInt32();
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        using var form = new MultipartFormDataContent();
        var photo = new ByteArrayContent(photoBytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(photo, "photo", "profile.jpg");

        var uploadResponse = await _client.PutAsync($"/api/profile-photos/{candidateId}", form);
        Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);
        var adminReadResponse = await _client.GetAsync($"/api/profile-photos/{candidateId}");
        Assert.Equal(photoBytes, await adminReadResponse.Content.ReadAsByteArrayAsync());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var ownerReadResponse = await _client.GetAsync($"/api/profile-photos/{candidateId}");
        Assert.Equal(photoBytes, await ownerReadResponse.Content.ReadAsByteArrayAsync());
        var otherAccountResponse = await _client.GetAsync($"/api/profile-photos/{adminId}");
        Assert.Equal(HttpStatusCode.Forbidden, otherAccountResponse.StatusCode);
    }

    [Fact]
    public async Task EmployeeRolesLimitDelegatedAccessToOwnedTeamAndPermissions()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var managerRoleResponse = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = $"Delegated manager {suffix}"
        });
        managerRoleResponse.EnsureSuccessStatusCode();
        using var managerRoleJson = JsonDocument.Parse(await managerRoleResponse.Content.ReadAsStringAsync());
        var managerRoleId = managerRoleJson.RootElement.GetProperty("id").GetInt32();
        Assert.Empty(managerRoleJson.RootElement.GetProperty("permissions").EnumerateArray());
        var managerPermissions = new[]
        {
            "Candidates.View", "Candidates.Create", "Candidates.Update", "Candidates.Deactivate", "Candidates.Delete",
            "Reviews.Work", "Reviews.InterviewDecision", "Team.View", "Team.CreateEmployees",
            "Team.ManageEmployees", "Team.CreateRoles", "Team.GrantPermissions"
        };
        var updateManagerPermissions = await _client.PutAsJsonAsync(
            $"/api/roles/{managerRoleId}/permissions", new { permissions = managerPermissions });
        Assert.Equal(HttpStatusCode.NoContent, updateManagerPermissions.StatusCode);

        var candidateOnlyRoleResponse = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = $"Candidate creator {suffix}"
        });
        candidateOnlyRoleResponse.EnsureSuccessStatusCode();
        using var candidateRoleJson = JsonDocument.Parse(await candidateOnlyRoleResponse.Content.ReadAsStringAsync());
        var candidateRoleId = candidateRoleJson.RootElement.GetProperty("id").GetInt32();
        var updateCandidatePermissions = await _client.PutAsJsonAsync(
            $"/api/roles/{candidateRoleId}/permissions", new { permissions = new[] { "Candidates.View", "Candidates.Create" } });
        Assert.Equal(HttpStatusCode.NoContent, updateCandidatePermissions.StatusCode);
        var restrictedRoleResponse = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = $"Restricted role {suffix}"
        });
        restrictedRoleResponse.EnsureSuccessStatusCode();
        using var restrictedRoleJson = JsonDocument.Parse(await restrictedRoleResponse.Content.ReadAsStringAsync());
        var restrictedRoleId = restrictedRoleJson.RootElement.GetProperty("id").GetInt32();
        var updateRestrictedPermissions = await _client.PutAsJsonAsync(
            $"/api/roles/{restrictedRoleId}/permissions", new { permissions = new[] { "Team.ManageRolePermissions" } });
        Assert.Equal(HttpStatusCode.NoContent, updateRestrictedPermissions.StatusCode);

        var managerName = $"permission-manager-{suffix}";
        var createManager = await _client.PostAsJsonAsync("/api/admin/employees", new
        {
            username = managerName,
            password = "PermissionManager123!",
            displayName = "Permission Manager",
            department = "Test",
            stack = "Test"
        });
        Assert.Equal(HttpStatusCode.Created, createManager.StatusCode);
        using var createdManager = JsonDocument.Parse(await createManager.Content.ReadAsStringAsync());
        var managerId = createdManager.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(JsonValueKind.Null, createdManager.RootElement.GetProperty("roleDefinitionId").ValueKind);
        var assignManagerRole = await _client.PutAsJsonAsync(
            $"/api/roles/employees/{managerId}", new { roleDefinitionId = managerRoleId });
        Assert.Equal(HttpStatusCode.NoContent, assignManagerRole.StatusCode);

        var managerToken = await LoginAndGetTokenAsync(managerName, "PermissionManager123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var deniedRolePermissionChange = await _client.PutAsJsonAsync(
            $"/api/roles/{candidateRoleId}/permissions", new { permissions = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.Forbidden, deniedRolePermissionChange.StatusCode);
        var childName = $"permission-child-{suffix}";
        var createChild = await _client.PostAsJsonAsync("/api/employee/managed-employees", new
        {
            username = childName,
            password = "PermissionChild123!",
            displayName = "Permission Child",
            department = "Test",
            stack = "Test"
        });
        Assert.Equal(HttpStatusCode.Created, createChild.StatusCode);
        using var childJson = JsonDocument.Parse(await createChild.Content.ReadAsStringAsync());
        var childId = childJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(JsonValueKind.Null, childJson.RootElement.GetProperty("roleDefinitionId").ValueKind);
        var deniedChildRole = await _client.PutAsJsonAsync(
            $"/api/roles/employees/{childId}", new { roleDefinitionId = restrictedRoleId });
        Assert.Equal(HttpStatusCode.Forbidden, deniedChildRole.StatusCode);
        var assignChildRole = await _client.PutAsJsonAsync(
            $"/api/roles/employees/{childId}", new { roleDefinitionId = candidateRoleId });
        Assert.Equal(HttpStatusCode.NoContent, assignChildRole.StatusCode);

        var managerSummary = await _client.GetFromJsonAsync<JsonElement>("/api/employee/managed-employees/summary");
        Assert.Equal(1, managerSummary.GetProperty("activeEmployees").GetInt32());
        Assert.Equal(0, managerSummary.GetProperty("inactiveEmployees").GetInt32());

        var childToken = await LoginAndGetTokenAsync(childName, "PermissionChild123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", childToken);
        var createCandidate = await _client.PostAsJsonAsync("/api/employee/candidates", new
        {
            username = $"permission-candidate-{suffix}",
            password = "PermissionCandidate123!",
            firstName = "Scoped",
            lastName = "Candidate",
            email = $"scoped-{suffix}@example.com",
            phoneNumber = "5550102030",
            stack = "Test"
        });
        Assert.Equal(HttpStatusCode.Created, createCandidate.StatusCode);
        var forbiddenEmployeeCreation = await _client.PostAsJsonAsync("/api/employee/managed-employees", new
        {
            username = $"unpermitted-child-{suffix}",
            password = "UnpermittedChild123!",
            displayName = "Unpermitted Child",
            department = "Test",
            stack = "Test"
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenEmployeeCreation.StatusCode);

        using var candidateJson = JsonDocument.Parse(await createCandidate.Content.ReadAsStringAsync());
        var candidateId = candidateJson.RootElement.GetProperty("id").GetInt32();
        var forbiddenUpdate = await _client.PutAsJsonAsync($"/api/employee/candidates/{candidateId}", new
        {
            firstName = "Changed", lastName = "Candidate", email = $"changed-{suffix}@example.com",
            phoneNumber = "5550102030", stack = "Test"
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var deactivateChild = await _client.PatchAsJsonAsync($"/api/employee/managed-employees/{childId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateChild.StatusCode);
        var inactiveSummary = await _client.GetFromJsonAsync<JsonElement>("/api/employee/managed-employees/summary");
        Assert.Equal(0, inactiveSummary.GetProperty("activeEmployees").GetInt32());
        Assert.Equal(1, inactiveSummary.GetProperty("inactiveEmployees").GetInt32());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", childToken);
        var inactiveRequest = await _client.GetAsync("/api/roles/permissions");
        Assert.Equal(HttpStatusCode.Unauthorized, inactiveRequest.StatusCode);
    }

    private static MultipartFormDataContent CreateCandidateUpdateForm(bool isActive, string? phoneNumber = "555-0101")
    {
        var form = new MultipartFormDataContent();
        AddFormValue(form, "FirstName", "New");
        AddFormValue(form, "LastName", "Candidate");
        AddFormValue(form, "Email", "new@example.com");
        if (phoneNumber is not null)
        {
            AddFormValue(form, "PhoneNumber", phoneNumber);
        }
        AddFormValue(form, "YearsOfExperience", "3");
        AddFormValue(form, "PerformanceScore", "87");
        AddFormValue(form, "IsActive", isActive.ToString().ToLowerInvariant());
        return form;
    }

    private async Task<string> LoginAndGetTokenAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static void AddFormValue(MultipartFormDataContent form, string name, string value) =>
        form.Add(new StringContent(value), name);
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuthenticationApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-bytes-long",
                ["DemoAccounts:AdminPassword"] = "AdminDemo123!",
                ["DemoAccounts:CandidatePassword"] = "CandidateDemo123!"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
