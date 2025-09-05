using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<GetUserDto>> GetAllUsersAsync();
    Task<GetUserDto?> GetUserByIdAsync(long userId);

    // Users tablosuna yeni kayıt 
    Task<bool> CreateUserAsync(CreateUserDto dto);

    // Profil alanlarını ve opsiyonel olarak Role/Password’ü günceller
    Task<bool> UpdateUserAsync(UpdateUserDto dto);

    // IsActive=false yapar
    Task<bool> DeactivateUserAsync(long userId);

    // Örnek rapor/özet uç noktası (varsa)
    Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId);
}