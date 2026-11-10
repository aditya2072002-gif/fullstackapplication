using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Identity;

namespace fullstackapplication.Server.Services;

public sealed class CandidateSelfService(
    ICandidateSelfRepository repository,
    IPasswordHasher<EmployeeAccount> passwordHasher) : ICandidateSelfService
{
    public async Task<ServiceResult<CandidateProfileResponse>> GetMyProfileAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetActiveCandidateWithProfileAsync(accountId, cancellationToken);
        if (account?.CandidateProfile is null)
        {
            return ServiceResult<CandidateProfileResponse>.NotFound();
        }

        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateMyStackAsync(
        int accountId, UpdateCandidateStackRequest request, CancellationToken cancellationToken)
    {
        var account = await repository.GetActiveCandidateWithProfileAsync(accountId, cancellationToken);
        if (account?.CandidateProfile is null) return ServiceResult<CandidateProfileResponse>.NotFound();
        account.CandidateProfile.Stack = request.Stack.Trim();
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<CandidateProfileResponse>> UpdateMyResumeAsync(
        int accountId,
        ResumeUploadRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetActiveCandidateWithProfileAsync(accountId, cancellationToken);
        if (account?.CandidateProfile is null)
        {
            return ServiceResult<CandidateProfileResponse>.NotFound();
        }

        var validation = await CandidateResumeUploadValidator.ValidateAndReadAsync(request.Resume!, cancellationToken);
        if (validation.Error is not null)
        {
            return ServiceResult<CandidateProfileResponse>.Invalid(validation.Error);
        }

        var resume = validation.File!;
        account.CandidateProfile.ResumeContent = resume.Content;
        account.CandidateProfile.ResumeFileName = resume.FileName;
        account.CandidateProfile.ResumeContentType = resume.ContentType;
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateProfileResponse>.Success(ToResponse(account));
    }

    public async Task<ServiceResult<bool>> ChangeMyPasswordAsync(
        int accountId,
        ChangeCandidatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetActiveCandidateAccountAsync(accountId, cancellationToken);
        if (account is null)
        {
            return ServiceResult<bool>.NotFound();
        }

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return ServiceResult<bool>.Invalid("Current password is incorrect.");
        }

        account.PasswordHash = passwordHasher.HashPassword(account, request.NewPassword);
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
}
