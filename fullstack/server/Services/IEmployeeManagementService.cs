using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface IEmployeeManagementService
{
    Task<IReadOnlyList<EmployeeResponseDto>> GetEmployeesAsync(CancellationToken cancellationToken);

    Task<ServiceResult<EmployeeResponseDto>> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken);
}
