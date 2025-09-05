
using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<GetEmployeeDto>> GetAllEmployeesAsync();
    Task<GetEmployeeDto?> GetEmployeeByIdAsync(long employeeId);
    Task<bool> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<bool> UpdateEmployeeDepartmentAsync(long employeeId, int newDepartmentId);
}