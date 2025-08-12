using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync();
    Task<GetAdminDto?> GetAdminByIdAsync(long adminId);

    // Users için ayrı DTO (CreateUserDto) zaten var; Admins için CreateAdminDto
    Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto);

    // DepartmentName artık string
    Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName);
}