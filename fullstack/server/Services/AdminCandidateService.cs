using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Identity;

namespace fullstackapplication.Server.Services;

public sealed class AdminCandidateService(
    IAdminCandidateRepository repository,
    IPasswordHasher<EmployeeAccount> passwordHasher) : IAdminCandidateService
{
    public async Task<IReadOnlyList<CandidateProfileResponse>> GetCandidatesAsync(CancellationToken cancellationToken)
    {
        var accounts = await repository.GetCandidatesAsync(cancellationToken);
        return accounts.Select(ToResponse).ToArray();
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
            Experience = request.Experience.Trim(),
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
        profile.Experience = request.Experience.Trim();
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
            profile.Experience,
            profile.Stack,
            profile.PerformanceScore,
            account.IsActive,
            account.InactiveAtUtc,
            profile.ResumeFileName,
            profile.ResumeContentType);
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
