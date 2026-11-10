using fullstackapplication.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace fullstackapplication.Server.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher<EmployeeAccount> passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var demoAccounts = new[]
        {
            (Username: "admin", DisplayName: "Demo Administrator", Role: EmployeeRoles.Admin, PasswordKey: "DemoAccounts:AdminPassword"),
            (Username: "candidate", DisplayName: "Demo Candidate", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate01", DisplayName: "Avery Morgan", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate02", DisplayName: "Jordan Lee", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate03", DisplayName: "Taylor Brooks", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate04", DisplayName: "Casey Rivera", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate05", DisplayName: "Riley Chen", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate06", DisplayName: "Morgan Patel", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate07", DisplayName: "Quinn Davis", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate08", DisplayName: "Jamie Wilson", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword"),
            (Username: "candidate09", DisplayName: "Alex Kim", Role: EmployeeRoles.Candidate, PasswordKey: "DemoAccounts:CandidatePassword")
        };

        foreach (var demoAccount in demoAccounts)
        {
            var normalizedUsername = demoAccount.Username.ToUpperInvariant();
            var account = await dbContext.EmployeeAccounts
                .Include(employee => employee.CandidateProfile)
                .SingleOrDefaultAsync(
                    employee => employee.NormalizedUsername == normalizedUsername,
                    cancellationToken);

            if (account is null)
            {
                var password = configuration[demoAccount.PasswordKey];
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException($"Configure {demoAccount.PasswordKey} to seed development demo accounts.");
                }

                account = new EmployeeAccount
                {
                    Username = demoAccount.Username,
                    NormalizedUsername = normalizedUsername,
                    DisplayName = demoAccount.DisplayName,
                    Role = demoAccount.Role,
                    IsActive = true
                };
                account.PasswordHash = passwordHasher.HashPassword(account, password);
                dbContext.EmployeeAccounts.Add(account);
            }

            if (demoAccount.Role == EmployeeRoles.Candidate && account.CandidateProfile is null)
            {
                var nameParts = demoAccount.DisplayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                account.CandidateProfile = new CandidateProfile
                {
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                    Email = $"{demoAccount.Username}@development.invalid",
                    PhoneNumber = string.Empty,
                    LegacyExperienceDetails = "Development sample profile",
                    YearsOfExperience = 1,
                    PerformanceScore = 0
                };
            }

            var candidateProfile = account.CandidateProfile;
            if (demoAccount.Role == EmployeeRoles.Candidate
                && candidateProfile is not null
                && string.IsNullOrWhiteSpace(candidateProfile.Email))
            {
                candidateProfile.Email = $"{demoAccount.Username}@development.invalid";
            }
        }

        var mentorRole = await dbContext.EmployeeRoleDefinitions
            .Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.IsBuiltIn && role.Name == "Mentor", cancellationToken);
        if (mentorRole is null)
        {
            mentorRole = new EmployeeRoleDefinition
            {
                Name = "Mentor",
                IsBuiltIn = true,
                Permissions = new[]
                {
                    EmployeePermissions.ViewCandidates,
                    EmployeePermissions.CreateCandidates,
                    EmployeePermissions.UpdateCandidates,
                    EmployeePermissions.DeactivateCandidates,
                    EmployeePermissions.ReviewWork,
                    EmployeePermissions.DecideInterviews
                }.Select(permission => new EmployeeRolePermission { PermissionCode = permission }).ToList()
            };
            dbContext.EmployeeRoleDefinitions.Add(mentorRole);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
