using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync();
    Task<GetAdminDto?> GetAdminByIdAsync(long adminId);

    Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto);
    Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName);
}
