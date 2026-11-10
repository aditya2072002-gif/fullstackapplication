using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Identity;

namespace fullstackapplication.Server.Services;

public sealed class AdminCandidateService(
    IAdminCandidateRepository repository,
    IPasswordHasher<EmployeeAccount> passwordHasher,
    INotificationService notificationService) : IAdminCandidateService
{
    public async Task<IReadOnlyList<CandidateProfileResponse>> GetCandidatesAsync(CancellationToken cancellationToken)
    {
        var accounts = await repository.GetCandidatesAsync(cancellationToken);
        return accounts.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<CandidateProfileResponse>> GetEmployeeCandidatesAsync(
        int employeeAccountId,
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetCandidatesCreatedByAsync(employeeAccountId, cancellationToken);
        return accounts.Select(ToResponse).ToArray();
    }

    public async Task<ServiceResult<CandidateProfileResponse>> GetEmployeeCandidateAsync(
        int employeeAccountId,
        int candidateId,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateCreatedByAsync(candidateId, employeeAccountId, cancellationToken);
        return account?.CandidateProfile is null
            ? ServiceResult<CandidateProfileResponse>.NotFound()
            : ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> GetCandidateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateAsync(id, cancellationToken);
        return account is null
            ? ServiceResult<CandidateProfileResponse>.NotFound()
            : ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateResumeFile>> GetCandidateResumeAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetCandidateResumeAsync(id, cancellationToken);
        if (profile?.ResumeContent is not { Length: > 0 }
            || string.IsNullOrWhiteSpace(profile.ResumeFileName)
            || string.IsNullOrWhiteSpace(profile.ResumeContentType))
        {
            return ServiceResult<CandidateResumeFile>.NotFound();
        }

        return ServiceResult<CandidateResumeFile>.Success(new CandidateResumeFile(
            profile.ResumeContent,
            profile.ResumeFileName,
            profile.ResumeContentType));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> CreateCandidateAsync(
        CreateCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        if (await repository.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return ServiceResult<CandidateProfileResponse>.Conflict("Username is already in use.");
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var displayName = $"{firstName} {lastName}";
        if (displayName.Length > 100)
        {
            return ServiceResult<CandidateProfileResponse>.Invalid(
                "First and last name combined must not exceed 100 characters.");
        }

        CandidateResumeFile? resume = null;
        if (request.Resume is not null)
        {
            var validation = await CandidateResumeUploadValidator.ValidateAndReadAsync(request.Resume, cancellationToken);
            if (validation.Error is not null)
            {
                return ServiceResult<CandidateProfileResponse>.Invalid(validation.Error);
            }

            resume = validation.File;
        }

        var account = new EmployeeAccount
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            DisplayName = displayName,
            Role = EmployeeRoles.Candidate,
            IsActive = true,
            PasswordHash = string.Empty
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
        account.CandidateProfile = new CandidateProfile
        {
            FirstName = firstName,
            LastName = lastName,
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            YearsOfExperience = request.YearsOfExperience,
            LegacyExperienceDetails = string.Empty,
            Stack = request.Stack.Trim(),
            PerformanceScore = request.PerformanceScore,
            ResumeContent = resume?.Content,
            ResumeFileName = resume?.FileName,
            ResumeContentType = resume?.ContentType
        };

        await repository.AddCandidateAsync(account, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> CreateEmployeeCandidateAsync(
        int employeeAccountId,
        CreateEmployeeCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (await repository.UsernameExistsAsync(username.ToUpperInvariant(), cancellationToken))
        {
            return ServiceResult<CandidateProfileResponse>.Conflict("Username is already in use.");
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var displayName = $"{firstName} {lastName}";
        if (displayName.Length > 100)
        {
            return ServiceResult<CandidateProfileResponse>.Invalid("First and last name combined must not exceed 100 characters.");
        }

        var phoneNumber = request.PhoneNumber.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10}$"))
        {
            return ServiceResult<CandidateProfileResponse>.Invalid("Enter a 10-digit phone number.");
        }

        var account = new EmployeeAccount
        {
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            DisplayName = displayName,
            Role = EmployeeRoles.Candidate,
            IsActive = true
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
        account.CandidateProfile = new CandidateProfile
        {
            FirstName = firstName,
            LastName = lastName,
            Email = request.Email.Trim(),
            PhoneNumber = phoneNumber,
            Stack = request.Stack.Trim(),
            PerformanceScore = 0,
            LegacyExperienceDetails = string.Empty,
            CreatorEmployeeAccountId = employeeAccountId
        };

        await repository.AddCandidateAsync(account, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        var employeeName = await notificationService.GetAccountDisplayNameAsync(employeeAccountId, cancellationToken)
            ?? throw new InvalidOperationException("The creating employee could not be found.");
        await notificationService.NotifyCandidateCreatedAsync(displayName, employeeName, cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(new CandidateProfileResponse(
            account.Id, account.Username, firstName, lastName, account.CandidateProfile.Email,
            phoneNumber, null, account.CandidateProfile.Stack, 0, true, null, null, null,
            employeeAccountId, employeeName, account.CreatedAtUtc,
            account.CandidateProfile.InterviewDecision, account.CandidateProfile.InterviewDecisionComment,
            account.CandidateProfile.InterviewDecisionAtUtc, account.CandidateProfile.InterviewDecisionByEmployeeAccountId,
            account.CreatedAtUtc.AddDays(30), DateTime.UtcNow < account.CreatedAtUtc.AddDays(30),
            DateTime.UtcNow >= account.CreatedAtUtc.AddDays(30)));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateEmployeeCandidateAsync(
        int employeeAccountId,
        int candidateId,
        UpdateEmployeeCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateCreatedByAsync(candidateId, employeeAccountId, cancellationToken);
        if (account?.CandidateProfile is null) return ServiceResult<CandidateProfileResponse>.NotFound();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        if ($"{firstName} {lastName}".Length > 100)
        {
            return ServiceResult<CandidateProfileResponse>.Invalid("First and last name combined must not exceed 100 characters.");
        }

        var phoneNumber = request.PhoneNumber.Trim();
        if (account.IsActive && !System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10}$"))
        {
            return ServiceResult<CandidateProfileResponse>.Invalid("Enter a 10-digit phone number for an active candidate.");
        }

        account.CandidateProfile.FirstName = firstName;
        account.CandidateProfile.LastName = lastName;
        account.CandidateProfile.Email = request.Email.Trim();
        account.CandidateProfile.PhoneNumber = phoneNumber;
        account.CandidateProfile.Stack = request.Stack.Trim();
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateEmployeeCandidateStatusAsync(
        int employeeAccountId,
        int candidateId,
        UpdateEmployeeCandidateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateCreatedByAsync(candidateId, employeeAccountId, cancellationToken);
        if (account?.CandidateProfile is null) return ServiceResult<CandidateProfileResponse>.NotFound();
        SetActiveStatus(account, request.IsActive!.Value);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateCandidateAsync(
        int id,
        UpdateCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateAsync(id, cancellationToken);
        if (account?.CandidateProfile is null)
        {
            return ServiceResult<CandidateProfileResponse>.NotFound();
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        if ($"{firstName} {lastName}".Length > 100)
        {
            return ServiceResult<CandidateProfileResponse>.Invalid(
                "First and last name combined must not exceed 100 characters.");
        }

        var phoneNumber = request.PhoneNumber?.Trim();
        if (request.IsActive == true && string.IsNullOrWhiteSpace(phoneNumber))
        {
            return ServiceResult<CandidateProfileResponse>.Invalid("Phone number is required for an active candidate.");
        }

        CandidateResumeFile? resume = null;
        if (request.Resume is not null)
        {
            var validation = await CandidateResumeUploadValidator.ValidateAndReadAsync(request.Resume, cancellationToken);
            if (validation.Error is not null)
            {
                return ServiceResult<CandidateProfileResponse>.Invalid(validation.Error);
            }

            resume = validation.File;
        }

        var profile = account.CandidateProfile;
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.Email = request.Email.Trim();
        profile.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
        profile.YearsOfExperience = request.YearsOfExperience;
        profile.Stack = request.Stack.Trim();
        profile.PerformanceScore = request.PerformanceScore;
        if (resume is not null)
        {
            SetResume(profile, resume);
        }

        SetActiveStatus(account, request.IsActive!.Value);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateCandidateStatusAsync(
        int id,
        UpdateCandidateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateAsync(id, cancellationToken);
        if (account?.CandidateProfile is null)
        {
            return ServiceResult<CandidateProfileResponse>.NotFound();
        }

        SetActiveStatus(account, request.IsActive!.Value);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<bool>> DeleteCandidateAsync(int id, CancellationToken cancellationToken)
    {
        var account = await repository.GetCandidateAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return ServiceResult<bool>.NotFound();
        }

        if (account.IsActive)
        {
            return ServiceResult<bool>.Conflict("Deactivate the candidate before deleting the record.");
        }

        repository.RemoveCandidate(account);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static CandidateProfileResponse ToResponse(EmployeeAccount account)
    {
        var profile = account.CandidateProfile!;
        return new CandidateProfileResponse(
            account.Id,
            account.Username,
            profile.FirstName,
            profile.LastName,
            profile.Email,
            profile.PhoneNumber,
            profile.YearsOfExperience,
            profile.Stack,
            profile.PerformanceScore,
            account.IsActive,
            account.InactiveAtUtc,
            profile.ResumeFileName,
            profile.ResumeContentType,
            profile.CreatorEmployeeAccountId,
            profile.CreatorEmployeeAccount?.DisplayName,
            account.CreatedAtUtc,
            profile.InterviewDecision,
            profile.InterviewDecisionComment,
            profile.InterviewDecisionAtUtc,
            profile.InterviewDecisionByEmployeeAccountId,
            account.CreatedAtUtc.AddDays(30),
            DateTime.UtcNow < account.CreatedAtUtc.AddDays(30),
            DateTime.UtcNow >= account.CreatedAtUtc.AddDays(30));
    }

    private static void SetActiveStatus(EmployeeAccount account, bool isActive)
    {
        if (isActive)
        {
            account.IsActive = true;
            account.InactiveAtUtc = null;
        }
        else if (account.IsActive || account.InactiveAtUtc is null)
        {
            account.IsActive = false;
            account.InactiveAtUtc = DateTime.UtcNow;
        }
    }

    private static void SetResume(CandidateProfile profile, CandidateResumeFile resume)
    {
        profile.ResumeContent = resume.Content;
        profile.ResumeFileName = resume.FileName;
        profile.ResumeContentType = resume.ContentType;
    }
}
