using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface ICandidateSelfService
{
    Task<ServiceResult<CandidateProfileResponse>> GetMyProfileAsync(int accountId, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateMyStackAsync(
        int accountId, UpdateCandidateStackRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateMyResumeAsync(
        int accountId,
        ResumeUploadRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ChangeMyPasswordAsync(
        int accountId,
        ChangeCandidatePasswordRequest request,
        CancellationToken cancellationToken);
}
