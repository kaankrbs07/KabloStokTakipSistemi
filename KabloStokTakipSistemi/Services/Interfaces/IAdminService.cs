using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAdminService
{
    // Tüm adminleri getir
    Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync();

    // Username ile admin getir
    Task<GetAdminDto?> GetAdminByUsernameAsync(string username);

    // Admin oluştur (CreateUserDto + CreateAdminDto ile)
    Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto);

    // Departman güncelleme (Username bazlı)
    Task<bool> UpdateAdminDepartmentAsync(string username, string newDepartmentName);
}