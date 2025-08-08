using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync();
    Task<GetAdminDto?> GetAdminByIdAsync(long adminId);
    Task<bool> CreateAdminAsync(CreateUserDto dto,CreateAdminDto adminDto);
    Task<bool> UpdateAdminDepartmentAsync(long adminId, int newDepartmentId);
}
