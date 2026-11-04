using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IAdminCandidateService
{
    Task<IReadOnlyList<CandidateProfileResponse>> GetCandidatesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> GetCandidateAsync(int id, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateResumeFile>> GetCandidateResumeAsync(int id, CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> CreateCandidateAsync(
        CreateCandidateRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CandidateProfileResponse>> GetEmployeeCandidatesAsync(
        int employeeAccountId,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> GetEmployeeCandidateAsync(
        int employeeAccountId,
        int candidateId,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> CreateEmployeeCandidateAsync(
        int employeeAccountId,
        CreateEmployeeCandidateRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateEmployeeCandidateAsync(
        int employeeAccountId,
        int candidateId,
        UpdateEmployeeCandidateRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CandidateProfileResponse>> UpdateEmployeeCandidateStatusAsync(
        int employeeAccountId,
        int candidateId,
        UpdateEmployeeCandidateStatusRequest request,
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
