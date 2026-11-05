using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Identity;

namespace fullstackapplication.Server.Services;

public sealed class EmployeeManagementService(
    IEmployeeManagementRepository repository,
    IPasswordHasher<EmployeeAccount> passwordHasher) : IEmployeeManagementService
{
    public async Task<IReadOnlyList<EmployeeResponseDto>> GetEmployeesAsync(CancellationToken cancellationToken)
    {
        var employees = await repository.GetEmployeesAsync(cancellationToken);
        return employees.Select(ToResponse).ToArray();
    }

    public async Task<ServiceResult<EmployeeResponseDto>> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        if (await repository.UsernameExistsAsync(normalizedUsername, cancellationToken))
        {
            return ServiceResult<EmployeeResponseDto>.Conflict("Username is already in use.");
        }

        var displayName = request.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return ServiceResult<EmployeeResponseDto>.Invalid("Display name is required.");
        }

        var employee = new EmployeeAccount
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            DisplayName = displayName,
            Role = EmployeeRoles.Employee,
            Department = request.Department.Trim(),
            Stack = request.Stack.Trim(),
            IsActive = true
        };
        employee.PasswordHash = passwordHasher.HashPassword(employee, request.Password);
        await repository.AddAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<EmployeeResponseDto>.Success(ToResponse(employee));
    }

    private static EmployeeResponseDto ToResponse(EmployeeAccount employee) => new(
        employee.Id,
        employee.Username,
        employee.DisplayName,
        employee.Department ?? string.Empty,
        employee.Stack ?? string.Empty,
        employee.Role,
        employee.IsActive);
}
