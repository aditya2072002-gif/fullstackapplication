using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IEmployeeManagementService
{
    Task<IReadOnlyList<EmployeeResponseDto>> GetEmployeesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<EmployeeResponseDto>> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        int? creatorEmployeeAccountId,
        bool isAdmin,
        System.Security.Claims.ClaimsPrincipal caller,
        CancellationToken cancellationToken);
}
