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

    public AuthenticationApiTests(ApiFactory factory)
    {
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
        AddFormValue(createForm, "Experience", "Three years of experience");
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
        Assert.Equal("No blockers", createdJson.RootElement.GetProperty("blockersAndChallenges").GetString());

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
        var candidateReviewAttempt = await _client.PutAsJsonAsync($"/api/admin/work-tracking/{entryId}/review", new { grade = 90, adminFeedback = "Good" });
        Assert.Equal(HttpStatusCode.Forbidden, candidateReviewAttempt.StatusCode);

        var adminToken = await LoginAndGetTokenAsync("admin", "AdminDemo123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var filteredResponse = await _client.GetAsync("/api/admin/work-tracking?stack=ASP.NET%20Core&dateFrom=2026-11-02&dateTo=2026-11-02");
        filteredResponse.EnsureSuccessStatusCode();
        using var filteredJson = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync());
        Assert.Contains(filteredJson.RootElement.EnumerateArray(), entry => entry.GetProperty("id").GetInt32() == entryId);

        var reviewResponse = await _client.PutAsJsonAsync($"/api/admin/work-tracking/{entryId}/review", new { grade = 94, adminFeedback = "Clear delivery." });
        reviewResponse.EnsureSuccessStatusCode();
        using var reviewJson = JsonDocument.Parse(await reviewResponse.Content.ReadAsStringAsync());
        Assert.Equal(94, reviewJson.RootElement.GetProperty("grade").GetInt32());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateToken);
        var entriesResponse = await _client.GetAsync("/api/candidates/me/work-tracking");
        entriesResponse.EnsureSuccessStatusCode();
        using var entriesJson = JsonDocument.Parse(await entriesResponse.Content.ReadAsStringAsync());
        Assert.Equal(94, Assert.Single(entriesJson.RootElement.EnumerateArray()).GetProperty("grade").GetInt32());
        Assert.Equal("Clear delivery.", entriesJson.RootElement[0].GetProperty("adminFeedback").GetString());
        Assert.Equal("No blockers", entriesJson.RootElement[0].GetProperty("blockersAndChallenges").GetString());
        var deleteReviewedResponse = await _client.DeleteAsync($"/api/candidates/me/work-tracking/{entryId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteReviewedResponse.StatusCode);
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
        AddFormValue(form, "Experience", "Three years of experience");
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
