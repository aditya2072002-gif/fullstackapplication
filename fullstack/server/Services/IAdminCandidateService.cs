using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IAdminCandidateService
{
    Task<IReadOnlyList<CandidateProfileResponse>> GetCandidatesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> GetCandidateAsync(int id, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> CreateCandidateAsync(
        CreateCandidateRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateCandidateAsync(
        int id,
        UpdateCandidateRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateCandidateStatusAsync(
        int id,
        UpdateCandidateStatusRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteCandidateAsync(int id, CancellationToken cancellationToken);
}
